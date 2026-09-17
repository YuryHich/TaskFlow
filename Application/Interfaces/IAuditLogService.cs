using Application.DTOs.Audit;

namespace Application.Interfaces;

public interface IAuditLogService
{
    Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(int take, CancellationToken cancellationToken = default);
}
