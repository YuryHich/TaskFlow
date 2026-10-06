namespace Analytics.Persistence;

public sealed class ProcessedEvent
{
    public Guid EventId { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public sealed class ProjectFact
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Deleted { get; set; }
}

public sealed class TaskFact
{
    public Guid TaskId { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "New";
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool Deleted { get; set; }
}
