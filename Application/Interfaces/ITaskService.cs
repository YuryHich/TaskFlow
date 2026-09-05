using Application.DTOs;

namespace Application.Interfaces;

public interface ITaskService
{
    Task<IEnumerable<TaskDto>> GetTasksAsync();
    Task<TaskDto?> GetTaskByIdAsync(Guid id);
    Task<IEnumerable<CommentDto>> GetTaskCommentsAsync(Guid taskId);
    Task<TaskDto> CreateTaskAsync(CreateTaskDto task);
    Task UpdateTaskAsync(Guid id, UpdateTaskDto task);
    Task DeleteTaskAsync(Guid id);
}