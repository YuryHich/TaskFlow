using System.Text.Json;
using Analytics.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Analytics.Streaming;

public static class AnalyticsReducer
{
    private static readonly JsonSerializerOptions PayloadJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task ApplyAsync(AnalyticsDbContext db, InboundEnvelope envelope, CancellationToken cancellationToken)
    {
        if (await db.ProcessedEvents.AnyAsync(row => row.EventId == envelope.EventId, cancellationToken))
            return;

        switch (envelope.EventType)
        {
            case "task.created":
            case "task.updated":
                ApplyTaskUpsert(db, envelope, envelope.EventType == "task.created");
                break;
            case "task.deleted":
                ApplyTaskDeleted(db, envelope);
                break;
            case "project.created":
            case "project.updated":
                ApplyProjectUpsert(db, envelope, envelope.EventType == "project.created");
                break;
            case "project.deleted":
                await ApplyProjectDeletedAsync(db, envelope, cancellationToken);
                break;
        }

        db.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = envelope.EventId,
            ProcessedAt = DateTime.UtcNow
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            db.ChangeTracker.Clear();
        }
    }

    private static void ApplyTaskUpsert(AnalyticsDbContext db, InboundEnvelope envelope, bool created)
    {
        var payload = envelope.Payload.Deserialize<TaskPayload>(PayloadJson);
        if (payload is null || payload.TaskId == Guid.Empty)
            return;

        var fact = db.TaskFacts.Local.FirstOrDefault(row => row.TaskId == payload.TaskId)
            ?? db.TaskFacts.Find(payload.TaskId);
        var previousStatus = fact?.Status;
        if (fact is null)
        {
            fact = new TaskFact
            {
                TaskId = payload.TaskId,
                CreatedAt = envelope.OccurredAt
            };
            db.TaskFacts.Add(fact);
        }

        if (payload.ProjectId != Guid.Empty)
            fact.ProjectId = payload.ProjectId;
        if (!string.IsNullOrWhiteSpace(payload.Title))
            fact.Title = payload.Title;
        if (!string.IsNullOrWhiteSpace(payload.Status))
            fact.Status = payload.Status;
        if (created)
            fact.Deleted = false;

        if (fact.Status == "Done" && previousStatus != "Done" && fact.CompletedAt is null)
            fact.CompletedAt = envelope.OccurredAt;
        if (fact.Status != "Done" && previousStatus == "Done")
            fact.CompletedAt = null;
    }

    private static void ApplyTaskDeleted(AnalyticsDbContext db, InboundEnvelope envelope)
    {
        var payload = envelope.Payload.Deserialize<TaskPayload>(PayloadJson);
        if (payload is null || payload.TaskId == Guid.Empty)
            return;

        var fact = db.TaskFacts.Find(payload.TaskId);
        if (fact is null)
        {
            fact = new TaskFact
            {
                TaskId = payload.TaskId,
                ProjectId = payload.ProjectId,
                CreatedAt = envelope.OccurredAt,
                Title = payload.Title ?? string.Empty,
                Status = payload.Status ?? "New"
            };
            db.TaskFacts.Add(fact);
        }

        fact.Deleted = true;
    }

    private static void ApplyProjectUpsert(AnalyticsDbContext db, InboundEnvelope envelope, bool created)
    {
        var payload = envelope.Payload.Deserialize<ProjectPayload>(PayloadJson);
        if (payload is null || payload.ProjectId == Guid.Empty)
            return;

        var fact = db.ProjectFacts.Find(payload.ProjectId);
        if (fact is null)
        {
            fact = new ProjectFact { ProjectId = payload.ProjectId };
            db.ProjectFacts.Add(fact);
        }

        if (!string.IsNullOrWhiteSpace(payload.Name))
            fact.Name = payload.Name;
        if (created)
            fact.Deleted = false;
    }

    private static async Task ApplyProjectDeletedAsync(
        AnalyticsDbContext db,
        InboundEnvelope envelope,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Payload.Deserialize<ProjectPayload>(PayloadJson);
        if (payload is null || payload.ProjectId == Guid.Empty)
            return;

        var fact = await db.ProjectFacts.FindAsync([payload.ProjectId], cancellationToken);
        if (fact is null)
        {
            fact = new ProjectFact { ProjectId = payload.ProjectId, Name = payload.Name ?? string.Empty };
            db.ProjectFacts.Add(fact);
        }

        fact.Deleted = true;
        var tasks = await db.TaskFacts.Where(task => task.ProjectId == payload.ProjectId).ToListAsync(cancellationToken);
        foreach (var task in tasks)
            task.Deleted = true;
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation;

    private sealed class TaskPayload
    {
        public Guid TaskId { get; set; }
        public Guid ProjectId { get; set; }
        public string? Title { get; set; }
        public string? Status { get; set; }
    }

    private sealed class ProjectPayload
    {
        public Guid ProjectId { get; set; }
        public string? Name { get; set; }
    }
}
