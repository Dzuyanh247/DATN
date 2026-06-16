using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Datn.PcStore.Data;

public sealed record ResolvedDatabaseConnection(
    string ConnectionString,
    string ConnectionName,
    string Server,
    string Database,
    bool UsedFallback);

public static class DatabaseConfiguration
{
    public static ResolvedDatabaseConnection ResolveConnectionString(IConfiguration configuration, ILogger logger)
    {
        var settings = configuration.GetSection(DatabaseSettings.SectionName).Get<DatabaseSettings>() ?? new DatabaseSettings();
        var primary = GetRequiredConnectionString(configuration, settings.PrimaryConnectionName);
        var fallback = GetRequiredConnectionString(configuration, settings.FallbackConnectionName);

        if (CanOpen(primary, settings.ConnectionTestTimeoutSeconds, logger, settings.PrimaryConnectionName))
        {
            return CreateResolved(primary, settings.PrimaryConnectionName, usedFallback: false);
        }

        logger.LogWarning(
            "Database primary connection {PrimaryConnectionName} is unavailable. Falling back to {FallbackConnectionName}.",
            settings.PrimaryConnectionName,
            settings.FallbackConnectionName);

        return CreateResolved(fallback, settings.FallbackConnectionName, usedFallback: true);
    }

    public static async Task MigrateDatabaseAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            var pendingMigrations = (await db.Database.GetPendingMigrationsAsync()).ToList();
            if (pendingMigrations.Count == 0)
            {
                logger.LogInformation("Database is up to date. No pending migrations.");
            }
            else
            {
                logger.LogInformation("Applying {MigrationCount} pending migration(s): {Migrations}", pendingMigrations.Count, string.Join(", ", pendingMigrations));
                foreach (var migration in pendingMigrations)
                {
                    logger.LogInformation("Pending migration: {Migration}", migration);
                }
            }

            await db.Database.MigrateAsync();
            logger.LogInformation("Database migration completed successfully.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database migration failed. Connection: {ConnectionString}", RedactConnectionString(db.Database.GetDbConnection()));
            throw;
        }
    }

    private static string GetRequiredConnectionString(IConfiguration configuration, string name) =>
        configuration.GetConnectionString(name)
        ?? throw new InvalidOperationException($"Missing ConnectionStrings:{name}");

    private static bool CanOpen(string connectionString, int timeoutSeconds, ILogger logger, string connectionName)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = Math.Max(1, timeoutSeconds)
            };

            using var connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            logger.LogInformation("Database connection {ConnectionName} is available at server {Server}.", connectionName, builder.DataSource);
            return true;
        }
        catch (Exception exception) when (exception is SqlException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Database connection {ConnectionName} is not available.", connectionName);
            return false;
        }
    }

    private static ResolvedDatabaseConnection CreateResolved(string connectionString, string connectionName, bool usedFallback)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        return new ResolvedDatabaseConnection(
            connectionString,
            connectionName,
            builder.DataSource,
            builder.InitialCatalog,
            usedFallback);
    }

    private static string RedactConnectionString(DbConnection connection)
    {
        var builder = new SqlConnectionStringBuilder(connection.ConnectionString)
        {
            Password = string.IsNullOrWhiteSpace(new SqlConnectionStringBuilder(connection.ConnectionString).Password) ? string.Empty : "***"
        };

        return builder.ConnectionString;
    }
}
