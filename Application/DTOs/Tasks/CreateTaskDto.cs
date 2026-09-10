using Domain.Models;

namespace Application.DTOs;

public class CreateTaskDto
{
    public Guid ProjectId { get; set; }
    public IReadOnlyList<Guid> AssigneeIds { get; set; } = [];
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskState Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? Deadline { get; set; }
}
