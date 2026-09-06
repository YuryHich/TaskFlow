using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

    public class AppDbContext : DbContext
    {
        
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Domain.Models.User> Users { get; set; } = null!;
        public DbSet<Domain.Models.Project> Projects { get; set; } = null!;
        public DbSet<Domain.Models.WorkTask> Tasks { get; set; } = null!;
        public DbSet<Domain.Models.Tag> Tags { get; set; } = null!;
        public DbSet<Domain.Models.Comment> Comments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    
        }
    }
