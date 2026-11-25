namespace TeamLeave.Application.Common;

public sealed record AppError(
    string Code,
    string Title,
    int Status,
    string Detail,
    IReadOnlyDictionary<string, object?>? Extensions = null)
{
    public static AppError Create(string code, string title, int status, string detail) =>
        new(code, title, status, detail);

    public AppError With(string key, object? value)
    {
        var extensions = Extensions is null
            ? new Dictionary<string, object?>(StringComparer.Ordinal)
            : new Dictionary<string, object?>(Extensions, StringComparer.Ordinal);
        extensions[key] = value;
        return this with { Extensions = extensions };
    }
}
