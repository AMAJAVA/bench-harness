namespace TeamLeave.Domain.Entities;

public class Holiday
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
