using Domain.Models;

namespace Domain.Repositories;

public interface ITaskRepository
{
    Task<IEnumerable<WorkTask>> GetTasksAsync();
    Task<WorkTask?> GetTaskByIdAsync(Guid id);
    Task<IEnumerable<Comment>> GetTaskCommentsAsync(Guid taskId);
    Task CreateTaskAsync(WorkTask task);
    Task UpdateTaskAsync(WorkTask task);
    Task DeleteTaskAsync(Guid id);
}
