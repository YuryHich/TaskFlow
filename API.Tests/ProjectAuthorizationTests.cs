using System.Net;
using System.Net.Http.Json;
using API.Tests.Infrastructure;
using Application.DTOs;
using Domain.Models;
using Xunit;

namespace API.Tests;

[Collection(ApiTestCollection.Name)]
public class ProjectAuthorizationTests
{
    private readonly TaskFlowApiFixture _fixture;

    public ProjectAuthorizationTests(TaskFlowApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Admin_CanCreateProject()
    {
        using var client = _fixture.CreateClient();

        var adminTokens = await AuthHelper.RegisterAndLoginAsync(
            client,
            _fixture,
            password: TaskFlowApiFixture.TestPassword);

        await AuthHelper.SetRoleAsync(_fixture, adminTokens.UserId, UserRole.Admin);
        var newTokens = await AuthHelper.LoginAsync(client, _fixture, adminTokens.Email, TaskFlowApiFixture.TestPassword);

        using var adminClient = _fixture.CreateClient().WithBearer(newTokens.AccessToken);

        var createProject = await adminClient.PostAsJsonAsync("/api/projects", new CreateProjectDto
        {
            Name = "Sprint 3 Test",
            Description = "authz"
        });

        Assert.Equal(HttpStatusCode.Created, createProject.StatusCode);

        var createdProject = await createProject.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(createdProject);
        Assert.NotEqual(Guid.Empty, createdProject.Id);
    }

    [Fact]
    public async Task Manager_CanCreateProject()
    {
        using var client = _fixture.CreateClient();

        var managerTokens = await AuthHelper.RegisterAndLoginAsync(
            client,
            _fixture,
            password: TaskFlowApiFixture.TestPassword);

        await AuthHelper.SetRoleAsync(_fixture, managerTokens.UserId, UserRole.Manager);
        var newTokens = await AuthHelper.LoginAsync(client, _fixture, managerTokens.Email, TaskFlowApiFixture.TestPassword);

        using var managerClient = _fixture.CreateClient().WithBearer(newTokens.AccessToken);

        var createProject = await managerClient.PostAsJsonAsync("/api/projects", new CreateProjectDto
        {
            Name = "Sprint 3 Test",
            Description = "authz"
        });

        Assert.Equal(HttpStatusCode.Created, createProject.StatusCode);

        var createdProject = await createProject.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(createdProject);
        Assert.NotEqual(Guid.Empty, createdProject.Id);
    }

    [Fact]
    public async Task Developer_CannotCreateProject()
    {
        using var client = _fixture.CreateClient();

        var userTokens = await AuthHelper.RegisterAndLoginAsync(
            client,
            _fixture,
            password: TaskFlowApiFixture.TestPassword);

        using var userClient = _fixture.CreateClient().WithBearer(userTokens.AccessToken);

        var createProject = await userClient.PostAsJsonAsync("/api/projects", new CreateProjectDto
        {
            Name = "Sprint 3 Test",
            Description = "authz"
        });

        Assert.Equal(HttpStatusCode.Forbidden, createProject.StatusCode);
    }

    [Fact]
    public async Task User_CannotAccessForeignProject_ById()
    {
        using var managerClient = _fixture.CreateClient();

        var managerTokens = await AuthHelper.RegisterAndLoginAsync(
            managerClient,
            _fixture,
            password: TaskFlowApiFixture.TestPassword);

        await AuthHelper.SetRoleAsync(_fixture, managerTokens.UserId, UserRole.Manager);
        var managerLoggedInTokens = await AuthHelper.LoginAsync(
            managerClient,
            _fixture,
            managerTokens.Email,
            TaskFlowApiFixture.TestPassword);

        using var managerAuthorizedClient = _fixture.CreateClient().WithBearer(managerLoggedInTokens.AccessToken);

        var createProject = await managerAuthorizedClient.PostAsJsonAsync("/api/projects", new CreateProjectDto
        {
            Name = "Sprint 3 Test",
            Description = "authz"
        });

        Assert.Equal(HttpStatusCode.Created, createProject.StatusCode);

        var createdProject = await createProject.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(createdProject);
        Assert.NotEqual(Guid.Empty, createdProject.Id);

        using var foreignClient = _fixture.CreateClient();

        var foreignTokens = await AuthHelper.RegisterAndLoginAsync(
            foreignClient,
            _fixture,
            password: TaskFlowApiFixture.TestPassword);

        using var foreignAuthorizedClient = _fixture.CreateClient().WithBearer(foreignTokens.AccessToken);

        var getProject = await foreignAuthorizedClient.GetAsync($"/api/projects/{createdProject.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, getProject.StatusCode);
    }

    [Fact]
    public async Task Authenticated_User_GettingMissingProject_ReturnsNotFound()
    {
        using var client = _fixture.CreateClient();

        var tokens = await AuthHelper.RegisterAndLoginAsync(
            client,
            _fixture,
            password: TaskFlowApiFixture.TestPassword);

        using var authorizedClient = _fixture.CreateClient().WithBearer(tokens.AccessToken);

        var missingProjectId = Guid.NewGuid();
        var getProject = await authorizedClient.GetAsync($"/api/projects/{missingProjectId}");

        Assert.Equal(HttpStatusCode.NotFound, getProject.StatusCode);
    }

    [Fact]
    public async Task Developer_SeesOnlyOwnProjects()
    {
        using var ownerClient = _fixture.CreateClient();

        var ownerTokens = await AuthHelper.RegisterAndLoginAsync(
            ownerClient,
            _fixture,
            password: TaskFlowApiFixture.TestPassword);

        await AuthHelper.SetRoleAsync(_fixture, ownerTokens.UserId, UserRole.Manager);

        var managerTokens = await AuthHelper.LoginAsync(
            ownerClient,
            _fixture,
            ownerTokens.Email,
            TaskFlowApiFixture.TestPassword);

        using var managerAuthorizedClient = _fixture.CreateClient().WithBearer(managerTokens.AccessToken);

        var createProject = await managerAuthorizedClient.PostAsJsonAsync("/api/projects", new CreateProjectDto
        {
            Name = "Sprint 3 Test",
            Description = "authz"
        });

        Assert.Equal(HttpStatusCode.Created, createProject.StatusCode);

        var createdProject = await createProject.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(createdProject);
        Assert.NotEqual(Guid.Empty, createdProject.Id);

        await AuthHelper.SetRoleAsync(_fixture, ownerTokens.UserId, UserRole.Developer);

        var developerTokens = await AuthHelper.LoginAsync(
            ownerClient,
            _fixture,
            ownerTokens.Email,
            TaskFlowApiFixture.TestPassword);

        using var developerAuthorizedClient = _fixture.CreateClient().WithBearer(developerTokens.AccessToken);

        var ownerProjects = await developerAuthorizedClient.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.OK, ownerProjects.StatusCode);

        var ownerProjectsList = await ownerProjects.Content.ReadFromJsonAsync<ProjectDto[]>();
        Assert.NotNull(ownerProjectsList);
        Assert.Contains(ownerProjectsList, p => p.Id == createdProject.Id);

        using var otherClient = _fixture.CreateClient();

        var otherTokens = await AuthHelper.RegisterAndLoginAsync(
            otherClient,
            _fixture,
            password: TaskFlowApiFixture.TestPassword);

        using var otherAuthorizedClient = _fixture.CreateClient().WithBearer(otherTokens.AccessToken);

        var otherProjects = await otherAuthorizedClient.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.OK, otherProjects.StatusCode);

        var otherProjectsList = await otherProjects.Content.ReadFromJsonAsync<ProjectDto[]>();
        Assert.NotNull(otherProjectsList);
        Assert.DoesNotContain(otherProjectsList, p => p.Id == createdProject.Id);
    }

    [Fact]
    public async Task Developer_CannotAccessForeignTask_ById()
    {
        using var ownerClient = _fixture.CreateClient();

        var ownerTokens = await AuthHelper.RegisterAndLoginAsync(
            ownerClient,
            _fixture,
            password: TaskFlowApiFixture.TestPassword);

        await AuthHelper.SetRoleAsync(_fixture, ownerTokens.UserId, UserRole.Manager);

        var managerTokens = await AuthHelper.LoginAsync(
            ownerClient,
            _fixture,
            ownerTokens.Email,
            TaskFlowApiFixture.TestPassword);

        using var managerAuthorizedClient = _fixture.CreateClient().WithBearer(managerTokens.AccessToken);

        var createProject = await managerAuthorizedClient.PostAsJsonAsync("/api/projects", new CreateProjectDto
        {
            Name = "Task Access Test",
            Description = "authz"
        });

        Assert.Equal(HttpStatusCode.Created, createProject.StatusCode);

        var createdProject = await createProject.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(createdProject);
        Assert.NotEqual(Guid.Empty, createdProject.Id);

        var createTask = await managerAuthorizedClient.PostAsJsonAsync("/api/tasks", new CreateTaskDto
        {
            ProjectId = createdProject.Id,
            Title = "Task 1",
            Description = "test task",
            Status = TaskState.New,
            Priority = TaskPriority.Medium
        });

        Assert.Equal(HttpStatusCode.Created, createTask.StatusCode);

        var createdTask = await createTask.Content.ReadFromJsonAsync<TaskDto>();
        Assert.NotNull(createdTask);
        Assert.NotEqual(Guid.Empty, createdTask.Id);

        await AuthHelper.SetRoleAsync(_fixture, ownerTokens.UserId, UserRole.Developer);

        var developerTokens = await AuthHelper.LoginAsync(
            ownerClient,
            _fixture,
            ownerTokens.Email,
            TaskFlowApiFixture.TestPassword);

        using var foreignClient = _fixture.CreateClient();

        var foreignTokens = await AuthHelper.RegisterAndLoginAsync(
            foreignClient,
            _fixture,
            password: TaskFlowApiFixture.TestPassword);

        using var foreignAuthorizedClient = _fixture.CreateClient().WithBearer(foreignTokens.AccessToken);

        var getTask = await foreignAuthorizedClient.GetAsync($"/api/tasks/{createdTask.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, getTask.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_User_CannotCreateProject()
    {
        using var client = _fixture.CreateClient();

        var createProject = await client.PostAsJsonAsync("/api/projects", new CreateProjectDto
        {
            Name = "Sprint 3 Test",
            Description = "authz"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, createProject.StatusCode);
    }
}
