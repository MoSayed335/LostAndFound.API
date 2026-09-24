namespace LostAndFound.Application.Common.Models;

public enum ResultErrorType
{
    None,
    Validation,
    Conflict,
    Unauthorized,
    Forbidden,
    NotFound
}

/// <summary>
/// Wraps the outcome of a use case. Expected business failures (duplicate email,
/// wrong password) go through this - they are not exceptional, so they don't use
/// exceptions/the global exception middleware. Unexpected failures still throw and
/// are caught by ExceptionHandlingMiddleware.
/// </summary>
public class Result<T>
{
    public bool Succeeded { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ResultErrorType ErrorType { get; }

    private Result(bool succeeded, T? value, string? error, ResultErrorType errorType)
    {
        Succeeded = succeeded;
        Value = value;
        Error = error;
        ErrorType = errorType;
    }

    public static Result<T> Success(T value) => new(true, value, null, ResultErrorType.None);

    public static Result<T> Failure(string error, ResultErrorType errorType = ResultErrorType.Validation) =>
        new(false, default, error, errorType);
}
