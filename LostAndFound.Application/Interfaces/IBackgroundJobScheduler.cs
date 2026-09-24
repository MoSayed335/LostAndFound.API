using System.Linq.Expressions;

namespace LostAndFound.Application.Interfaces;

public interface IBackgroundJobScheduler
{
    // 1. Fire-and-Forget Job
    string Enqueue<T>(Expression<Action<T>> methodCall);
    string Enqueue<T>(Expression<Func<T, Task>> methodCall);

    // 2. Delayed Job
    string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay);
    string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);

    // 3. Continuation Job
    string ContinueWith<T>(string parentJobId, Expression<Action<T>> methodCall);
    string ContinueWith<T>(string parentJobId, Expression<Func<T, Task>> methodCall);

    // 4. Recurring Job
    void AddOrUpdateRecurring<T>(string recurringJobId, Expression<Func<T, Task>> methodCall, string cronExpression);
    void TriggerRecurring(string recurringJobId);
}