namespace Domain.Models;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid OwnerId { get; set; }

    public User Owner { get; set; } = null!;
    public ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();
}
