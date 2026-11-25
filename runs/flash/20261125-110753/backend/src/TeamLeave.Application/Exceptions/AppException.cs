namespace TeamLeave.Application.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }
    public object? Errors { get; }

    public AppException(int statusCode, string code, string message, object? errors = null) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Errors = errors;
    }
}
