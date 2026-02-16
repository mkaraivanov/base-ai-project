namespace Domain.Common;

public class Result
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    public List<string> Errors { get; init; } = [];

    protected Result(bool isSuccess, string? error = null, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors ?? [];
    }

    public static Result Success() => new(true);
    public static Result Failure(string error) => new(false, error);
    public static Result Failure(List<string> errors) => new(false, errors: errors);
}

public class Result<T> : Result
{
    public T? Value { get; init; }

    private Result(bool isSuccess, T? value = default, string? error = null, List<string>? errors = null)
        : base(isSuccess, error, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value);
    public static new Result<T> Failure(string error) => new(false, default, error);
    public static new Result<T> Failure(List<string> errors) => new(false, default, errors: errors);
}
