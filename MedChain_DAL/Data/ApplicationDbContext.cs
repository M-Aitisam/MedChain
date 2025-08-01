using MedChain_Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace MedChain_DAL.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Add your custom DbSets here if any
        // public DbSet<YourEntity> YourEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure schema
            builder.HasDefaultSchema("dbo");

            // Shorten the identity column length if needed
            builder.Entity<ApplicationUser>(b => {
                b.Property(u => u.Id).HasMaxLength(127);
            });

            builder.Entity<IdentityRole>(b => {
                b.Property(r => r.Id).HasMaxLength(127);
                b.Property(r => r.ConcurrencyStamp).HasMaxLength(127);
            });

            // Your role seeding (keep this)
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "1",
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                },
                new IdentityRole
                {
                    Id = "2",
                    Name = "Doctor",
                    NormalizedName = "DOCTOR",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                },
                new IdentityRole
                {
                    Id = "3",
                    Name = "Patient",
                    NormalizedName = "PATIENT",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                }
            );
        }
    }
}