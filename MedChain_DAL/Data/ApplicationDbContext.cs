// MedChain_DAL/Data/ApplicationDbContext.cs
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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Seed roles
            var adminRoleId = "1";
            var doctorRoleId = "2";
            var patientRoleId = "3";

            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = adminRoleId,
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },
                new IdentityRole
                {
                    Id = doctorRoleId,
                    Name = "Doctor",
                    NormalizedName = "DOCTOR"
                },
                new IdentityRole
                {
                    Id = patientRoleId,
                    Name = "Patient",
                    NormalizedName = "PATIENT"
                }
            );

            // Seed admin user
            var adminUserId = "1";
            var hasher = new PasswordHasher<ApplicationUser>();
            var adminUser = new ApplicationUser
            {
                Id = adminUserId,
                UserName = "admin@medchain.com",
                NormalizedUserName = "ADMIN@MEDCHAIN.COM",
                Email = "admin@medchain.com",
                NormalizedEmail = "ADMIN@MEDCHAIN.COM",
                EmailConfirmed = true,
                PasswordHash = hasher.HashPassword(null, "Admin@123"),
                SecurityStamp = Guid.NewGuid().ToString(),
                FullName = "System Admin"
            };

            builder.Entity<ApplicationUser>().HasData(adminUser);

            // Assign admin role to admin user
            builder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string>
                {
                    RoleId = adminRoleId,
                    UserId = adminUserId
                }
            );
        }
    }
}