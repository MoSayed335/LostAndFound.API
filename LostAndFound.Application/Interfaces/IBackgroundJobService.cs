namespace LostAndFound.Application.Interfaces;

public interface IBackgroundJobService
{
    /// <summary>
    /// Fire-and-Forget Job: Evaluates potential matches between the newly reported item and active opposite items
    /// using IMatchingService, and logs/notifies relevant matches.
    /// </summary>
    Task ProcessItemMatchesAsync(int itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delayed Job: Checks if a submitted claim is still pending after a specified duration.
    /// If still pending, logs/sends an urgent reminder to the item owner to review it.
    /// </summary>
    Task CheckPendingClaimReminderAsync(int claimId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recurring Job: Daily maintenance that scans for items in Claimed or Returned status
    /// that are older than 30 days and transitions them to Closed status (auto-archiving).
    /// </summary>
    Task CleanupStaleItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Continuation Job: Executed automatically after ProcessItemMatchesAsync completes.
    /// Compiles match statistics and dispatches a completion summary notification to the item owner.
    /// </summary>
    Task SendItemProcessingSummaryAsync(int itemId, CancellationToken cancellationToken = default);
}
