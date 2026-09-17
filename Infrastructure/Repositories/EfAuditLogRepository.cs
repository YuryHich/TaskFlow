using Domain.Models;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Repositories;

public class EfAuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public EfAuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task TryAddAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        _context.AuditLogs.Add(log);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsEventIdConflict(exception))
        {
            _context.Entry(log).State = EntityState.Detached;
        }
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(log => log.OccurredAt)
            .ThenByDescending(log => log.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    private static bool IsEventIdConflict(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
