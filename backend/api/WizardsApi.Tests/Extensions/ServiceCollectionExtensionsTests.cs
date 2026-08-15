using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Wizards.Application.Extensions;
using Wizards.Application.Interfaces;
using Wizards.Application.Models;

namespace WizardsApi.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    private const string UidDomainKey = "CalendarInvite:UidDomain";

    private const string OrganizerEmailAddressKey = "CalendarInvite:OrganizerEmailAddress";

    private const string OrganizerNameKey = "CalendarInvite:OrganizerName";

    private readonly IServiceCollection services = new ServiceCollection();

    [Fact]
    public void AddApplication_ServicesAreNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => _ = default(IServiceCollection)!.AddApplication(Configuration()));
    }

    [Fact]
    public void AddApplication_ConfigurationIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _ = this.services.AddApplication(null!));
    }

    [Fact]
    public void AddApplication_ConfigurationIsValid_RegistersTheConfiguredCalendarInviteSettings()
    {
        this.services.AddApplication(Configuration());

        CalendarInviteSettings settings = this.RegisteredSettings();

        Assert.Equal("wizards.local", settings.UidDomain);
        Assert.Equal(new Uri("mailto:events@wizards.local"), settings.OrganizerAddress);
        Assert.Equal("The Wizard's Table", settings.OrganizerName);
    }

    [Fact]
    public void AddApplication_ConfigurationIsValid_RegistersEachApplicationService()
    {
        this.services.AddApplication(Configuration());

        Assert.Equal(ServiceLifetime.Scoped, this.LifetimeOf<IEventsService>());
        Assert.Equal(ServiceLifetime.Scoped, this.LifetimeOf<IGameTypesService>());
        Assert.Equal(ServiceLifetime.Scoped, this.LifetimeOf<IRegistrationsService>());
        Assert.Equal(ServiceLifetime.Scoped, this.LifetimeOf<ICalendarInviteService>());
        Assert.Equal(ServiceLifetime.Singleton, this.LifetimeOf<TimeProvider>());
    }

    [Fact]
    public void AddApplication_ATimeProviderIsAlreadyRegistered_LeavesItInPlace()
    {
        FakeTimeProvider registered = new();

        this.services.AddSingleton<TimeProvider>(registered);

        this.services.AddApplication(Configuration());

        Assert.Same(
            registered,
            Assert.Single(this.services, descriptor => descriptor.ServiceType == typeof(TimeProvider))
                .ImplementationInstance);
    }

    [Fact]
    public void AddApplication_ConfigurationIsValid_ReturnsTheSameCollection()
    {
        Assert.Same(this.services, this.services.AddApplication(Configuration()));
    }

    [Fact]
    public void AddApplication_UidDomainKeyIsAbsent_ThrowsNamingTheKey()
    {
        Assert.Contains(UidDomainKey, this.Rejects(Configuration(uidDomain: null)).Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void AddApplication_UidDomainIsBlank_ThrowsNamingTheKey(string uidDomain)
    {
        Assert.Contains(
            UidDomainKey,
            this.Rejects(Configuration(uidDomain: uidDomain)).Message,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("wizards local")]
    [InlineData(" wizards.local")]
    [InlineData("wizards.local ")]
    public void AddApplication_UidDomainCarriesWhitespace_ThrowsNamingTheKey(string uidDomain)
    {
        Assert.Contains(
            UidDomainKey,
            this.Rejects(Configuration(uidDomain: uidDomain)).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void AddApplication_OrganizerEmailAddressKeyIsAbsent_ThrowsNamingTheKey()
    {
        Assert.Contains(
            OrganizerEmailAddressKey,
            this.Rejects(Configuration(organizerEmailAddress: null)).Message,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void AddApplication_OrganizerEmailAddressIsBlank_ThrowsNamingTheKey(string organizerEmailAddress)
    {
        Assert.Contains(
            OrganizerEmailAddressKey,
            this.Rejects(Configuration(organizerEmailAddress: organizerEmailAddress)).Message,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("The Wizard <events@wizards.local>")]
    [InlineData("<events@wizards.local>")]
    [InlineData("\"The Wizard\" <events@wizards.local>")]
    public void AddApplication_OrganizerEmailAddressCarriesADisplayName_ThrowsNamingTheKey(
        string organizerEmailAddress)
    {
        Assert.Contains(
            OrganizerEmailAddressKey,
            this.Rejects(Configuration(organizerEmailAddress: organizerEmailAddress)).Message,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("events")]
    [InlineData("events@")]
    [InlineData("@wizards.local")]
    [InlineData("events@wizards.local, second@wizards.local")]
    [InlineData("mailto:events@wizards.local")]
    [InlineData(" events@wizards.local ")]
    public void AddApplication_OrganizerEmailAddressDoesNotRoundTrip_ThrowsNamingTheKey(string organizerEmailAddress)
    {
        Assert.Contains(
            OrganizerEmailAddressKey,
            this.Rejects(Configuration(organizerEmailAddress: organizerEmailAddress)).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void AddApplication_OrganizerNameKeyIsAbsent_ThrowsNamingTheKey()
    {
        Assert.Contains(
            OrganizerNameKey,
            this.Rejects(Configuration(organizerName: null)).Message,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddApplication_OrganizerNameIsBlank_ThrowsNamingTheKey(string organizerName)
    {
        Assert.Contains(
            OrganizerNameKey,
            this.Rejects(Configuration(organizerName: organizerName)).Message,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("The \"Wizard's\" Table")]
    [InlineData("\"")]
    public void AddApplication_OrganizerNameCarriesADoubleQuote_ThrowsNamingTheKey(string organizerName)
    {
        Assert.Contains(
            OrganizerNameKey,
            this.Rejects(Configuration(organizerName: organizerName)).Message,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("The\nWizard's Table")]
    [InlineData("The\rWizard's Table")]
    [InlineData("The\u0000Wizard's Table")]
    [InlineData("The\u0007Wizard's Table")]
    [InlineData("The\u007fWizard's Table")]
    public void AddApplication_OrganizerNameCarriesAControlCharacter_ThrowsNamingTheKey(string organizerName)
    {
        Assert.Contains(
            OrganizerNameKey,
            this.Rejects(Configuration(organizerName: organizerName)).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void AddApplication_OrganizerNameCarriesATab_IsAcceptedBecauseRfc5545AllowsWhitespace()
    {
        this.services.AddApplication(Configuration(organizerName: "The\tWizard's Table"));

        Assert.Equal("The\tWizard's Table", this.RegisteredSettings().OrganizerName);
    }

    [Fact]
    public void AddApplication_ConfigurationIsRejected_RegistersNothing()
    {
        this.Rejects(Configuration(organizerEmailAddress: null));

        Assert.Empty(this.services);
    }

    private InvalidOperationException Rejects(IConfiguration configuration) =>
        Assert.Throws<InvalidOperationException>(() => this.services.AddApplication(configuration));

    private CalendarInviteSettings RegisteredSettings() =>
        Assert.IsType<CalendarInviteSettings>(
            Assert.Single(this.services, descriptor => descriptor.ServiceType == typeof(CalendarInviteSettings))
                .ImplementationInstance);

    private ServiceLifetime LifetimeOf<TService>() =>
        Assert.Single(this.services, descriptor => descriptor.ServiceType == typeof(TService)).Lifetime;

    private static IConfiguration Configuration(
        string? uidDomain = "wizards.local",
        string? organizerEmailAddress = "events@wizards.local",
        string? organizerName = "The Wizard's Table")
    {
        Dictionary<string, string?> values = [];

        if (uidDomain is not null)
        {
            values[UidDomainKey] = uidDomain;
        }

        if (organizerEmailAddress is not null)
        {
            values[OrganizerEmailAddressKey] = organizerEmailAddress;
        }

        if (organizerName is not null)
        {
            values[OrganizerNameKey] = organizerName;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private sealed class FakeTimeProvider : TimeProvider;
}
