using Wizards.Domain.Entities;
using Wizards.Domain.Exceptions;

namespace WizardsApi.Tests.Domain;

public sealed class EventRegistrationTests
{
    private static readonly Guid IdempotencyKey = new("6b1f9f0e-3a2c-4d5b-8e7f-1a2b3c4d5e6f");

    [Fact]
    public void Create_EventIsNull_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => EventRegistration.Create(null!, "Ada Lovelace", IdempotencyKey));

        Assert.Equal("@event", exception.ParamName);
    }

    [Fact]
    public void Create_IdempotencyKeyIsEmpty_ThrowsDomainExceptionKeyedToTheIdempotencyKey()
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => EventRegistration.Create(AnyEvent(), "Ada Lovelace", Guid.Empty));

        Assert.Equal("An idempotency key is required to register.", exception.Message);
        Assert.Equal("IdempotencyKey", exception.Key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NameIsMissing_ThrowsDomainExceptionKeyedToTheName(string? name)
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => EventRegistration.Create(AnyEvent(), name!, IdempotencyKey));

        Assert.Equal("A name is required to register.", exception.Message);
        Assert.Equal("Name", exception.Key);
    }

    [Fact]
    public void Create_NameIsLongerThanTheMaximum_ThrowsDomainExceptionKeyedToTheName()
    {
        string name = new('a', EventRegistration.MaxNameLength + 1);

        DomainException exception = Assert.Throws<DomainException>(
            () => EventRegistration.Create(AnyEvent(), name, IdempotencyKey));

        Assert.Equal(
            $"A name cannot exceed {EventRegistration.MaxNameLength} characters.",
            exception.Message);
        Assert.Equal("Name", exception.Key);
    }

    [Fact]
    public void Create_NameOnlyFitsOnceTrimmed_IsAcceptedAndCarriesTheTrimmedName()
    {
        string name = new('a', EventRegistration.MaxNameLength);

        EventRegistration registration = EventRegistration.Create(
            AnyEvent(),
            $"   {name}   ",
            IdempotencyKey);

        Assert.Equal(name, registration.Name);
    }

    [Fact]
    public void Create_DetailsAreValid_CarriesTheEventNameAndKey()
    {
        Event @event = AnyEvent();

        EventRegistration registration = EventRegistration.Create(
            @event,
            "Ada Lovelace",
            IdempotencyKey);

        Assert.Same(@event, registration.Event);
        Assert.Equal("Ada Lovelace", registration.Name);
        Assert.Equal(IdempotencyKey, registration.IdempotencyKey);
    }

    [Fact]
    public void Create_TwoRegistrationsCarryDifferentKeys_KeepsThemApart()
    {
        Event @event = AnyEvent();

        EventRegistration first = EventRegistration.Create(@event, "Ada Lovelace", IdempotencyKey);
        EventRegistration second = EventRegistration.Create(
            @event,
            "Ada Lovelace",
            Guid.CreateVersion7());

        Assert.NotEqual(first.IdempotencyKey, second.IdempotencyKey);
    }

    [Fact]
    public void Reconstitute_StoredStateBreaksTheRulesCreateEnforces_AppliesNoValidation()
    {
        Event @event = AnyEvent();

        EventRegistration registration = EventRegistration.Reconstitute(@event, "   ", Guid.Empty);

        Assert.Same(@event, registration.Event);
        Assert.Equal("   ", registration.Name);
        Assert.Equal(Guid.Empty, registration.IdempotencyKey);
    }

    private static Event AnyEvent()
    {
        DateTime start = DateTime.UtcNow.AddDays(7);

        return Event.Create(
            "Friday Night Magic",
            null,
            "The Back Room",
            GameType.Create("Magic: The Gathering"),
            start,
            start.AddHours(3),
            8);
    }
}
