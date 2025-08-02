using MedChain_Models.Entities;
using MedChain_Models.Enums;
using MedChain_DAL.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedChain_UI.Data
{
    public class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var logger = services.GetRequiredService<ILogger<SeedData>>();
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            try
            {
                logger.LogInformation("Starting database seeding process");

                // Ensure database is created and migrated
                await EnsureDatabaseCreatedAndMigrated(context, logger);

                // Seed roles
                await SeedRoles(roleManager, logger);

                // Seed admin user
                await SeedAdminUser(userManager, logger);

                // Seed test users (optional)
                await SeedTestUsers(userManager, logger);

                logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database");
                throw;
            }
        }

        private static async Task EnsureDatabaseCreatedAndMigrated(ApplicationDbContext context, ILogger logger)
        {
            try
            {
                logger.LogInformation("Applying pending migrations...");
                await context.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error applying migrations");
                throw;
            }
        }

        private static async Task SeedRoles(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            try
            {
                logger.LogInformation("Seeding roles...");

                foreach (var role in Enum.GetNames(typeof(UserRoles)))
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        var result = await roleManager.CreateAsync(new IdentityRole(role)
                        {
                            NormalizedName = role.ToUpper()
                        });

                        if (result.Succeeded)
                        {
                            logger.LogInformation("Created role: {Role}", role);
                        }
                        else
                        {
                            logger.LogError("Failed to create role {Role}: {Errors}",
                                role, string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding roles");
                throw;
            }
        }

        private static async Task SeedAdminUser(UserManager<ApplicationUser> userManager, ILogger logger)
        {
            const string adminEmail = "admin@medchain.com";
            const string adminPassword = "Admin@1234"; // Stronger password

            try
            {
                logger.LogInformation("Checking admin user...");

                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        FullName = "System Administrator",
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                    if (createResult.Succeeded)
                    {
                        logger.LogInformation("Admin user created successfully");

                        var roleResult = await userManager.AddToRoleAsync(adminUser, nameof(UserRoles.Admin));
                        if (roleResult.Succeeded)
                        {
                            logger.LogInformation("Admin role assigned successfully");
                        }
                        else
                        {
                            logger.LogError("Failed to assign admin role: {Errors}",
                                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                        }
                    }
                    else
                    {
                        logger.LogError("Failed to create admin user: {Errors}",
                            string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    logger.LogInformation("Admin user already exists");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding admin user");
                throw;
            }
        }

        private static async Task SeedTestUsers(UserManager<ApplicationUser> userManager, ILogger logger)
        {
            try
            {
                logger.LogInformation("Seeding test users...");

                var testUsers = new List<(string Email, string Password, string FullName, UserRoles Role)>
                {
                    ("doctor1@medchain.com", "Doctor@1234", "Dr. John Smith", UserRoles.Doctor),
                    ("patient1@medchain.com", "Patient@1234", "Jane Doe", UserRoles.Patient)
                };

                foreach (var (email, password, fullName, role) in testUsers)
                {
                    var user = await userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            FullName = fullName,
                            EmailConfirmed = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        var result = await userManager.CreateAsync(user, password);
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(user, role.ToString());
                            logger.LogInformation("Created test {Role} user: {Email}", role, email);
                        }
                        else
                        {
                            logger.LogWarning("Failed to create test user {Email}: {Errors}",
                                email, string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding test users");
                // Don't throw - test users are optional
            }
        }
    }
}