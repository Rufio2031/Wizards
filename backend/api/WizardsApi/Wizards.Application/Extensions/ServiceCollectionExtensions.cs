using Microsoft.Extensions.DependencyInjection;

using Wizards.Application.Interfaces;
using Wizards.Application.Services;

namespace Wizards.Application.Extensions;

/// <summary>
/// Registers the application layer's services into a host's dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers every application service against the interface its callers depend on, keeping the
    /// implementations internal to this assembly.
    /// </summary>
    /// <remarks>
    /// The services registered here depend on the repository and unit-of-work abstractions, which a
    /// separate infrastructure registration satisfies. Calling this without also registering
    /// persistence leaves the container unable to resolve them.
    /// </remarks>
    /// <param name="services">The container to register the application services into.</param>
    /// <returns>The same <paramref name="services"/> instance, so registrations can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IEventsService, EventsService>();
        services.AddScoped<IGameTypesService, GameTypesService>();

        return services;
    }
}
