using Wizards.Application.Enums;
using Wizards.Application.Models;

namespace WizardsApi.Tests.Models;

public sealed class RegistrationErrorsTests
{
    [Fact]
    public void EventNotFound_IsANotFoundAttributedToTheRequestAsAWhole()
    {
        Assert.Equal(ErrorKind.NotFound, RegistrationErrors.EventNotFound.Kind);
        Assert.Equal(string.Empty, RegistrationErrors.EventNotFound.Key);
        Assert.Equal(
            "No event is scheduled under that identifier.",
            RegistrationErrors.EventNotFound.Message);
    }

    [Fact]
    public void EventFull_IsAConflictAttributedToTheRequestAsAWhole()
    {
        Assert.Equal(ErrorKind.Conflict, RegistrationErrors.EventFull.Kind);
        Assert.Equal(string.Empty, RegistrationErrors.EventFull.Key);
        Assert.Equal("This event is full.", RegistrationErrors.EventFull.Message);
    }

    [Fact]
    public void RegistrationClosed_IsAConflictAttributedToTheRequestAsAWhole()
    {
        Assert.Equal(ErrorKind.Conflict, RegistrationErrors.RegistrationClosed.Kind);
        Assert.Equal(string.Empty, RegistrationErrors.RegistrationClosed.Key);
        Assert.Equal(
            "Registration for this event has closed.",
            RegistrationErrors.RegistrationClosed.Message);
    }

    [Fact]
    public void RegistrationClosed_ComparedToEventFull_MatchesItsShapeAndDiffersOnlyInTheMessage()
    {
        Assert.Equal(RegistrationErrors.EventFull.Kind, RegistrationErrors.RegistrationClosed.Kind);
        Assert.Equal(RegistrationErrors.EventFull.Key, RegistrationErrors.RegistrationClosed.Key);
        Assert.NotEqual(
            RegistrationErrors.EventFull.Message,
            RegistrationErrors.RegistrationClosed.Message);
        Assert.NotEqual(RegistrationErrors.EventFull, RegistrationErrors.RegistrationClosed);
    }

    [Fact]
    public void Invalid_MessageIsNull_ThrowsArgumentNullExceptionNamingTheMessage()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => RegistrationErrors.Invalid(null!));

        Assert.Equal("message", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Invalid_MessageIsBlank_ThrowsArgumentExceptionNamingTheMessage(string message)
    {
        ArgumentException exception =
            Assert.Throws<ArgumentException>(() => RegistrationErrors.Invalid(message));

        Assert.Equal("message", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Invalid_FieldIsSuppliedButBlank_ThrowsArgumentExceptionNamingTheField(string field)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => RegistrationErrors.Invalid("A name is required to register.", field));

        Assert.Equal("field", exception.ParamName);
    }

    [Fact]
    public void Invalid_FieldIsOmitted_AttributesTheFailureToTheRequestAsAWhole()
    {
        ApplicationError error = RegistrationErrors.Invalid("A name is required to register.");

        Assert.Equal(ErrorKind.Invalid, error.Kind);
        Assert.Equal(string.Empty, error.Key);
        Assert.Equal("A name is required to register.", error.Message);
    }

    [Fact]
    public void Invalid_FieldIsSupplied_AttributesTheFailureToIt()
    {
        ApplicationError error = RegistrationErrors.Invalid(
            "An idempotency key is required to register.",
            "IdempotencyKey");

        Assert.Equal(ErrorKind.Invalid, error.Kind);
        Assert.Equal("IdempotencyKey", error.Key);
        Assert.Equal("An idempotency key is required to register.", error.Message);
    }
}
