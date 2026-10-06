using System.Text.Json;
using Analytics.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Analytics.Streaming;

public sealed class EventReducer
{
    private readonly AnalyticsDbContext _db;

    public EventReducer(AnalyticsDbContext db)
    {
        _db = db;
    }

    public async Task ApplyAsync(string json, CancellationToken cancellationToken = default)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return;
        }

        using (document)
        {
            var root = document.RootElement;
            if (!TryGuid(root, "eventId", out var eventId))
                return;
            if (!root.TryGetProperty("eventType", out var typeElement))
                return;

            var eventType = typeElement.GetString() ?? string.Empty;
            var occurredAt = ReadTime(root, "occurredAt") ?? DateTime.UtcNow;
            var payload = root.TryGetProperty("payload", out var payloadElement)
                ? payloadElement
                : default;

            if (await _db.ProcessedEvents.AnyAsync(row => row.EventId == eventId, cancellationToken))
                return;

            switch (eventType)
            {
                case "task.created":
                case "task.updated":
                    await UpsertTaskAsync(payload, occurredAt, cancellationToken);
                    break;
                case "task.deleted":
                    await MarkTaskDeletedAsync(payload, occurredAt, cancellationToken);
                    break;
                case "project.created":
                case "project.updated":
                    await UpsertProjectAsync(payload, cancellationToken);
                    break;
                case "project.deleted":
                    await MarkProjectDeletedAsync(payload, cancellationToken);
                    break;
            }

            _db.ProcessedEvents.Add(new ProcessedEvent
            {
                EventId = eventId,
                ProcessedAt = DateTime.UtcNow
            });

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                _db.ChangeTracker.Clear();
            }
        }
    }

    private async Task UpsertTaskAsync(JsonElement payload, DateTime occurredAt, CancellationToken cancellationToken)
    {
        if (!TryGuid(payload, "taskId", out var taskId) || !TryGuid(payload, "projectId", out var projectId))
            return;

        await EnsureProjectAsync(projectId, cancellationToken);
        var fact = await _db.TaskFacts.FindAsync([taskId], cancellationToken);
        if (fact is null)
        {
            fact = new TaskFact
            {
                TaskId = taskId,
                CreatedAt = occurredAt
            };
            _db.TaskFacts.Add(fact);
        }

        fact.ProjectId = projectId;
        fact.Title = ReadString(payload, "title") ?? fact.Title;
        var status = ReadStatus(payload) ?? fact.Status;
        if (status == "Done")
        {
            if (fact.CompletedAt is null)
                fact.CompletedAt = occurredAt;
        }
        else
        {
            fact.CompletedAt = null;
        }

        fact.Status = status;
        fact.Deleted = false;
    }

    private async Task MarkTaskDeletedAsync(JsonElement payload, DateTime occurredAt, CancellationToken cancellationToken)
    {
        if (!TryGuid(payload, "taskId", out var taskId))
            return;

        var projectId = TryGuid(payload, "projectId", out var parsedProject) ? parsedProject : Guid.Empty;
        if (projectId != Guid.Empty)
            await EnsureProjectAsync(projectId, cancellationToken);

        var fact = await _db.TaskFacts.FindAsync([taskId], cancellationToken);
        if (fact is null)
        {
            fact = new TaskFact
            {
                TaskId = taskId,
                ProjectId = projectId,
                CreatedAt = occurredAt,
                Status = "New"
            };
            _db.TaskFacts.Add(fact);
        }

        fact.Deleted = true;
    }

    private async Task UpsertProjectAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        if (!TryGuid(payload, "projectId", out var projectId))
            return;

        var fact = await _db.ProjectFacts.FindAsync([projectId], cancellationToken);
        if (fact is null)
        {
            fact = new ProjectFact { ProjectId = projectId };
            _db.ProjectFacts.Add(fact);
        }

        var name = ReadString(payload, "name");
        if (!string.IsNullOrWhiteSpace(name))
            fact.Name = name;
        fact.Deleted = false;
    }

    private async Task MarkProjectDeletedAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        if (!TryGuid(payload, "projectId", out var projectId))
            return;

        var fact = await _db.ProjectFacts.FindAsync([projectId], cancellationToken);
        if (fact is null)
        {
            fact = new ProjectFact { ProjectId = projectId, Name = string.Empty };
            _db.ProjectFacts.Add(fact);
        }

        fact.Deleted = true;
        var tasks = await _db.TaskFacts.Where(task => task.ProjectId == projectId).ToListAsync(cancellationToken);
        foreach (var task in tasks)
            task.Deleted = true;
    }

    private async Task EnsureProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var exists = await _db.ProjectFacts.AnyAsync(project => project.ProjectId == projectId, cancellationToken);
        if (exists)
            return;

        _db.ProjectFacts.Add(new ProjectFact
        {
            ProjectId = projectId,
            Name = string.Empty
        });
    }

    private static string? ReadStatus(JsonElement payload)
    {
        if (!payload.TryGetProperty("status", out var status))
            return null;
        if (status.ValueKind == JsonValueKind.Number && status.TryGetInt32(out var code))
        {
            return code switch
            {
                0 => "New",
                1 => "InProgress",
                2 => "Done",
                3 => "Cancelled",
                _ => "New"
            };
        }

        return status.GetString();
    }

    private static string? ReadString(JsonElement payload, string name)
        => payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty(name, out var value)
            ? value.GetString()
            : null;

    private static DateTime? ReadTime(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
            return null;
        if (value.TryGetDateTime(out var dateTime))
            return dateTime.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
                : dateTime.ToUniversalTime();
        return null;
    }

    private static bool TryGuid(JsonElement element, string name, out Guid id)
    {
        id = Guid.Empty;
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
            return false;
        return value.TryGetGuid(out id);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException postgres
           && postgres.SqlState == PostgresErrorCodes.UniqueViolation;
}
