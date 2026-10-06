namespace Application.Streaming;

public static class StreamEventTypes
{
    public const string ProjectCreated = "project.created";
    public const string ProjectUpdated = "project.updated";
    public const string ProjectDeleted = "project.deleted";
    public const string TaskCreated = "task.created";
    public const string TaskUpdated = "task.updated";
    public const string TaskDeleted = "task.deleted";
    public const string CommentAdded = "comment.added";
    public const string CommentUpdated = "comment.updated";
    public const string CommentDeleted = "comment.deleted";
}
