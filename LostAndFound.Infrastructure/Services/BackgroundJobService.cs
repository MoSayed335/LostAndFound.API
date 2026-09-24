using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LostAndFound.Infrastructure.Services;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMatchingService _matchingService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<BackgroundJobService> _logger;

    public BackgroundJobService(
        IUnitOfWork unitOfWork,
        IMatchingService matchingService,
        INotificationService notificationService,
        ILogger<BackgroundJobService> logger)
    {
        _unitOfWork = unitOfWork;
        _matchingService = matchingService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// 1. Fire-and-Forget Job:
    /// Runs asynchronously in the background when a new item is created or updated.
    /// Finds candidate matches of the opposite type (Lost vs. Found) using IMatchingService,
    /// without blocking the caller or HTTP request thread.
    /// </summary>
    public async Task ProcessItemMatchesAsync(int itemId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Hangfire Fire-and-Forget] Starting match processing for Item #{ItemId}...", itemId);

        var item = await _unitOfWork.Items.GetByIdWithDetailsAsync(itemId, asNoTracking: true, cancellationToken);
        if (item is null)
        {
            _logger.LogWarning("[Hangfire Fire-and-Forget] Item #{ItemId} not found. Aborting match processing.", itemId);
            return;
        }

        if (item.Status != ItemStatus.Active)
        {
            _logger.LogInformation("[Hangfire Fire-and-Forget] Item #{ItemId} is {Status} (not Active). Skipping match processing.", itemId, item.Status);
            return;
        }

        var oppositeType = item.Type == ItemType.Lost ? ItemType.Found : ItemType.Lost;
        var candidates = await _unitOfWork.Items.GetActiveItemsByTypeAsync(oppositeType, item.Id, cancellationToken);

        var matches = _matchingService.FindMatches(item, candidates);

        _logger.LogInformation(
            "[Hangfire Fire-and-Forget] Finished matching for Item #{ItemId} ('{Title}'). Evaluated {CandidateCount} candidates, found {MatchCount} match(es).",
            item.Id, item.Title, candidates.Count, matches.Count);

        if (matches.Count > 0)
        {
            var topMatch = matches[0];
            _logger.LogInformation(
                "[Hangfire Fire-and-Forget] Top match for Item #{ItemId}: Candidate #{CandidateId} ('{CandidateTitle}') with Match Score: {Score}%",
                item.Id, topMatch.ItemId, topMatch.Title, topMatch.MatchScore);
        }
    }

    /// <summary>
    /// 2. Delayed Job:
    /// Scheduled with a time delay (e.g. 24 hours, or testable duration).
    /// Inspects whether a submitted claim has remained unreviewed (Pending).
    /// If so, dispatches an urgent reminder notification to the item owner.
    /// </summary>
    public async Task CheckPendingClaimReminderAsync(int claimId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Hangfire Delayed Job] Evaluating pending claim reminder for Claim #{ClaimId}...", claimId);

        var claim = await _unitOfWork.Claims.GetByIdWithItemAndClaimantAsync(claimId, cancellationToken);
        if (claim is null)
        {
            _logger.LogWarning("[Hangfire Delayed Job] Claim #{ClaimId} not found.", claimId);
            return;
        }

        if (claim.Status != ClaimStatus.Pending)
        {
            _logger.LogInformation(
                "[Hangfire Delayed Job] Claim #{ClaimId} is already resolved (Status: {Status}). No reminder needed.",
                claimId, claim.Status);
            return;
        }

        _logger.LogWarning(
            "[Hangfire Delayed Job] Claim #{ClaimId} on item '{ItemTitle}' (submitted by Claimant #{ClaimantId}) is still PENDING after the grace period. Sending reminder.",
            claim.Id, claim.Item?.Title, claim.ClaimantId);

        await _notificationService.SendClaimReminderNotificationAsync(claimId, cancellationToken);
    }

    /// <summary>
    /// 3. Recurring Job:
    /// Runs on a periodic schedule (e.g., daily at midnight).
    /// Auto-archives resolved items (Claimed or Returned) older than 30 days into Closed status.
    /// </summary>
    public async Task CleanupStaleItemsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Hangfire Recurring Job] Starting daily maintenance: archiving stale claimed/returned items...");

        var cutoffDate = DateTime.UtcNow.AddDays(-30);

        var staleItems = _unitOfWork.Items.GetQueryable(asNoTracking: false)
            .Where(i => (i.Status == ItemStatus.Claimed || i.Status == ItemStatus.Returned) &&
                        (i.UpdatedAt != null ? i.UpdatedAt < cutoffDate : i.CreatedAt < cutoffDate))
            .ToList();

        if (staleItems.Count == 0)
        {
            _logger.LogInformation("[Hangfire Recurring Job] Maintenance check complete: No stale items found to archive.");
            return;
        }

        foreach (var item in staleItems)
        {
            item.Status = ItemStatus.Closed;
            item.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Items.Update(item);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[Hangfire Recurring Job] Maintenance complete: Successfully transitioned {Count} stale items to Closed status.",
            staleItems.Count);
    }

    /// <summary>
    /// 4. Continuation Job:
    /// Automatically executed only after the parent ProcessItemMatchesAsync job finishes successfully.
    /// Aggregates match statistics and dispatches a notification/email to the item owner.
    /// </summary>
    public async Task SendItemProcessingSummaryAsync(int itemId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Hangfire Continuation Job] Running post-processing continuation pipeline for Item #{ItemId}...", itemId);

        var item = await _unitOfWork.Items.GetByIdWithDetailsAsync(itemId, asNoTracking: true, cancellationToken);
        if (item is null)
        {
            _logger.LogWarning("[Hangfire Continuation Job] Item #{ItemId} not found. Continuation aborted.", itemId);
            return;
        }

        var oppositeType = item.Type == ItemType.Lost ? ItemType.Found : ItemType.Lost;
        var candidates = await _unitOfWork.Items.GetActiveItemsByTypeAsync(oppositeType, item.Id, cancellationToken);
        var matches = _matchingService.FindMatches(item, candidates);

        await _notificationService.SendItemMatchesNotificationAsync(itemId, matches.Count, cancellationToken);

        _logger.LogInformation(
            "[Hangfire Continuation Job] Continuation pipeline completed for Item #{ItemId}. Summary notification dispatched (matches found: {MatchCount}).",
            itemId, matches.Count);
    }
}
