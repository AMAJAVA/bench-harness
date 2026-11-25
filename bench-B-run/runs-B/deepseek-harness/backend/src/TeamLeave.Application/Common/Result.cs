namespace TeamLeave.Application.Common;

public sealed class Result<T>
{
    private Result(T? value, AppError? error)
    {
        Value = value;
        Error = error;
    }

    public bool IsSuccess => Error is null;
    public T? Value { get; }
    public AppError? Error { get; }

    public static Result<T> Ok(T value) => new(value, null);
    public static Result<T> Fail(AppError error) => new(default, error);
    public static Result<T> Fail(string code, string title, int status, string detail) =>
        new(default, AppError.Create(code, title, status, detail));
}
