using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Items.Common;
using LostAndFound.Application.Features.Items.DTOs;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using LostAndFound.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LostAndFound.Application.Features.Items.Commands.CreateFoundItem;

public class CreateFoundItemCommandHandler : IRequestHandler<CreateFoundItemCommand, Result<ItemResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileService _fileService;
    private readonly ILogger<CreateFoundItemCommandHandler> _logger;
    private readonly IBackgroundJobScheduler? _backgroundJobScheduler;

    public CreateFoundItemCommandHandler(
        IUnitOfWork unitOfWork,
        IFileService fileService,
        ILogger<CreateFoundItemCommandHandler> logger,
        IBackgroundJobScheduler? backgroundJobScheduler = null)
    {
        _unitOfWork = unitOfWork;
        _fileService = fileService;
        _logger = logger;
        _backgroundJobScheduler = backgroundJobScheduler;
    }

    public async Task<Result<ItemResponseDto>> Handle(CreateFoundItemCommand request, CancellationToken cancellationToken)
    {
        string? storedImageUrl = request.ImageUrl;
        if (request.Image is not null)
        {
            storedImageUrl = await _fileService.SaveFileAsync(request.Image, "items", cancellationToken);
        }

        var item = new Item
        {
            Title = request.Title,
            Description = request.Description,
            Type = ItemType.Found,
            CategoryId = request.CategoryId,
            UserId = request.UserId,
            Location = request.Location,
            DateLostOrFound = request.DateLostOrFound.Kind == DateTimeKind.Utc ? request.DateLostOrFound : request.DateLostOrFound.ToUniversalTime(),
            ImageUrl = storedImageUrl,
            Status = ItemStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _unitOfWork.Items.AddAsync(item, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Item {ItemId} (Found) created by user {UserId} in category {CategoryId}", item.Id, item.UserId, item.CategoryId);

            if (_backgroundJobScheduler is not null)
            {
                // 1. Fire-and-Forget Job: Asynchronously process matches without blocking HTTP response
                var parentJobId = _backgroundJobScheduler.Enqueue<IBackgroundJobService>(
                    service => service.ProcessItemMatchesAsync(item.Id, CancellationToken.None));

                // 4. Continuation Job: Chain post-processing summary notification
                _backgroundJobScheduler.ContinueWith<IBackgroundJobService>(
                    parentJobId,
                    service => service.SendItemProcessingSummaryAsync(item.Id, CancellationToken.None));
            }
        }
        catch
        {
            if (request.Image is not null && !string.IsNullOrEmpty(storedImageUrl))
            {
                await _fileService.DeleteFileAsync(storedImageUrl, cancellationToken);
            }
            throw;
        }

        var created = await _unitOfWork.Items.GetByIdWithDetailsAsync(item.Id, cancellationToken);
        return Result<ItemResponseDto>.Success(created!.ToResponseDto());
    }
}
