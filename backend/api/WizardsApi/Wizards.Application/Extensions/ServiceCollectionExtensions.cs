using System.Net.Mail;

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

    private const string OrganizerEmailAddressConfigurationKey = "CalendarInvite:OrganizerEmailAddress";

    private const string OrganizerNameConfigurationKey = "CalendarInvite:OrganizerName";

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
        string uidDomain = ReadCalendarInviteValue(configuration, UidDomainConfigurationKey);

        if (uidDomain.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(
                $"Configuration value '{UidDomainConfigurationKey}' must be a domain carrying no whitespace, but was '{uidDomain}'.");
        }

        string organizerEmailAddress = ReadCalendarInviteValue(configuration, OrganizerEmailAddressConfigurationKey);

        if (!MailAddress.TryCreate(organizerEmailAddress, out MailAddress? organizer)
            || !string.Equals(organizer.Address, organizerEmailAddress, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Configuration value '{OrganizerEmailAddressConfigurationKey}' must be an email address carrying no display name, but was '{organizerEmailAddress}'.");
        }

        string organizerName = ReadCalendarInviteValue(configuration, OrganizerNameConfigurationKey);

        if (organizerName.Any(IsRejectedByCommonName))
        {
            throw new InvalidOperationException(
                $"Configuration value '{OrganizerNameConfigurationKey}' must carry no double quote or control character, but was '{organizerName}'.");
        }

        Uri organizerAddress = new($"{Uri.UriSchemeMailto}:{organizer.Address}");

        return new CalendarInviteSettings(uidDomain, organizerAddress, organizerName);
    }

    private static string ReadCalendarInviteValue(IConfiguration configuration, string key)
    {
        string? value = configuration[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Configuration value '{key}' was not found. Add it under the 'CalendarInvite' section of appsettings.json.");
        }

        return value;
    }

    /// <summary>
    /// Reports whether a character cannot survive an iCalendar parameter value, which RFC 5545 allows
    /// no double quote and no control character other than a tab.
    /// </summary>
    private static bool IsRejectedByCommonName(char character) =>
        character == '"' || (char.IsControl(character) && character != '\t');
}
