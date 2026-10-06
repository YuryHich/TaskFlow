using Microsoft.EntityFrameworkCore;

namespace Analytics.Persistence;

public sealed class AnalyticsDbContext : DbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options)
    {
    }

    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
    public DbSet<ProjectFact> ProjectFacts => Set<ProjectFact>();
    public DbSet<TaskFact> TaskFacts => Set<TaskFact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.HasKey(row => row.EventId);
        });

        modelBuilder.Entity<ProjectFact>(entity =>
        {
            entity.HasKey(row => row.ProjectId);
            entity.Property(row => row.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<TaskFact>(entity =>
        {
            entity.HasKey(row => row.TaskId);
            entity.Property(row => row.Title).HasMaxLength(500);
            entity.Property(row => row.Status).HasMaxLength(32);
            entity.HasIndex(row => row.ProjectId);
        });
    }
}
