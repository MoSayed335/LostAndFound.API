using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Items.Common;
using LostAndFound.Application.Features.Items.DTOs;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LostAndFound.Application.Features.Items.Commands.ReturnItem;

public class ReturnItemCommandHandler : IRequestHandler<ReturnItemCommand, Result<ItemResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReturnItemCommandHandler> _logger;

    public ReturnItemCommandHandler(IUnitOfWork unitOfWork, ILogger<ReturnItemCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ItemResponseDto>> Handle(ReturnItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _unitOfWork.Items.GetByIdAsync(request.ItemId, asNoTracking: false, cancellationToken);
        if (item is null)
        {
            return Result<ItemResponseDto>.Failure("Item not found.", ResultErrorType.NotFound);
        }

        // Only owner or Admin can mark as returned
        if (!request.IsAdmin && item.UserId != request.CurrentUserId)
        {
            _logger.LogWarning("Unauthorized attempt to mark item {ItemId} as returned by user {UserId}", request.ItemId, request.CurrentUserId);
            return Result<ItemResponseDto>.Failure("You are not authorized to mark this item as returned.", ResultErrorType.Forbidden);
        }

        // Item must have status Claimed
        if (item.Status != ItemStatus.Claimed)
        {
            _logger.LogWarning("Return rejected for item {ItemId}: Current status is {Status}, not Claimed", item.Id, item.Status);
            return Result<ItemResponseDto>.Failure($"Only claimed items can be marked as returned. Current item status is {item.Status}.", ResultErrorType.Conflict);
        }

        var now = DateTime.UtcNow;
        item.Status = ItemStatus.Returned;
        item.UpdatedAt = now;
        _unitOfWork.Items.Update(item);

        // Safe cleanup: reject any residual pending claims
        var pendingClaims = await _unitOfWork.Claims.GetPendingClaimsForItemAsync(item.Id, cancellationToken);
        foreach (var claim in pendingClaims)
        {
            claim.Status = ClaimStatus.Rejected;
            claim.ReviewedAt = now;
        }

        if (pendingClaims.Count > 0)
        {
            _unitOfWork.Claims.UpdateRange(pendingClaims);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Item {ItemId} successfully marked as Returned by user {UserId}", item.Id, request.CurrentUserId);

        var updated = await _unitOfWork.Items.GetByIdWithDetailsAsync(item.Id, cancellationToken);
        return Result<ItemResponseDto>.Success(updated!.ToResponseDto());
    }
}
