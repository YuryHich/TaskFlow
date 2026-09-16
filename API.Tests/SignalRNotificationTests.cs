using System.Net;
using System.Net.Http.Json;
using API.Tests.Infrastructure;
using Application.DTOs;
using Application.Realtime;
using Domain.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace API.Tests;

[Collection(ApiTestCollection.Name)]
public class SignalRNotificationTests
{
    private readonly TaskFlowApiFixture _fixture;

    public SignalRNotificationTests(TaskFlowApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Connect_WithoutToken_Fails()
    {
        await using var connection = HubTestHelper.CreateConnection(_fixture, accessToken: null);

        var exception = await Record.ExceptionAsync(() => connection.StartAsync());

        Assert.NotNull(exception);
        Assert.NotEqual(HubConnectionState.Connected, connection.State);
    }

    [Fact]
    public async Task Connect_WithBearerToken_Succeeds()
    {
        using var http = _fixture.CreateApiClient();
        var user = await AuthHelper.RegisterAndLoginAsync(http, _fixture);

        await using var connection = HubTestHelper.CreateConnection(_fixture, user.AccessToken);
        await connection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, connection.State);
    }

    [Fact]
    public async Task Connect_WithAccessTokenQuery_Succeeds()
    {
        using var http = _fixture.CreateApiClient();
        var user = await AuthHelper.RegisterAndLoginAsync(http, _fixture);

        await using var connection = HubTestHelper.CreateConnection(
            _fixture,
            user.AccessToken,
            tokenInQuery: true);
        await connection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, connection.State);
    }

    [Fact]
    public async Task PutTask_NotifiesOwnerAssigneeAndManager_NotForeignDeveloper()
    {
        var seed = await SeedProjectWithAssigneeAsync();
        using var http = seed.Http;

        await using var ownerHub = HubTestHelper.CreateConnection(_fixture, seed.Owner.AccessToken);
        await using var assigneeHub = HubTestHelper.CreateConnection(_fixture, seed.Assignee.AccessToken);
        await using var managerHub = HubTestHelper.CreateConnection(_fixture, seed.Manager.AccessToken);
        await using var strangerHub = HubTestHelper.CreateConnection(_fixture, seed.Stranger.AccessToken);

        var ownerWait = HubTestHelper.WaitNotifyAsync(ownerHub);
        var assigneeWait = HubTestHelper.WaitNotifyAsync(assigneeHub);
        var managerWait = HubTestHelper.WaitNotifyAsync(managerHub);
        var strangerWait = HubTestHelper.WaitNotifyAsync(strangerHub);

        await ownerHub.StartAsync();
        await assigneeHub.StartAsync();
        await managerHub.StartAsync();
        await strangerHub.StartAsync();

        var put = await http.PutAsJsonAsync($"/api/tasks/{seed.Task.Id}", new UpdateTaskDto
        {
            Title = "updated-task",
            AssigneeIds = [seed.Assignee.UserId]
        });
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        AssertTaskNotify(await ownerWait, "task.updated", seed.Project.Id, seed.Task.Id);
        AssertTaskNotify(await assigneeWait, "task.updated", seed.Project.Id, seed.Task.Id);
        AssertTaskNotify(await managerWait, "task.updated", seed.Project.Id, seed.Task.Id);
        await Assert.ThrowsAsync<TimeoutException>(() => strangerWait);
    }

    [Fact]
    public async Task PostComment_NotifiesAudience_NotForeignDeveloper()
    {
        var seed = await SeedProjectWithAssigneeAsync();
        using var http = seed.Http;

        await using var ownerHub = HubTestHelper.CreateConnection(_fixture, seed.Owner.AccessToken);
        await using var assigneeHub = HubTestHelper.CreateConnection(_fixture, seed.Assignee.AccessToken);
        await using var managerHub = HubTestHelper.CreateConnection(_fixture, seed.Manager.AccessToken);
        await using var strangerHub = HubTestHelper.CreateConnection(_fixture, seed.Stranger.AccessToken);

        var ownerWait = HubTestHelper.WaitNotifyAsync(ownerHub);
        var assigneeWait = HubTestHelper.WaitNotifyAsync(assigneeHub);
        var managerWait = HubTestHelper.WaitNotifyAsync(managerHub);
        var strangerWait = HubTestHelper.WaitNotifyAsync(strangerHub);

        await ownerHub.StartAsync();
        await assigneeHub.StartAsync();
        await managerHub.StartAsync();
        await strangerHub.StartAsync();

        using var assigneeHttp = _fixture.CreateApiClient().WithBearer(seed.Assignee.AccessToken);
        var create = await assigneeHttp.PostAsJsonAsync("/api/comments", new CreateCommentDto
        {
            TaskId = seed.Task.Id,
            Content = "hello"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var comment = await create.Content.ReadFromJsonAsync<CommentDto>();
        Assert.NotNull(comment);

        AssertCommentNotify(await ownerWait, "comment.added", seed.Project.Id, seed.Task.Id, comment.Id);
        AssertCommentNotify(await assigneeWait, "comment.added", seed.Project.Id, seed.Task.Id, comment.Id);
        AssertCommentNotify(await managerWait, "comment.added", seed.Project.Id, seed.Task.Id, comment.Id);
        await Assert.ThrowsAsync<TimeoutException>(() => strangerWait);
    }

    [Fact]
    public async Task PutProject_NotifiesAudience_NotForeignDeveloper()
    {
        var seed = await SeedProjectWithAssigneeAsync();
        using var http = seed.Http;

        await using var ownerHub = HubTestHelper.CreateConnection(_fixture, seed.Owner.AccessToken);
        await using var assigneeHub = HubTestHelper.CreateConnection(_fixture, seed.Assignee.AccessToken);
        await using var managerHub = HubTestHelper.CreateConnection(_fixture, seed.Manager.AccessToken);
        await using var strangerHub = HubTestHelper.CreateConnection(_fixture, seed.Stranger.AccessToken);

        var ownerWait = HubTestHelper.WaitNotifyAsync(ownerHub);
        var assigneeWait = HubTestHelper.WaitNotifyAsync(assigneeHub);
        var managerWait = HubTestHelper.WaitNotifyAsync(managerHub);
        var strangerWait = HubTestHelper.WaitNotifyAsync(strangerHub);

        await ownerHub.StartAsync();
        await assigneeHub.StartAsync();
        await managerHub.StartAsync();
        await strangerHub.StartAsync();

        var put = await http.PutAsJsonAsync($"/api/projects/{seed.Project.Id}", new UpdateProjectDto
        {
            Name = "hub-updated",
            Description = "after"
        });
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        AssertProjectNotify(await ownerWait, "project.updated", seed.Project.Id);
        AssertProjectNotify(await assigneeWait, "project.updated", seed.Project.Id);
        AssertProjectNotify(await managerWait, "project.updated", seed.Project.Id);
        await Assert.ThrowsAsync<TimeoutException>(() => strangerWait);
    }

    [Fact]
    public async Task DeleteProject_NotifiesSnapshottedAudience_NotForeignDeveloper()
    {
        var seed = await SeedProjectWithAssigneeAsync();
        using var http = seed.Http;

        await using var ownerHub = HubTestHelper.CreateConnection(_fixture, seed.Owner.AccessToken);
        await using var assigneeHub = HubTestHelper.CreateConnection(_fixture, seed.Assignee.AccessToken);
        await using var managerHub = HubTestHelper.CreateConnection(_fixture, seed.Manager.AccessToken);
        await using var strangerHub = HubTestHelper.CreateConnection(_fixture, seed.Stranger.AccessToken);

        var ownerWait = HubTestHelper.WaitNotifyAsync(ownerHub);
        var assigneeWait = HubTestHelper.WaitNotifyAsync(assigneeHub);
        var managerWait = HubTestHelper.WaitNotifyAsync(managerHub);
        var strangerWait = HubTestHelper.WaitNotifyAsync(strangerHub);

        await ownerHub.StartAsync();
        await assigneeHub.StartAsync();
        await managerHub.StartAsync();
        await strangerHub.StartAsync();

        var delete = await http.DeleteAsync($"/api/projects/{seed.Project.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        AssertProjectNotify(await ownerWait, "project.deleted", seed.Project.Id);
        AssertProjectNotify(await assigneeWait, "project.deleted", seed.Project.Id);
        AssertProjectNotify(await managerWait, "project.deleted", seed.Project.Id);
        await Assert.ThrowsAsync<TimeoutException>(() => strangerWait);
    }

    private async Task<Seed> SeedProjectWithAssigneeAsync()
    {
        var http = _fixture.CreateApiClient();

        var managerReg = await AuthHelper.RegisterAndLoginAsync(http, _fixture);
        await AuthHelper.SetRoleAsync(_fixture, managerReg.UserId, UserRole.Manager);
        var manager = await AuthHelper.LoginAsync(http, _fixture, managerReg.Email);

        var owner = await AuthHelper.RegisterAndLoginAsync(http, _fixture);
        var assignee = await AuthHelper.RegisterAndLoginAsync(http, _fixture);
        var stranger = await AuthHelper.RegisterAndLoginAsync(http, _fixture);

        http.WithBearer(manager.AccessToken);

        var projectResponse = await http.PostAsJsonAsync("/api/projects", new CreateProjectDto
        {
            Name = "hub-project",
            OwnerId = owner.UserId
        });
        projectResponse.EnsureSuccessStatusCode();
        var project = await projectResponse.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(project);

        var taskResponse = await http.PostAsJsonAsync("/api/tasks", new CreateTaskDto
        {
            ProjectId = project.Id,
            Title = "hub-task",
            AssigneeIds = [assignee.UserId]
        });
        taskResponse.EnsureSuccessStatusCode();
        var task = await taskResponse.Content.ReadFromJsonAsync<TaskDto>();
        Assert.NotNull(task);

        return new Seed(http, manager, owner, assignee, stranger, project, task);
    }

    private static void AssertProjectNotify(RealtimeNotification notification, string eventName, Guid projectId)
    {
        Assert.Equal(eventName, notification.EventName);
        Assert.Equal(projectId, notification.ProjectId);
        Assert.Null(notification.TaskId);
        Assert.Null(notification.CommentId);
    }

    private static void AssertTaskNotify(
        RealtimeNotification notification,
        string eventName,
        Guid projectId,
        Guid taskId)
    {
        Assert.Equal(eventName, notification.EventName);
        Assert.Equal(projectId, notification.ProjectId);
        Assert.Equal(taskId, notification.TaskId);
        Assert.Null(notification.CommentId);
    }

    private static void AssertCommentNotify(
        RealtimeNotification notification,
        string eventName,
        Guid projectId,
        Guid taskId,
        Guid commentId)
    {
        Assert.Equal(eventName, notification.EventName);
        Assert.Equal(projectId, notification.ProjectId);
        Assert.Equal(taskId, notification.TaskId);
        Assert.Equal(commentId, notification.CommentId);
    }

    private sealed record Seed(
        HttpClient Http,
        AuthTokens Manager,
        AuthTokens Owner,
        AuthTokens Assignee,
        AuthTokens Stranger,
        ProjectDto Project,
        TaskDto Task);
}
