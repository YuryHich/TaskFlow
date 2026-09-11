using System.Net;
using System.Net.Http.Json;
using API.Tests.Infrastructure;
using Application.DTOs;
using Application.Interfaces;
using Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.Tests;

[Collection(ApiTestCollection.Name)]
public class ProjectAccessTests
{
    private readonly TaskFlowApiFixture _fixture;

    public ProjectAccessTests(TaskFlowApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MultipleAssignees_CanEachGetTheTask()
    {
        var manager = await CreateManagerClientAsync();
        var first = await AuthHelper.RegisterAndLoginAsync(_fixture.CreateClient(), _fixture);
        var second = await AuthHelper.RegisterAndLoginAsync(_fixture.CreateClient(), _fixture);
        var project = await CreateProjectAsync(manager);

        var createTask = await manager.PostAsJsonAsync("/api/tasks", new CreateTaskDto
        {
            ProjectId = project.Id,
            AssigneeIds = [first.UserId, second.UserId],
            Title = "Shared task",
            Description = "two assignees",
            Status = TaskState.New,
            Priority = TaskPriority.Medium
        });
        Assert.Equal(HttpStatusCode.Created, createTask.StatusCode);
        var task = await createTask.Content.ReadFromJsonAsync<TaskDto>();
        Assert.NotNull(task);
        Assert.Equal(2, task.AssigneeIds.Count);

        using var firstClient = _fixture.CreateClient().WithBearer(first.AccessToken);
        using var secondClient = _fixture.CreateClient().WithBearer(second.AccessToken);

        var firstGet = await firstClient.GetAsync($"/api/tasks/{task.Id}");
        var secondGet = await secondClient.GetAsync($"/api/tasks/{task.Id}");
        Assert.Equal(HttpStatusCode.OK, firstGet.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondGet.StatusCode);
    }

    [Fact]
    public async Task UnassignFromLastTask_RemovesProjectReadAccess()
    {
        var manager = await CreateManagerClientAsync();
        var assignee = await AuthHelper.RegisterAndLoginAsync(_fixture.CreateClient(), _fixture);
        var project = await CreateProjectAsync(manager);

        var createTask = await manager.PostAsJsonAsync("/api/tasks", new CreateTaskDto
        {
            ProjectId = project.Id,
            AssigneeIds = [assignee.UserId],
            Title = "Only assignment",
            Description = null,
            Status = TaskState.New,
            Priority = TaskPriority.Medium
        });
        Assert.Equal(HttpStatusCode.Created, createTask.StatusCode);
        var task = await createTask.Content.ReadFromJsonAsync<TaskDto>();
        Assert.NotNull(task);

        using var assigneeClient = _fixture.CreateClient().WithBearer(assignee.AccessToken);
        var before = await assigneeClient.GetAsync($"/api/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);

        var unassign = await manager.PutAsJsonAsync($"/api/tasks/{task.Id}", new UpdateTaskDto
        {
            AssigneeIds = [],
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority
        });
        Assert.Equal(HttpStatusCode.NoContent, unassign.StatusCode);

        var list = await assigneeClient.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var projects = await list.Content.ReadFromJsonAsync<ProjectDto[]>();
        Assert.NotNull(projects);
        Assert.DoesNotContain(projects, p => p.Id == project.Id);

        var after = await assigneeClient.GetAsync($"/api/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, after.StatusCode);

        var getTask = await assigneeClient.GetAsync($"/api/tasks/{task.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, getTask.StatusCode);
    }

    [Fact]
    public async Task Admin_SeesForeignProjectInList()
    {
        var manager = await CreateManagerClientAsync();
        var project = await CreateProjectAsync(manager);

        var adminTokens = await AuthHelper.RegisterAndLoginAsync(_fixture.CreateClient(), _fixture);
        await AuthHelper.SetRoleAsync(_fixture, adminTokens.UserId, UserRole.Admin);
        adminTokens = await AuthHelper.LoginAsync(
            _fixture.CreateClient(),
            _fixture,
            adminTokens.Email,
            TaskFlowApiFixture.TestPassword);

        using var adminClient = _fixture.CreateClient().WithBearer(adminTokens.AccessToken);
        var list = await adminClient.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var projects = await list.Content.ReadFromJsonAsync<ProjectDto[]>();
        Assert.NotNull(projects);
        Assert.Contains(projects, p => p.Id == project.Id);
    }

    [Fact]
    public async Task Assignee_CannotDeleteProject()
    {
        var manager = await CreateManagerClientAsync();
        var assignee = await AuthHelper.RegisterAndLoginAsync(_fixture.CreateClient(), _fixture);
        var project = await CreateProjectAsync(manager);

        var createTask = await manager.PostAsJsonAsync("/api/tasks", new CreateTaskDto
        {
            ProjectId = project.Id,
            AssigneeIds = [assignee.UserId],
            Title = "Assigned",
            Status = TaskState.New,
            Priority = TaskPriority.Medium
        });
        createTask.EnsureSuccessStatusCode();

        using var assigneeClient = _fixture.CreateClient().WithBearer(assignee.AccessToken);
        var deleteProject = await assigneeClient.DeleteAsync($"/api/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, deleteProject.StatusCode);
    }

    [Fact]
    public async Task MissingTask_ReturnsNotFound()
    {
        var tokens = await AuthHelper.RegisterAndLoginAsync(_fixture.CreateClient(), _fixture);
        using var client = _fixture.CreateClient().WithBearer(tokens.AccessToken);
        var get = await client.GetAsync($"/api/tasks/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task ProjectAudience_ContainsOwnerAssigneesAndStaff()
    {
        var manager = await CreateManagerClientAsync();
        var assignee = await AuthHelper.RegisterAndLoginAsync(_fixture.CreateClient(), _fixture);
        var otherDev = await AuthHelper.RegisterAndLoginAsync(_fixture.CreateClient(), _fixture);
        var adminTokens = await AuthHelper.RegisterAndLoginAsync(_fixture.CreateClient(), _fixture);
        await AuthHelper.SetRoleAsync(_fixture, adminTokens.UserId, UserRole.Admin);

        var project = await CreateProjectAsync(manager);
        var createTask = await manager.PostAsJsonAsync("/api/tasks", new CreateTaskDto
        {
            ProjectId = project.Id,
            AssigneeIds = [assignee.UserId],
            Title = "Audience task",
            Status = TaskState.New,
            Priority = TaskPriority.Medium
        });
        createTask.EnsureSuccessStatusCode();

        using var scope = _fixture.Services.CreateScope();
        var audience = scope.ServiceProvider.GetRequiredService<IProjectAudience>();
        var ids = await audience.GetUserIdsAsync(project.Id);

        Assert.Contains(project.OwnerId, ids);
        Assert.Contains(assignee.UserId, ids);
        Assert.Contains(adminTokens.UserId, ids);
        Assert.DoesNotContain(otherDev.UserId, ids);
        Assert.Equal(ids.Distinct().Count(), ids.Count);
    }

    private async Task<HttpClient> CreateManagerClientAsync()
    {
        var tokens = await AuthHelper.RegisterAndLoginAsync(_fixture.CreateClient(), _fixture);
        await AuthHelper.SetRoleAsync(_fixture, tokens.UserId, UserRole.Manager);
        tokens = await AuthHelper.LoginAsync(
            _fixture.CreateClient(),
            _fixture,
            tokens.Email,
            TaskFlowApiFixture.TestPassword);
        return _fixture.CreateClient().WithBearer(tokens.AccessToken);
    }

    private static async Task<ProjectDto> CreateProjectAsync(HttpClient manager)
    {
        var create = await manager.PostAsJsonAsync("/api/projects", new CreateProjectDto
        {
            Name = $"Access {Guid.NewGuid():N}"[..20],
            Description = "slice-a"
        });
        create.EnsureSuccessStatusCode();
        var project = await create.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(project);
        return project;
    }
}

