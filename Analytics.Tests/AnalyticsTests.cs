using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Analytics.Contracts;
using Analytics.Streaming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Analytics.Tests;

[Collection("analytics")]
public sealed class AnalyticsTests
{
    private readonly AnalyticsFixture _fixture;

    public AnalyticsTests(AnalyticsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Reducer_IsIdempotent_AndTracksDone()
    {
        await _fixture.ResetAsync();
        var eventId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var created = Envelope(eventId, "task.created", new
        {
            taskId,
            projectId,
            title = "Ship",
            status = "New"
        }, DateTime.UtcNow.AddDays(-2));

        await ApplyAsync(created);
        await ApplyAsync(created);

        var doneId = Guid.NewGuid();
        await ApplyAsync(Envelope(doneId, "task.updated", new
        {
            taskId,
            projectId,
            title = "Ship",
            status = "Done"
        }, DateTime.UtcNow));

        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Manager"));
        var summary = await client.GetFromJsonAsync<AnalyticsSummaryDto>("/api/analytics/summary");
        Assert.NotNull(summary);
        Assert.Equal(1, summary.Total);
        Assert.Equal(0, summary.New);
        Assert.Equal(1, summary.Done);
        Assert.NotNull(summary.AverageCompletionDays);
        Assert.True(summary.AverageCompletionDays > 1);

        var projects = await client.GetFromJsonAsync<List<ProjectAnalyticsDto>>("/api/analytics/projects");
        Assert.NotNull(projects);
        Assert.Single(projects);
        Assert.Equal(1, projects[0].Done);
    }

    [Fact]
    public async Task Summary_DeveloperIsForbidden_AnonymousIsUnauthorized()
    {
        using var client = _fixture.CreateClient();
        var anonymous = await client.GetAsync("/api/analytics/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Developer"));
        var forbidden = await client.GetAsync("/api/analytics/summary");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    private async Task ApplyAsync(string json)
    {
        using var scope = _fixture.Services.CreateScope();
        var reducer = scope.ServiceProvider.GetRequiredService<EventReducer>();
        await reducer.ApplyAsync(json);
    }

    private static string Envelope(Guid eventId, string eventType, object payload, DateTime occurredAt)
    {
        return JsonSerializer.Serialize(new
        {
            eventId,
            eventType,
            occurredAt,
            version = 1,
            payload
        });
    }

    private static string Token(string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AnalyticsFixture.JwtKey));
        var token = new JwtSecurityToken(
            issuer: "TaskFlow",
            audience: "TaskFlow",
            claims:
            [
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            ],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

[CollectionDefinition("analytics")]
public sealed class AnalyticsCollection : ICollectionFixture<AnalyticsFixture>;
