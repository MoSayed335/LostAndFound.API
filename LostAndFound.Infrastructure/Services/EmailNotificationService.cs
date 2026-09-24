using LostAndFound.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace LostAndFound.Infrastructure.Services;

public class EmailNotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(IUnitOfWork unitOfWork, ILogger<EmailNotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task SendItemCreatedNotificationAsync(int itemId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.Items.GetByIdWithDetailsAsync(itemId, cancellationToken);
        if (item is null)
        {
            _logger.LogWarning("[Notification] Cannot send item created notification: Item #{ItemId} not found.", itemId);
            return;
        }

        var recipientEmail = item.User?.Email ?? $"user_{item.UserId}@lostandfound.local";
        _logger.LogInformation(
            "[Notification] Email sent to {RecipientEmail}: Your {ItemType} item '{Title}' has been successfully published.",
            recipientEmail, item.Type, item.Title);
    }

    public async Task SendClaimReminderNotificationAsync(int claimId, CancellationToken cancellationToken = default)
    {
        var claim = await _unitOfWork.Claims.GetByIdWithItemAndClaimantAsync(claimId, cancellationToken);
        if (claim is null)
        {
            _logger.LogWarning("[Notification] Cannot send claim reminder: Claim #{ClaimId} not found.", claimId);
            return;
        }

        var ownerEmail = claim.Item?.User?.Email ?? $"owner_item_{claim.ItemId}@lostandfound.local";
        _logger.LogInformation(
            "[Notification] Urgent Reminder sent to item owner ({OwnerEmail}): Claim #{ClaimId} on item '{ItemTitle}' is awaiting your review.",
            ownerEmail, claim.Id, claim.Item?.Title);
    }

    public async Task SendItemMatchesNotificationAsync(int itemId, int matchCount, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.Items.GetByIdWithDetailsAsync(itemId, cancellationToken);
        if (item is null)
        {
            _logger.LogWarning("[Notification] Cannot send match summary notification: Item #{ItemId} not found.", itemId);
            return;
        }

        var recipientEmail = item.User?.Email ?? $"user_{item.UserId}@lostandfound.local";
        _logger.LogInformation(
            "[Notification] Match alert email sent to {RecipientEmail}: We found {MatchCount} potential matches for your {ItemType} item '{Title}'.",
            recipientEmail, matchCount, item.Type, item.Title);
    }
}