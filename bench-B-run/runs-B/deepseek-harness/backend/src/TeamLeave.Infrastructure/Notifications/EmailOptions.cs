namespace TeamLeave.Infrastructure.Notifications;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }
    public string From { get; set; } = "no-reply@teamleave.example";
    public string AppBaseUrl { get; set; } = "https://app.teamleave.example";
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public int MaxAttempts { get; set; } = 5;
    public int BatchSize { get; set; } = 20;
}
