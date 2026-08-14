using Wizards.Application.DTOs.Requests;
using Wizards.Application.DTOs.Responses;
using Wizards.Application.Interfaces;
using Wizards.Application.Models;
using Wizards.Domain.Entities;
using Wizards.Domain.Exceptions;
using Wizards.Domain.Interfaces.Repositories;
using Wizards.Domain.Models;

namespace Wizards.Application.Services;

internal sealed class EventsService(
    IEventsRepository eventsRepository,
    IGameTypesRepository gameTypesRepository,
    IUnitOfWork unitOfWork) : IEventsService
{
    /// <inheritdoc />
    public async Task<Page<EventResponse>> GetEvents(
        GetEventsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        EventQuery query = new(
            request.Skip,
            request.Take,
            request.SortBy,
            request.SortDirection,
            request.StartingOnOrAfterUtc,
            request.StartingBeforeUtc);

        EventPage page = await eventsRepository.GetEventsAsync(query, cancellationToken);

        return new Page<EventResponse>(
            page.Events.Select(@event => new EventResponse(@event)).ToList(),
            new PaginationMeta(request.Skip, request.Take, page.TotalCount));
    }

    /// <inheritdoc />
    public async Task<EventResponse?> GetEvent(Guid eventId, CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event identifier cannot be empty.", nameof(eventId));
        }

        Event? @event = await eventsRepository.GetEventByPublicIdAsync(eventId, cancellationToken);

        return @event is null ? null : new EventResponse(@event);
    }

    /// <inheritdoc />
    public async Task<EventWriteResult> AddEvent(CreateEventRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        GameType? gameType = await gameTypesRepository.GetGameTypeByPublicIdAsync(
            request.GameType.GameTypeId,
            cancellationToken);

        if (gameType is null)
        {
            return EventWriteResult.Failure(EventErrors.GameTypeNotFound);
        }

        Event @event;

        try
        {
            IReadOnlyList<EventGameTypeSelection> selections = gameType.Validate(
                request.GameType.Selections?.Select(
                    selection => EventGameTypeSelection.Create(selection.Key, selection.Value)));

            @event = Event.Create(
                request.Name,
                request.Description,
                request.Location,
                gameType,
                request.StartDateTime,
                request.EndDateTime,
                request.RegistrationLimit,
                selections);
        }
        catch (DomainException exception)
        {
            return EventWriteResult.Failure(
                exception.Key is { } settingKey
                    ? EventErrors.InvalidSelection(exception.Message, settingKey)
                    : EventErrors.Invalid(exception.Message));
        }

        await eventsRepository.AddEventAsync(@event, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return EventWriteResult.Success(new EventResponse(@event));
    }
}
