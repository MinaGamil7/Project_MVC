﻿using BulkyBookWebRazor.Models;
using Microsoft.EntityFrameworkCore;

namespace BulkyBookWebRazor.Data
{
    public class ApplicationDbContext :DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<Category> Category { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Web Development", DisplayOrder = 1 },
                new Category { Id = 2, Name = "Programming Languages", DisplayOrder = 2 },
                new Category { Id = 3, Name = "Databases", DisplayOrder = 3 }
                );
        }
    }
}
