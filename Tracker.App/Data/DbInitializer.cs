using Microsoft.EntityFrameworkCore;
using Tracker.App.Common.Helpers;
using Tracker.App.Data.Entities;

namespace Tracker.App.Data;

/// <summary>
/// Database initializer and development data seeder.
/// Ensures the database schema is up-to-date and seeds a default test user for authentication testing.
/// </summary>
public static class DbInitializer
{
    public const string SeedUserEmail = "test@example.com";
    public const string SeedUserPassword = "Password123!";

    /// <summary>
    /// Applies pending migrations and seeds the test user if they do not exist.
    /// </summary>
    public static async Task SeedDevelopmentDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            // Apply pending database migrations automatically
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending database migrations...", pendingMigrations.Count());
                await context.Database.MigrateAsync();
            }

            // Check if test user exists
            var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == SeedUserEmail);
            if (existingUser is null)
            {
                var testUser = new User
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Email = SeedUserEmail,
                    PasswordHash = PasswordHashHelper.HashPassword(SeedUserPassword),
                    FullName = "Test User",
                    IsActive = true,
                    IsEmailVerified = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                await context.Users.AddAsync(testUser);
                await context.SaveChangesAsync();

                logger.LogInformation("=================================================================");
                logger.LogInformation("DEFAULT TEST USER SEEDED SUCCESSFULLY:");
                logger.LogInformation("Email:    {Email}", SeedUserEmail);
                logger.LogInformation("Password: {Password}", SeedUserPassword);
                logger.LogInformation("=================================================================");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database seeding warning: could not connect to PostgreSQL. Verify PostgreSQL is running on localhost:5432.");
        }
    }
}
