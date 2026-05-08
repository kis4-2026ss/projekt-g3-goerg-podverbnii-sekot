namespace GraderTool.Core.Errors;

public class Result
{
    protected Result(bool isSuccess, GraderError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public GraderError? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(GraderError error) => new(false, error);
}

public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, GraderError? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, null);

    public static new Result<T> Failure(GraderError error) => new(false, default, error);
}
