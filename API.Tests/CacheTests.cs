using System.Net;
using System.Net.Http.Json;
using API.Tests.Infrastructure;
using Application.Caching;
using Application.DTOs;
using Domain.Models;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

[Collection(ApiTestCollection.Name)]
public class CacheTests
{
    private readonly TaskFlowApiFixture _fixture;

    public CacheTests(TaskFlowApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetProjectById_PopulatesCache_PutRemovesIt()
    {
        using var client = _fixture.CreateApiClient();
        var manager = await AuthHelper.RegisterAndLoginAsync(client, _fixture);
        await AuthHelper.SetRoleAsync(_fixture, manager.UserId, UserRole.Manager);
        var tokens = await AuthHelper.LoginAsync(client, _fixture, manager.Email);
        client.WithBearer(tokens.AccessToken);

        var created = await client.PostAsJsonAsync("/api/projects", new CreateProjectDto
        {
            Name = "Cached",
            Description = "before"
        });
        created.EnsureSuccessStatusCode();
        var project = await created.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(project);

        var cache = _fixture.Services.GetRequiredService<ICacheService>();
        Assert.Null(await cache.GetAsync<ProjectDto>(CacheKeys.Project(project.Id)));

        var get = await client.GetAsync($"/api/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var cached = await cache.GetAsync<ProjectDto>(CacheKeys.Project(project.Id));
        Assert.NotNull(cached);
        Assert.Equal("Cached", cached.Name);

        var put = await client.PutAsJsonAsync($"/api/projects/{project.Id}", new UpdateProjectDto
        {
            Name = "Cached-updated",
            Description = "after"
        });
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
        Assert.Null(await cache.GetAsync<ProjectDto>(CacheKeys.Project(project.Id)));

        var getAgain = await client.GetAsync($"/api/projects/{project.Id}");
        var updated = await getAgain.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.Equal("Cached-updated", updated!.Name);
        Assert.NotNull(await cache.GetAsync<ProjectDto>(CacheKeys.Project(project.Id)));
    }

    [Fact]
    public async Task GetTags_PopulatesCache_CreateTagInvalidates()
    {
        using var client = _fixture.CreateApiClient();
        var manager = await AuthHelper.RegisterAndLoginAsync(client, _fixture);
        await AuthHelper.SetRoleAsync(_fixture, manager.UserId, UserRole.Manager);
        var tokens = await AuthHelper.LoginAsync(client, _fixture, manager.Email);
        client.WithBearer(tokens.AccessToken);

        var cache = _fixture.Services.GetRequiredService<ICacheService>();

        (await client.GetAsync("/api/tags")).EnsureSuccessStatusCode();
        Assert.NotNull(await cache.GetAsync<List<TagDto>>(CacheKeys.TagsAll));

        var create = await client.PostAsJsonAsync("/api/tags", new CreateTagDto { Name = $"tag-{Guid.NewGuid():N}" });
        create.EnsureSuccessStatusCode();
        Assert.Null(await cache.GetAsync<List<TagDto>>(CacheKeys.TagsAll));
    }

    [Fact]
    public async Task GetTaskById_PopulatesCache_DeleteRemovesIt()
    {
        using var client = _fixture.CreateApiClient();
        var manager = await AuthHelper.RegisterAndLoginAsync(client, _fixture);
        await AuthHelper.SetRoleAsync(_fixture, manager.UserId, UserRole.Manager);
        var tokens = await AuthHelper.LoginAsync(client, _fixture, manager.Email);
        client.WithBearer(tokens.AccessToken);

        var projectResponse = await client.PostAsJsonAsync("/api/projects", new CreateProjectDto { Name = "T" });
        var project = await projectResponse.Content.ReadFromJsonAsync<ProjectDto>();

        var taskResponse = await client.PostAsJsonAsync("/api/tasks", new CreateTaskDto
        {
            ProjectId = project!.Id,
            Title = "cached-task",
            AssigneeIds = []
        });
        var task = await taskResponse.Content.ReadFromJsonAsync<TaskDto>();
        Assert.NotNull(task);

        var cache = _fixture.Services.GetRequiredService<ICacheService>();
        (await client.GetAsync($"/api/tasks/{task.Id}")).EnsureSuccessStatusCode();
        Assert.NotNull(await cache.GetAsync<TaskDto>(CacheKeys.Task(task.Id)));

        var delete = await client.DeleteAsync($"/api/tasks/{task.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Null(await cache.GetAsync<TaskDto>(CacheKeys.Task(task.Id)));
    }
}