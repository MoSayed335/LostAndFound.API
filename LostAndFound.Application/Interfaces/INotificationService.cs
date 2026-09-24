namespace LostAndFound.Application.Interfaces;

public interface INotificationService
{
    Task SendItemCreatedNotificationAsync(int itemId, CancellationToken cancellationToken = default);
    Task SendClaimReminderNotificationAsync(int claimId, CancellationToken cancellationToken = default);
    Task SendItemMatchesNotificationAsync(int itemId, int matchCount, CancellationToken cancellationToken = default);
}