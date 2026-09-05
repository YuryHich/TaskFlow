namespace Domain.Models;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<Project> OwnedProjects { get; set; } = new List<Project>();
    public ICollection<WorkTask> AssignedTasks { get; set; } = new List<WorkTask>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
