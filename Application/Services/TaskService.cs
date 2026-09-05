using Application.DTOs;
using Application.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Mapster;

namespace Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;

    public TaskService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<IEnumerable<TaskDto>> GetTasksAsync()
    {
        var tasks = await _taskRepository.GetTasksAsync();
        return tasks.Adapt<IEnumerable<TaskDto>>();
    }

    public async Task<TaskDto?> GetTaskByIdAsync(Guid id)
    {
        var task = await _taskRepository.GetTaskByIdAsync(id);
        return task?.Adapt<TaskDto>();
    }

    public async Task<IEnumerable<CommentDto>> GetTaskCommentsAsync(Guid taskId)
    {
        var comments = await _taskRepository.GetTaskCommentsAsync(taskId);
        return comments.Adapt<IEnumerable<CommentDto>>();
    }

    public async Task<TaskDto> CreateTaskAsync(CreateTaskDto task)
    {
        var taskEntity = task.Adapt<WorkTask>();
        taskEntity.Id = Guid.NewGuid();
        taskEntity.CreatedAt = DateTime.UtcNow;

        await _taskRepository.CreateTaskAsync(taskEntity);
        return taskEntity.Adapt<TaskDto>();
    }

    public async Task UpdateTaskAsync(Guid id, UpdateTaskDto task)
    {
        var taskEntity = await _taskRepository.GetTaskByIdAsync(id);
        if (taskEntity is null)
        {
            return;
        }

        task.Adapt(taskEntity);
        await _taskRepository.UpdateTaskAsync(taskEntity);
    }

    public async Task DeleteTaskAsync(Guid id)
    {
        await _taskRepository.DeleteTaskAsync(id);
    }
}