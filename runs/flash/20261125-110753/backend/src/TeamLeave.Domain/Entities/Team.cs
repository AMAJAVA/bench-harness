namespace TeamLeave.Domain.Entities;

public class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid ManagerId { get; set; }
    public User Manager { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public ICollection<User> Members { get; set; } = new List<User>();
}
