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

        builder.HasMany(task => task.Assignees)
            .WithMany(user => user.AssignedTasks)
            .UsingEntity<Dictionary<string, object>>(
                "TaskAssignees",
                right => right
                    .HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne<WorkTask>()
                    .WithMany()
                    .HasForeignKey("TaskId")
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.HasKey("TaskId", "UserId");
                    join.ToTable("TaskAssignees");
                    join.HasIndex("UserId");
                });

        builder.HasIndex(task => task.ProjectId);
        }
    }
}