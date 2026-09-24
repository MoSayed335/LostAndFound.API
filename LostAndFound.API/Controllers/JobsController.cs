using Hangfire;
using LostAndFound.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.API.Controllers;

/// <summary>
/// Provides demonstration endpoints for all 4 primary Hangfire background processing job types:
/// Fire-and-Forget, Delayed, Recurring, and Continuation.
/// All jobs execute against real domain services and repositories via IBackgroundJobService.
/// </summary>
[Route("api/jobs")]
public class JobsController : ApiControllerBase
{
    private readonly IBackgroundJobScheduler _scheduler;

    public JobsController(IBackgroundJobScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    /// <summary>
    /// 1. Fire-and-Forget Job:
    /// Enqueues background matching analysis for the specified item.
    /// Runs immediately in the background on the Hangfire server without blocking the caller.
    /// </summary>
    [HttpPost("fire-and-forget/{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult TriggerFireAndForgetJob(int itemId)
    {
        var jobId = BackgroundJob.Enqueue<IBackgroundJobService>(
            service => service.ProcessItemMatchesAsync(itemId, CancellationToken.None));

        return Accepted(new
        {
            jobId,
            jobType = "Fire-and-Forget",
            description = $"Enqueued automated match evaluation for Item #{itemId}.",
            dashboardUrl = "/hangfire"
        });
    }

    /// <summary>
    /// 2. Delayed Job:
    /// Schedules a reminder check for a pending claim after a specified delay (default: 30 seconds for quick testing).
    /// Once the delay expires, Hangfire evaluates whether the claim is still pending and sends an owner reminder.
    /// </summary>
    [HttpPost("delayed/{claimId:int}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult TriggerDelayedJob(int claimId, [FromQuery] int delayInSeconds = 30)
    {
        var delay = TimeSpan.FromSeconds(Math.Max(5, delayInSeconds));

        var jobId = BackgroundJob.Schedule<IBackgroundJobService>(
            service => service.CheckPendingClaimReminderAsync(claimId, CancellationToken.None),
            delay);

        return Accepted(new
        {
            jobId,
            jobType = "Delayed",
            delaySeconds = (int)delay.TotalSeconds,
            description = $"Scheduled pending status review for Claim #{claimId} in {delay.TotalSeconds} seconds.",
            dashboardUrl = "/hangfire"
        });
    }

    /// <summary>
    /// 3. Recurring Job:
    /// Triggers an immediate execution of the daily 'stale-items-cleanup' maintenance job,
    /// which scans and auto-archives items that have been Claimed or Returned for > 30 days.
    /// </summary>
    [HttpPost("recurring/cleanup-stale-items/trigger")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult TriggerRecurringJob()
    {
        const string recurringJobId = "stale-items-cleanup";

        // Ensure registration is present
        RecurringJob.AddOrUpdate<IBackgroundJobService>(
            recurringJobId,
            service => service.CleanupStaleItemsAsync(CancellationToken.None),
            Cron.Daily);

        // Trigger immediately for demonstration
        RecurringJob.TriggerJob(recurringJobId);

        return Ok(new
        {
            recurringJobId,
            jobType = "Recurring",
            schedule = "Daily (Cron.Daily)",
            description = "Triggered immediate execution of daily stale items archive maintenance job.",
            dashboardUrl = "/hangfire"
        });
    }

    /// <summary>
    /// 4. Continuation Job:
    /// Enqueues a two-stage background pipeline:
    /// Stage 1 (Parent): Evaluates item matches asynchronously.
    /// Stage 2 (Continuation): Chains automatically upon Stage 1 success to compile and send a summary notification.
    /// </summary>
    [HttpPost("continuation/{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult TriggerContinuationJob(int itemId)
    {
        // Stage 1: Parent Job
        var parentJobId = BackgroundJob.Enqueue<IBackgroundJobService>(
            service => service.ProcessItemMatchesAsync(itemId, CancellationToken.None));

        // Stage 2: Continuation Job (triggers strictly after parentJobId succeeds)
        var continuationJobId = BackgroundJob.ContinueJobWith<IBackgroundJobService>(
            parentJobId,
            service => service.SendItemProcessingSummaryAsync(itemId, CancellationToken.None));

        return Accepted(new
        {
            jobType = "Continuation",
            parentJob = new
            {
                jobId = parentJobId,
                action = $"ProcessItemMatchesAsync for Item #{itemId}"
            },
            continuationJob = new
            {
                jobId = continuationJobId,
                action = $"SendItemProcessingSummaryAsync for Item #{itemId}"
            },
            description = "Two-stage continuation pipeline enqueued. Stage 2 executes immediately after Stage 1 finishes.",
            dashboardUrl = "/hangfire"
        });
    }
}
