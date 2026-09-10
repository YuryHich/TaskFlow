namespace Domain.Models;

public class WorkTask
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskState Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? Deadline { get; set; }

    public Project Project { get; set; } = null!;
    public ICollection<User> Assignees { get; set; } = new List<User>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
