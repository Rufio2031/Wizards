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
    public async Task<WriteResult<RegistrationResponse>> AddRegistration(
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
            return WriteResult<RegistrationResponse>.Failure(RegistrationErrors.EventNotFound);
        }

        WriteResult<RegistrationResponse>? heldRegistration =
            await this.ReadRegistrationHeldByKey(@event, request.IdempotencyKey, cancellationToken);

        if (heldRegistration is not null)
        {
            return heldRegistration;
        }

        EventRegistration registration;

        try
        {
            registration = EventRegistration.Create(@event, request.Name, request.IdempotencyKey);
        }
        catch (DomainException exception)
        {
            return WriteResult<RegistrationResponse>.Failure(
                RegistrationErrors.Invalid(exception.Message, exception.Key));
        }

        if (@event.IsRegistrationClosed)
        {
            return WriteResult<RegistrationResponse>.Failure(RegistrationErrors.RegistrationClosed);
        }

        int registrationCount = await registrationsRepository.CountRegistrationsAsync(
            @event,
            cancellationToken);

        if (@event.IsFull(registrationCount))
        {
            return WriteResult<RegistrationResponse>.Failure(RegistrationErrors.EventFull);
        }

        await registrationsRepository.AddRegistrationAsync(registration, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (StoreRuleViolationException)
        {
            heldRegistration = await this.ReadRegistrationHeldByKey(
                @event,
                request.IdempotencyKey,
                cancellationToken);

            return heldRegistration
                ?? WriteResult<RegistrationResponse>.Failure(RegistrationErrors.EventFull);
        }
        catch (StoreUniquenessViolationException)
        {
            heldRegistration = await this.ReadRegistrationHeldByKey(
                @event,
                request.IdempotencyKey,
                cancellationToken);

            if (heldRegistration is null)
            {
                throw;
            }

            return heldRegistration;
        }

        return WriteResult<RegistrationResponse>.Success(new RegistrationResponse(registration));
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

    private async Task<WriteResult<RegistrationResponse>?> ReadRegistrationHeldByKey(
        Event @event,
        Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        EventRegistration? heldRegistration =
            await registrationsRepository.GetRegistrationByIdempotencyKeyAsync(
                @event,
                idempotencyKey,
                cancellationToken);

        return heldRegistration is null
            ? null
            : WriteResult<RegistrationResponse>.Success(new RegistrationResponse(heldRegistration));
    }
}
