using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;

using Wizards.Application.DTOs.Requests;
using Wizards.Domain.Entities;

namespace WizardsApi.Tests.Models;

public sealed class CreateRegistrationRequestTests
{
    private static readonly JsonSerializerOptions WireOptions = new(JsonSerializerDefaults.Web);

    // The request's rules are written on the record's positional parameters, which plain
    // Validator.TryValidateObject never reads. Only the model validator the framework runs sees them.
    private static readonly IObjectModelValidator ModelValidator = new ServiceCollection()
        .AddLogging()
        .AddMvcCore()
        .AddDataAnnotations()
        .Services
        .BuildServiceProvider()
        .GetRequiredService<IObjectModelValidator>();

    [Fact]
    public void Validate_IdempotencyKeyIsAbsentFromTheBody_IsRejected()
    {
        CreateRegistrationRequest request = Deserialize("""{"name":"Ada Lovelace"}""");

        Assert.Equal(Guid.Empty, request.IdempotencyKey);
        Assert.Equal([nameof(CreateRegistrationRequest.IdempotencyKey)], RejectedFields(request));
    }

    [Fact]
    public void Validate_IdempotencyKeyIsTheAllZeroGuid_IsRejected()
    {
        CreateRegistrationRequest request = new("Ada Lovelace", Guid.Empty);

        Assert.Equal([nameof(CreateRegistrationRequest.IdempotencyKey)], RejectedFields(request));
    }

    [Fact]
    public void Validate_IdempotencyKeyIsSupplied_IsAccepted()
    {
        CreateRegistrationRequest request = new("Ada Lovelace", Guid.CreateVersion7());

        Assert.Empty(RejectedFields(request));
    }

    [Fact]
    public void Validate_BodyCarriesAKey_BindsItRatherThanDiscardingIt()
    {
        Guid idempotencyKey = new("6b1f9f0e-3a2c-4d5b-8e7f-1a2b3c4d5e6f");

        CreateRegistrationRequest request = Deserialize(
            $$"""{"name":"Ada Lovelace","idempotencyKey":"{{idempotencyKey}}"}""");

        Assert.Equal(idempotencyKey, request.IdempotencyKey);
        Assert.Empty(RejectedFields(request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NameIsMissing_IsRejected(string? name)
    {
        CreateRegistrationRequest request = new(name!, Guid.CreateVersion7());

        Assert.Equal([nameof(CreateRegistrationRequest.Name)], RejectedFields(request));
    }

    [Fact]
    public void Validate_NameIsLongerThanTheMaximum_IsRejected()
    {
        CreateRegistrationRequest request = new(
            new string('a', EventRegistration.MaxNameLength + 1),
            Guid.CreateVersion7());

        Assert.Equal([nameof(CreateRegistrationRequest.Name)], RejectedFields(request));
    }

    [Fact]
    public void Validate_NameIsAtTheMaximum_IsAccepted()
    {
        CreateRegistrationRequest request = new(
            new string('a', EventRegistration.MaxNameLength),
            Guid.CreateVersion7());

        Assert.Empty(RejectedFields(request));
    }

    private static CreateRegistrationRequest Deserialize(string body) =>
        JsonSerializer.Deserialize<CreateRegistrationRequest>(body, WireOptions)!;

    private static IReadOnlyList<string> RejectedFields(CreateRegistrationRequest request)
    {
        ActionContext actionContext = new();

        ModelValidator.Validate(actionContext, validationState: null, prefix: string.Empty, request);

        return [.. actionContext.ModelState
            .Where(field => field.Value?.Errors.Count > 0)
            .Select(field => field.Key)
            .Order()];
    }
}
