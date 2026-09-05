using Domain.Models;
using Domain.Repositories;

namespace Infrastructure.Repositories;

public class InMemoryTaskRepository : ITaskRepository
{
    private readonly List<WorkTask> _tasks = [];

    public Task<IEnumerable<WorkTask>> GetTasksAsync()
    {
        return Task.FromResult<IEnumerable<WorkTask>>(_tasks.ToList());
    }

    public Task<WorkTask?> GetTaskByIdAsync(Guid id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        return Task.FromResult(task);
    }

    public Task<IEnumerable<Comment>> GetTaskCommentsAsync(Guid taskId)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId);
        IEnumerable<Comment> comments = task?.Comments.ToList() ?? [];
        return Task.FromResult(comments);
    }

    public Task CreateTaskAsync(WorkTask task)
    {
        _tasks.Add(task);
        return Task.CompletedTask;
    }

    public Task UpdateTaskAsync(WorkTask task)
    {
        var existingTask = _tasks.FirstOrDefault(t => t.Id == task.Id);
        if (existingTask is not null)
        {
            existingTask.ProjectId = task.ProjectId;
            existingTask.AssigneeId = task.AssigneeId;
            existingTask.Title = task.Title;
            existingTask.Description = task.Description;
            existingTask.Status = task.Status;
            existingTask.Priority = task.Priority;
            existingTask.Deadline = task.Deadline;
        }

        return Task.CompletedTask;
    }

    public Task DeleteTaskAsync(Guid id)
    {
        _tasks.RemoveAll(t => t.Id == id);
        return Task.CompletedTask;
    }
}