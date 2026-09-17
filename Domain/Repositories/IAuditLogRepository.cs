using Domain.Models;

namespace Domain.Repositories;

public interface IAuditLogRepository
{
    Task TryAddAsync(AuditLog log, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(int take, CancellationToken cancellationToken = default);
}
