using System.Globalization;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Wizards.Domain.Interfaces.Repositories;
using Wizards.Infrastructure.Persistence;
using Wizards.Infrastructure.Persistence.Interceptors;
using Wizards.Infrastructure.Persistence.Repositories;

namespace Wizards.Infrastructure.Extensions;

/// <summary>
/// Registers the infrastructure layer's services into a host's dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string WizardsConnectionStringName = "Wizards";

    private const string BusyTimeoutConfigurationKey = "Sqlite:BusyTimeoutSeconds";

    private static readonly TimeSpan DefaultBusyTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Registers <see cref="AppDbContext"/> against the SQLite database named by the <c>Wizards</c>
    /// connection string along with the repositories that read it, the <see cref="IUnitOfWork"/> that
    /// commits their changes, the seeder that
    /// <see cref="ServiceProviderExtensions.InitializeDatabaseAsync(IServiceProvider, CancellationToken)"/>
    /// runs, and attempts to put every connection into write-ahead logging mode with a configured wait
    /// for contended write locks.
    /// </summary>
    /// <remarks>
    /// The lock wait is read from <c>Sqlite:BusyTimeoutSeconds</c> and defaults to 30 seconds when the
    /// key is absent. Zero is honored and means a contended write fails immediately rather than
    /// waiting. Pragma application is best effort and is logged rather than thrown if it fails, so a
    /// successful call does not by itself guarantee write-ahead logging is active.
    /// </remarks>
    /// <param name="services">The container to register the persistence services into.</param>
    /// <param name="configuration">
    /// The configuration to read the <c>Wizards</c> connection string and <c>Sqlite:BusyTimeoutSeconds</c>
    /// from. Both are read once during registration, so later changes to the underlying configuration
    /// source are not picked up.
    /// </param>
    /// <returns>The same <paramref name="services"/> instance, so registrations can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configuration"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <c>Wizards</c> connection string is absent or blank, or when
    /// <c>Sqlite:BusyTimeoutSeconds</c> is present but is not a non-negative whole number. Both surface
    /// at startup rather than on the first database call.
    /// </exception>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string? wizardsConnectionString = configuration.GetConnectionString(WizardsConnectionStringName);

        if (string.IsNullOrWhiteSpace(wizardsConnectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{WizardsConnectionStringName}' was not found. Add it under the 'ConnectionStrings' section of appsettings.json.");
        }

        TimeSpan busyTimeout = ResolveBusyTimeout(configuration);

        // Essentially handles connection queueing and lock contention for SQLite, which is a single-writer database.
        services.AddSingleton(serviceProvider => new SqlitePragmaConnectionInterceptor(
            serviceProvider.GetRequiredService<ILogger<SqlitePragmaConnectionInterceptor>>(),
            busyTimeout));

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseSqlite(wizardsConnectionString);
            options.AddInterceptors(serviceProvider.GetRequiredService<SqlitePragmaConnectionInterceptor>());
        });

        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventsRepository, EventsRepository>();
        services.AddScoped<IEventRegistrationsRepository, EventRegistrationsRepository>();
        services.AddScoped<IGameTypesRepository, GameTypesRepository>();

        return services;
    }

    private static TimeSpan ResolveBusyTimeout(IConfiguration configuration)
    {
        string? configuredBusyTimeout = configuration[BusyTimeoutConfigurationKey];

        if (string.IsNullOrWhiteSpace(configuredBusyTimeout))
        {
            return DefaultBusyTimeout;
        }

        if (!int.TryParse(configuredBusyTimeout, CultureInfo.InvariantCulture, out int busyTimeoutSeconds)
            || busyTimeoutSeconds < 0)
        {
            throw new InvalidOperationException(
                $"Configuration value '{BusyTimeoutConfigurationKey}' must be a non-negative whole number of seconds, but was '{configuredBusyTimeout}'.");
        }

        return TimeSpan.FromSeconds(busyTimeoutSeconds);
    }
}
