using Analytics.Contracts;
using Analytics.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize(Roles = "Admin,Manager")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly AnalyticsDbContext _db;

    public AnalyticsController(AnalyticsDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<AnalyticsSummaryDto> Summary(CancellationToken cancellationToken)
    {
        var counts = await _db.TaskFacts.AsNoTracking()
            .Where(task => !task.Deleted)
            .GroupBy(task => task.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var done = await _db.TaskFacts.AsNoTracking()
            .Where(task => !task.Deleted && task.Status == "Done" && task.CompletedAt != null)
            .Select(task => new { task.CreatedAt, CompletedAt = task.CompletedAt!.Value })
            .ToListAsync(cancellationToken);

        int Count(string status) => counts.FirstOrDefault(row => row.Status == status)?.Count ?? 0;
        var summary = new AnalyticsSummaryDto
        {
            New = Count("New"),
            InProgress = Count("InProgress"),
            Done = Count("Done"),
            Cancelled = Count("Cancelled")
        };
        summary.Total = summary.New + summary.InProgress + summary.Done + summary.Cancelled;
        summary.AverageCompletionDays = done.Count == 0
            ? null
            : done.Average(row => (row.CompletedAt - row.CreatedAt).TotalDays);
        return summary;
    }

    [HttpGet("projects")]
    public async Task<IReadOnlyList<ProjectAnalyticsDto>> Projects(CancellationToken cancellationToken)
    {
        var projects = await _db.ProjectFacts.AsNoTracking()
            .Where(project => !project.Deleted)
            .OrderBy(project => project.Name)
            .ToListAsync(cancellationToken);
        var counts = await _db.TaskFacts.AsNoTracking()
            .Where(task => !task.Deleted)
            .GroupBy(task => new { task.ProjectId, task.Status })
            .Select(group => new { group.Key.ProjectId, group.Key.Status, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return projects.Select(project =>
        {
            int Count(string status) => counts
                .Where(row => row.ProjectId == project.ProjectId && row.Status == status)
                .Sum(row => row.Count);
            var dto = new ProjectAnalyticsDto
            {
                ProjectId = project.ProjectId,
                Name = string.IsNullOrWhiteSpace(project.Name) ? project.ProjectId.ToString() : project.Name,
                New = Count("New"),
                InProgress = Count("InProgress"),
                Done = Count("Done"),
                Cancelled = Count("Cancelled")
            };
            dto.Total = dto.New + dto.InProgress + dto.Done + dto.Cancelled;
            return dto;
        }).ToList();
    }
}
