using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
       public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(project => project.Id);

        builder.Property(project => project.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(project => project.Description)
            .HasMaxLength(1000);

        builder.Property(project => project.CreatedAt)
            .IsRequired();

        builder.Property(project => project.OwnerId)
            .IsRequired();

        builder.HasOne(project => project.Owner)
            .WithMany(user => user.OwnedProjects)
            .HasForeignKey(project => project.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(project => project.OwnerId);
    } 
    }
}