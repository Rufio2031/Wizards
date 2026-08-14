using Wizards.Application.DTOs.Requests;
using Wizards.Application.DTOs.Responses;
using Wizards.Application.Interfaces;
using Wizards.Application.Models;
using Wizards.Domain.Entities;
using Wizards.Domain.Exceptions;
using Wizards.Domain.Interfaces.Repositories;

namespace Wizards.Application.Services;

internal sealed class RegistrationsService(
    IEventsRepository eventsRepository,
    IEventRegistrationsRepository registrationsRepository,
    IUnitOfWork unitOfWork) : IRegistrationsService
{
    /// <inheritdoc />
    public async Task<ApplicationError?> AddRegistration(
        Guid eventId,
        CreateRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event identifier cannot be empty.", nameof(eventId));
        }

        Event? @event = await eventsRepository.GetEventByPublicIdAsync(eventId, cancellationToken);

        if (@event is null)
        {
            return RegistrationErrors.EventNotFound;
        }

        int registrationCount = await registrationsRepository.CountRegistrationsAsync(
            @event,
            cancellationToken);

        if (@event.IsFull(registrationCount))
        {
            return RegistrationErrors.EventFull;
        }

        EventRegistration registration;

        try
        {
            registration = EventRegistration.Create(@event, request.Name);
        }
        catch (DomainException exception)
        {
            return RegistrationErrors.Invalid(exception.Message, exception.Key);
        }

        await registrationsRepository.AddRegistrationAsync(registration, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (StoreRuleViolationException)
        {
            return RegistrationErrors.EventFull;
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RegistrationResponse>?> GetRegistrations(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event identifier cannot be empty.", nameof(eventId));
        }

        Event? @event = await eventsRepository.GetEventByPublicIdAsync(eventId, cancellationToken);

        if (@event is null)
        {
            return null;
        }

        IReadOnlyList<EventRegistration> registrations =
            await registrationsRepository.GetRegistrationsAsync(@event, cancellationToken);

        return registrations
            .Select(registration => new RegistrationResponse(registration))
            .ToList();
    }
}
