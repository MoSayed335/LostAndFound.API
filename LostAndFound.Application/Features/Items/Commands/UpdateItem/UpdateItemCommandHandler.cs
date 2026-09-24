using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Items.Common;
using LostAndFound.Application.Features.Items.DTOs;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LostAndFound.Application.Features.Items.Commands.UpdateItem;

public class UpdateItemCommandHandler : IRequestHandler<UpdateItemCommand, Result<ItemResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileService _fileService;
    private readonly ILogger<UpdateItemCommandHandler> _logger;

    public UpdateItemCommandHandler(
        IUnitOfWork unitOfWork,
        IFileService fileService,
        ILogger<UpdateItemCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _fileService = fileService;
        _logger = logger;
    }

    public async Task<Result<ItemResponseDto>> Handle(UpdateItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _unitOfWork.Items.GetByIdAsync(request.Id, asNoTracking: false, cancellationToken);
        if (item is null)
        {
            return Result<ItemResponseDto>.Failure("Item not found.", ResultErrorType.NotFound);
        }

        // Only owner or Admin can update
        if (!request.IsAdmin && item.UserId != request.CurrentUserId)
        {
            _logger.LogWarning("Unauthorized attempt to update item {ItemId} by user {UserId}", request.Id, request.CurrentUserId);
            return Result<ItemResponseDto>.Failure("You are not authorized to update this item.", ResultErrorType.Forbidden);
        }

        // Returned/Closed items should not be editable unless there is a clear reason
        if (item.Status is ItemStatus.Returned or ItemStatus.Closed)
        {
            _logger.LogWarning("Attempt to update item {ItemId} with final status {Status}", item.Id, item.Status);
            return Result<ItemResponseDto>.Failure("Returned or closed items cannot be modified.", ResultErrorType.Validation);
        }

        // Guard against bypassing workflow states via item update
        if (request.Status.HasValue && request.Status.Value != item.Status)
        {
            if (request.Status.Value is ItemStatus.Claimed or ItemStatus.Returned)
            {
                _logger.LogWarning("Attempt to directly set item {ItemId} status to {TargetStatus} bypassing workflow", item.Id, request.Status.Value);
                return Result<ItemResponseDto>.Failure(
                    "Item status cannot be set directly to Claimed or Returned. Use the claim approval and return workflow.",
                    ResultErrorType.Validation);
            }

            item.Status = request.Status.Value;
        }

        string? oldImageUrl = item.ImageUrl;
        string? newUploadedUrl = null;

        if (request.Image is not null)
        {
            newUploadedUrl = await _fileService.SaveFileAsync(request.Image, "items", cancellationToken);
        }

        // Apply updates (UserId, CreatedAt, ItemType remain untouched)
        item.Title = request.Title;
        item.Description = request.Description;
        item.CategoryId = request.CategoryId;
        item.Location = request.Location;
        item.DateLostOrFound = request.DateLostOrFound.Kind == DateTimeKind.Utc ? request.DateLostOrFound : request.DateLostOrFound.ToUniversalTime();
        
        if (newUploadedUrl is not null)
        {
            item.ImageUrl = newUploadedUrl;
        }
        else if (request.ImageUrl is not null)
        {
            item.ImageUrl = request.ImageUrl;
        }

        item.UpdatedAt = DateTime.UtcNow;

        try
        {
            _unitOfWork.Items.Update(item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Item {ItemId} successfully updated by user {UserId}", item.Id, request.CurrentUserId);
        }
        catch
        {
            if (newUploadedUrl is not null)
            {
                await _fileService.DeleteFileAsync(newUploadedUrl, cancellationToken);
            }
            throw;
        }

        // If a new image was uploaded and persisted, cleanly delete the old image file
        if (newUploadedUrl is not null && !string.IsNullOrEmpty(oldImageUrl) && !string.Equals(oldImageUrl, newUploadedUrl, StringComparison.OrdinalIgnoreCase))
        {
            await _fileService.DeleteFileAsync(oldImageUrl, cancellationToken);
        }

        var updated = await _unitOfWork.Items.GetByIdWithDetailsAsync(item.Id, cancellationToken);
        return Result<ItemResponseDto>.Success(updated!.ToResponseDto());
    }
}
