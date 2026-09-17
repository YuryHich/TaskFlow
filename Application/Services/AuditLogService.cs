using Application.DTOs.Audit;
using Application.Interfaces;
using Domain.Repositories;

namespace Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogs;

    public AuditLogService(IAuditLogRepository auditLogs)
    {
        _auditLogs = auditLogs;
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        var size = take < 1 ? 50 : Math.Min(take, 100);
        var rows = await _auditLogs.GetRecentAsync(size, cancellationToken);
        return rows
            .Select(log => new AuditLogDto(
                log.Id,
                log.EventId,
                log.EventType,
                log.EntityType,
                log.EntityId,
                log.ProjectId,
                log.ActorUserId,
                log.OccurredAt,
                log.Payload))
            .ToList();
    }
}
