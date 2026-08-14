using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Wizards.Application.Interfaces;
using Wizards.Application.Models;
using Wizards.Application.Services;

namespace Wizards.Application.Extensions;

/// <summary>
/// Registers the application layer's services into a host's dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string UidDomainConfigurationKey = "CalendarInvite:UidDomain";

    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton(ResolveCalendarInviteSettings(configuration));
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IEventsService, EventsService>();
        services.AddScoped<IGameTypesService, GameTypesService>();
        services.AddScoped<IRegistrationsService, RegistrationsService>();
        services.AddScoped<ICalendarInviteService, CalendarInviteService>();

        return services;
    }

    private static CalendarInviteSettings ResolveCalendarInviteSettings(IConfiguration configuration)
    {
        string? uidDomain = configuration[UidDomainConfigurationKey];

        if (string.IsNullOrWhiteSpace(uidDomain))
        {
            throw new InvalidOperationException(
                $"Configuration value '{UidDomainConfigurationKey}' was not found. Add it under the 'CalendarInvite' section of appsettings.json.");
        }

        if (uidDomain.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(
                $"Configuration value '{UidDomainConfigurationKey}' must be a domain carrying no whitespace, but was '{uidDomain}'.");
        }

        return new CalendarInviteSettings(uidDomain);
    }
}
