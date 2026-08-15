using System.ComponentModel.DataAnnotations;

using Wizards.Application.Validation;

namespace WizardsApi.Tests.Validation;

public sealed class RequiredValueAttributeTests
{
    private readonly RequiredValueAttribute attribute = new();

    [Fact]
    public void IsValid_ValueIsNull_IsRejected()
    {
        Assert.False(this.attribute.IsValid(null));
    }

    [Fact]
    public void IsValid_ValueIsANullableValueTypeCarryingNothing_IsRejected()
    {
        int? omitted = null;

        Assert.False(this.attribute.IsValid(omitted));
    }

    [Fact]
    public void IsValid_ValueIsANullableValueTypeCarryingItsUnderlyingDefault_IsRejected()
    {
        int? zero = 0;

        Assert.False(this.attribute.IsValid(zero));
    }

    [Fact]
    public void IsValid_ValueIsAReferenceType_IsAccepted()
    {
        Assert.True(this.attribute.IsValid("Friday Night Magic"));
    }

    [Fact]
    public void IsValid_ValueIsAnEmptyString_IsAcceptedBecauseOnlyValueTypesAreCheckedForTheirDefault()
    {
        Assert.True(this.attribute.IsValid(string.Empty));
    }

    [Fact]
    public void IsValid_ValueIsAnEmptyCollection_IsAccepted()
    {
        Assert.True(this.attribute.IsValid(Array.Empty<string>()));
    }

    [Fact]
    public void IsValid_DateTimeIsLeftAtItsDefault_IsRejected()
    {
        Assert.False(this.attribute.IsValid(default(DateTime)));
    }

    [Fact]
    public void IsValid_DateTimeIsSupplied_IsAccepted()
    {
        Assert.True(this.attribute.IsValid(new DateTime(2026, 8, 13, 16, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void IsValid_DateTimeIsOneTickPastItsDefault_IsAccepted()
    {
        Assert.True(this.attribute.IsValid(DateTime.MinValue.AddTicks(1)));
    }

    [Fact]
    public void IsValid_GuidIsLeftAtItsDefault_IsRejected()
    {
        Assert.False(this.attribute.IsValid(Guid.Empty));
    }

    [Fact]
    public void IsValid_GuidIsSupplied_IsAccepted()
    {
        Assert.True(this.attribute.IsValid(Guid.CreateVersion7()));
    }

    [Fact]
    public void IsValid_IntIsLeftAtItsDefault_IsRejected()
    {
        Assert.False(this.attribute.IsValid(0));
    }

    [Fact]
    public void IsValid_IntIsSupplied_IsAccepted()
    {
        Assert.True(this.attribute.IsValid(8));
    }

    [Fact]
    public void IsValid_IntIsNegative_IsAcceptedBecauseOnlyAbsenceIsChecked()
    {
        Assert.True(this.attribute.IsValid(-1));
    }

    [Theory]
    [MemberData(nameof(DefaultedValueTypes))]
    public void IsValid_AnyValueTypeIsLeftAtItsDefault_IsRejected(object defaulted)
    {
        Assert.False(this.attribute.IsValid(defaulted));
    }

    [Theory]
    [MemberData(nameof(SuppliedValueTypes))]
    public void IsValid_AnyValueTypeIsSupplied_IsAccepted(object supplied)
    {
        Assert.True(this.attribute.IsValid(supplied));
    }

    [Fact]
    public void IsValid_TheSameTypeIsCheckedRepeatedly_KeepsReportingTheSameOutcome()
    {
        Assert.False(this.attribute.IsValid(default(DateTime)));
        Assert.True(this.attribute.IsValid(DateTime.MinValue.AddTicks(1)));
        Assert.False(this.attribute.IsValid(default(DateTime)));
    }

    [Fact]
    public void FormatErrorMessage_FieldIsNamed_ReportsThatFieldAsRequired()
    {
        Assert.Equal(
            "The StartDateTime field is required.",
            this.attribute.FormatErrorMessage("StartDateTime"));
    }

    [Fact]
    public void GetValidationResult_ValueIsLeftAtItsDefault_ReportsTheFailureAgainstTheMember()
    {
        object instance = new();

        ValidationContext context = new(instance)
        {
            MemberName = "StartDateTime",
            DisplayName = "StartDateTime"
        };

        ValidationResult? result = this.attribute.GetValidationResult(default(DateTime), context);

        Assert.NotNull(result);
        Assert.Equal("The StartDateTime field is required.", result.ErrorMessage);
        Assert.Equal(["StartDateTime"], result.MemberNames);
    }

    [Fact]
    public void GetValidationResult_ValueIsSupplied_ReportsSuccess()
    {
        object instance = new();

        ValidationContext context = new(instance) { MemberName = "StartDateTime" };

        ValidationResult? result = this.attribute.GetValidationResult(
            new DateTime(2026, 8, 13, 16, 0, 0, DateTimeKind.Utc),
            context);

        Assert.Equal(ValidationResult.Success, result);
    }

    public static TheoryData<object> DefaultedValueTypes() =>
        [
            default(DateTime),
            default(DateTimeOffset),
            default(DateOnly),
            default(TimeOnly),
            default(TimeSpan),
            default(Guid),
            default(int),
            default(long),
            default(decimal),
            default(double),
            default(bool),
            default(char),
            default(DayOfWeek),
            default(Coordinates)
        ];

    public static TheoryData<object> SuppliedValueTypes() =>
        [
            new DateTime(2026, 8, 13, 16, 0, 0, DateTimeKind.Utc),
            new DateTimeOffset(2026, 8, 13, 16, 0, 0, TimeSpan.Zero),
            new DateOnly(2026, 8, 13),
            new TimeOnly(16, 0),
            TimeSpan.FromHours(3),
            Guid.CreateVersion7(),
            8,
            8L,
            8.5m,
            8.5d,
            true,
            'a',
            DayOfWeek.Friday,
            new Coordinates(0, 1)
        ];

    private readonly record struct Coordinates(double Latitude, double Longitude);
}
