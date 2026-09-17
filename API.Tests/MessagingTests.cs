using System.Net;
using System.Net.Http.Json;
using API.Tests.Infrastructure;
using Application.DTOs;
using Application.DTOs.Audit;
using Application.Events;
using Domain.Models;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

[Collection(ApiTestCollection.Name)]
public class MessagingTests
{
    private readonly TaskFlowApiFixture _fixture;

    public MessagingTests(TaskFlowApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateTask_WritesOneAuditLog_RepublishSameEventId_DoesNotDuplicate()
    {
        using var client = _fixture.CreateApiClient();
        var created = await CreateTaskAsManagerAsync(client);
        var rows = await WaitForAuditAsync(created.Task.Id, expectedCount: 1);

        Assert.Single(rows);
        Assert.Equal(nameof(TaskCreatedEvent), rows[0].EventType);
        Assert.Equal("Task", rows[0].EntityType);
        Assert.Equal(created.Task.Id, rows[0].EntityId);
        Assert.Equal(created.Project.Id, rows[0].ProjectId);

        using (var scope = _fixture.Services.CreateScope())
        {
            var publish = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            await publish.Publish(new TaskCreatedEvent(
                EventId: rows[0].EventId,
                OccurredAt: rows[0].OccurredAt,
                TaskId: created.Task.Id,
                ProjectId: created.Project.Id,
                ActorUserId: rows[0].ActorUserId ?? Guid.Empty,
                Title: created.Task.Title,
                AssigneeIds: created.Task.AssigneeIds));
        }

        await Task.Delay(400);
        var after = await LoadAuditForEntityAsync(created.Task.Id);
        Assert.Single(after);
    }

    [Fact]
    public async Task GetAudit_IsStaffOnly()
    {
        using var client = _fixture.CreateApiClient();
        var created = await CreateTaskAsManagerAsync(client);
        await WaitForAuditAsync(created.Task.Id, expectedCount: 1);

        var developer = await AuthHelper.RegisterAndLoginAsync(client, _fixture);
        using var developerClient = _fixture.CreateApiClient().WithBearer(developer.AccessToken);
        var forbidden = await developerClient.GetAsync("/api/audit");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var allowed = await client.GetAsync("/api/audit?take=20");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        var list = await allowed.Content.ReadFromJsonAsync<List<AuditLogDto>>();
        Assert.NotNull(list);
        Assert.Contains(list, row => row.EntityId == created.Task.Id && row.EventType == nameof(TaskCreatedEvent));
    }

    private async Task<(ProjectDto Project, TaskDto Task)> CreateTaskAsManagerAsync(HttpClient client)
    {
        var manager = await AuthHelper.RegisterAndLoginAsync(client, _fixture);
        await AuthHelper.SetRoleAsync(_fixture, manager.UserId, UserRole.Manager);
        var tokens = await AuthHelper.LoginAsync(client, _fixture, manager.Email);
        client.WithBearer(tokens.AccessToken);

        var projectResponse = await client.PostAsJsonAsync("/api/projects", new CreateProjectDto { Name = "audit-project" });
        projectResponse.EnsureSuccessStatusCode();
        var project = await projectResponse.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(project);

        var taskResponse = await client.PostAsJsonAsync("/api/tasks", new CreateTaskDto
        {
            ProjectId = project.Id,
            Title = "audit-task",
            AssigneeIds = []
        });
        taskResponse.EnsureSuccessStatusCode();
        var task = await taskResponse.Content.ReadFromJsonAsync<TaskDto>();
        Assert.NotNull(task);
        return (project, task);
    }

    private async Task<List<AuditLog>> WaitForAuditAsync(Guid entityId, int expectedCount)
    {
        var deadline = DateTime.UtcNow.AddSeconds(8);
        List<AuditLog> rows = [];
        while (DateTime.UtcNow < deadline)
        {
            rows = await LoadAuditForEntityAsync(entityId);
            if (rows.Count >= expectedCount)
                return rows;
            await Task.Delay(100);
        }

        throw new TimeoutException($"Expected {expectedCount} audit row(s) for {entityId}, got {rows.Count}.");
    }

    private async Task<List<AuditLog>> LoadAuditForEntityAsync(Guid entityId)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.AuditLogs.AsNoTracking().Where(log => log.EntityId == entityId).ToListAsync();
    }
}
