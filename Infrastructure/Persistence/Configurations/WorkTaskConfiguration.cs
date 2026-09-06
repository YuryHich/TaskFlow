using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class WorkTaskConfiguration : IEntityTypeConfiguration<WorkTask>
    {
        public void Configure(EntityTypeBuilder<WorkTask> builder)
        {
            builder.ToTable("Tasks");

        builder.HasKey(task => task.Id);

        builder.Property(task => task.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(task => task.Description)
            .HasMaxLength(2000);

        builder.Property(task => task.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(task => task.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(task => task.CreatedAt)
            .IsRequired();

        builder.HasOne(task => task.Project)
            .WithMany(project => project.Tasks)
            .HasForeignKey(task => task.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(task => task.Assignee)
            .WithMany(user => user.AssignedTasks)
            .HasForeignKey(task => task.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(task => task.ProjectId);
        builder.HasIndex(task => task.AssigneeId);
        }
    }
}