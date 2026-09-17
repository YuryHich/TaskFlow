namespace Application.DTOs.Audit;

public sealed record AuditLogDto(
    Guid Id,
    Guid EventId,
    string EventType,
    string EntityType,
    Guid EntityId,
    Guid? ProjectId,
    Guid? ActorUserId,
    DateTime OccurredAt,
    string? Payload);
