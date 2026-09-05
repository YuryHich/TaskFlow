namespace Domain.Models;

public class Tag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();
}
