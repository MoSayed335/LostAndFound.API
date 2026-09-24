using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LostAndFound.Application.Features.Items.Commands.DeleteItem;

public class DeleteItemCommandHandler : IRequestHandler<DeleteItemCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileService _fileService;
    private readonly ILogger<DeleteItemCommandHandler> _logger;

    public DeleteItemCommandHandler(
        IUnitOfWork unitOfWork,
        IFileService fileService,
        ILogger<DeleteItemCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _fileService = fileService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _unitOfWork.Items.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (item is null)
        {
            return Result<bool>.Failure("Item not found.", ResultErrorType.NotFound);
        }

        // Only owner or Admin can delete
        if (!request.IsAdmin && item.UserId != request.CurrentUserId)
        {
            _logger.LogWarning("Unauthorized attempt to delete item {ItemId} by user {UserId}", request.Id, request.CurrentUserId);
            return Result<bool>.Failure("You are not authorized to delete this item.", ResultErrorType.Forbidden);
        }

        string? imageUrl = item.ImageUrl;

        // Cleanly and explicitly remove attached claims first to avoid cascade-delete anomalies
        if (item.Claims.Count != 0)
        {
            _unitOfWork.Items.RemoveClaims(item.Claims);
        }

        _unitOfWork.Items.Delete(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Item {ItemId} successfully deleted by user {UserId}", request.Id, request.CurrentUserId);

        if (!string.IsNullOrEmpty(imageUrl))
        {
            await _fileService.DeleteFileAsync(imageUrl, cancellationToken);
        }

        return Result<bool>.Success(true);
    }
}
