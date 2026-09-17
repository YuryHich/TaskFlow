using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(log => log.Id);

        builder.Property(log => log.EventId).IsRequired();
        builder.HasIndex(log => log.EventId).IsUnique();

        builder.Property(log => log.EventType).IsRequired().HasMaxLength(64);
        builder.Property(log => log.EntityType).IsRequired().HasMaxLength(32);
        builder.Property(log => log.EntityId).IsRequired();
        builder.Property(log => log.OccurredAt).IsRequired();
        builder.Property(log => log.Payload).HasColumnType("jsonb");

        builder.HasIndex(log => log.OccurredAt);
    }
}
