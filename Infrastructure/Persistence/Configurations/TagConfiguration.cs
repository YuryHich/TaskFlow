using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.ToTable("Tags");

            builder.HasKey(tag => tag.Id);

            builder.Property(tag => tag.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(tag => tag.Name)
                .IsUnique();

            builder.HasMany(tag => tag.Tasks)
                .WithMany(task => task.Tags)
                .UsingEntity<Dictionary<string, object>>(
                    "TaskTags",
                    right => right
                        .HasOne<WorkTask>()
                        .WithMany()
                        .HasForeignKey("TaskId")
                        .OnDelete(DeleteBehavior.Cascade),
                    left => left
                        .HasOne<Tag>()
                        .WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.HasKey("TaskId", "TagId");
                        join.ToTable("TaskTags");
                    });
        }
    }
}