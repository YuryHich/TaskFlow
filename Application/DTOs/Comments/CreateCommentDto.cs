namespace Application.DTOs;

public class CreateCommentDto
{
    public Guid TaskId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
}
