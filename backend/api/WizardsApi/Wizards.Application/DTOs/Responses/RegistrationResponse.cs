using Wizards.Domain.Entities;

namespace Wizards.Application.DTOs.Responses;

/// <summary>
/// A registration held against an event, as returned to API callers.
/// </summary>
/// <param name="Name">The name the player registered under.</param>
public record RegistrationResponse(string Name)
{
    /// <summary>
    /// Projects a registration onto the shape returned to API callers.
    /// </summary>
    /// <param name="registration">The registration to project. Must not be <see langword="null"/>.</param>
    public RegistrationResponse(EventRegistration registration)
        : this(registration.Name)
    {
    }
}
