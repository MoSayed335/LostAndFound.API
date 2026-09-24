using System.Linq.Expressions;
using Hangfire;
using LostAndFound.Application.Interfaces;

namespace LostAndFound.Infrastructure.Services;

public class HangfireBackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;

    public HangfireBackgroundJobScheduler(
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager)
    {
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
    }

    public string Enqueue<T>(Expression<Action<T>> methodCall) =>
        _backgroundJobClient.Enqueue<T>(methodCall);

    public string Enqueue<T>(Expression<Func<T, Task>> methodCall) =>
        _backgroundJobClient.Enqueue<T>(methodCall);

    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay) =>
        _backgroundJobClient.Schedule<T>(methodCall, delay);

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay) =>
        _backgroundJobClient.Schedule<T>(methodCall, delay);

    public string ContinueWith<T>(string parentJobId, Expression<Action<T>> methodCall) =>
        _backgroundJobClient.ContinueJobWith<T>(parentJobId, methodCall);

    public string ContinueWith<T>(string parentJobId, Expression<Func<T, Task>> methodCall) =>
        _backgroundJobClient.ContinueJobWith<T>(parentJobId, methodCall);

    public void AddOrUpdateRecurring<T>(string recurringJobId, Expression<Func<T, Task>> methodCall, string cronExpression) =>
        _recurringJobManager.AddOrUpdate<T>(recurringJobId, methodCall, cronExpression);

    public void TriggerRecurring(string recurringJobId) =>
        _recurringJobManager.Trigger(recurringJobId);
}