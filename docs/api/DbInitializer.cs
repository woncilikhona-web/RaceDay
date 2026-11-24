using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, ILogger logger)
    {
        const int maxAttempts = 10;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

                // Creates the database and tables if they do not exist yet.
                // If you already ran database/01_Create_Database_And_Tables.sql this does nothing.
                await db.Database.EnsureCreatedAsync();

                if (!await db.Categories.AnyAsync())
                {
                    db.Categories.AddRange(
                        new Category { Name = "5K Fun Run", Description = "Short, friendly runs for all fitness levels." },
                        new Category { Name = "10K Road Race", Description = "Road races of around 10 km." },
                        new Category { Name = "Half Marathon", Description = "21.1 km road races." },
                        new Category { Name = "Marathon", Description = "42.2 km road races." },
                        new Category { Name = "Trail", Description = "Off-road trail runs and walks." },
                        new Category { Name = "Road Cycling", Description = "Road cycling tours and races." },
                        new Category { Name = "Mountain Bike", Description = "Off-road cycling events." },
                        new Category { Name = "Charity Walk", Description = "Walks that raise money for good causes." });
                    await db.SaveChangesAsync();
                }

                logger.LogInformation("Database ready.");
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}", attempt, maxAttempts, ex.Message);
                if (attempt < maxAttempts) await Task.Delay(TimeSpan.FromSeconds(3));
                else
                {
                    logger.LogError(ex,
                        "=== DATABASE CONNECTION FAILED === Check ConnectionStrings:DefaultConnection in appsettings.json and that SQL Server / LocalDB is running.");
                }
            }
        }
    }
}
