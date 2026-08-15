using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Wizards.Infrastructure.Persistence;

namespace Wizards.Infrastructure.Extensions;

/// <summary>
/// Brings the Wizards database up to date from a host's built service provider.
/// </summary>
public static class ServiceProviderExtensions
{
    private const string LoggerCategory = "Wizards.Infrastructure.DatabaseInitialization";

    /// <summary>
    /// Applies every pending migration to the Wizards database, creating it if it does not exist, and
    /// then seeds the reference data the application cannot run without.
    /// </summary>
    /// <remarks>
    /// Both steps are idempotent, so this is safe to call on every start against a database at any
    /// point in its history.
    /// </remarks>
    /// <param name="serviceProvider">
    /// The built root provider. A scope is created internally, so this must not be a scoped provider
    /// that is disposed before the returned task completes.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancels the initialization before it completes. Pass the host's shutdown token so a stop
    /// requested while a migration waits on a contended database file is honored rather than ignored.
    /// </param>
    /// <returns>A task that completes once the database is migrated and seeded.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serviceProvider"/> is <see langword="null"/>.
    /// </exception>
    public static async Task InitializeDatabaseAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        await using AsyncServiceScope initializationScope = serviceProvider.CreateAsyncScope();

        ILogger logger = initializationScope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(LoggerCategory);

        try
        {
            AppDbContext dbContext = initializationScope.ServiceProvider.GetRequiredService<AppDbContext>();
            DatabaseSeeder databaseSeeder = initializationScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

            await dbContext.Database.MigrateAsync(cancellationToken);

            await databaseSeeder.SeedAsync(cancellationToken);
        }
        catch (Exception initializationFailure)
        {
            logger.LogCritical(
                initializationFailure,
                "Migrating or seeding the Wizards database failed. Startup is being aborted so the API is never served against an unusable database.");

            throw;
        }
    }
}
