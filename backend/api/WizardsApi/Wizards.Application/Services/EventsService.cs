using Wizards.Application.DTOs.Requests;
using Wizards.Application.DTOs.Responses;
using Wizards.Application.Interfaces;
using Wizards.Application.Models;
using Wizards.Domain.Entities;
using Wizards.Domain.Exceptions;
using Wizards.Domain.Interfaces.Repositories;
using Wizards.Domain.Models;

namespace Wizards.Application.Services;

/// <summary>
/// Reads and maintains the collection of events, resolving the game type each one is played with.
/// </summary>
/// <remarks>
/// Every write is committed through the unit of work before returning, so a returned result is
/// already durable. Instances are scoped alongside the repositories they orchestrate and are not
/// safe to share across threads or concurrent requests.
/// </remarks>
/// <param name="eventsRepository">The repository events are read from and staged against.</param>
/// <param name="gameTypesRepository">
/// The repository requested game type names are resolved against. Game types are only ever read, so
/// a name that is not registered fails the write rather than registering it.
/// </param>
/// <param name="unitOfWork">The unit of work that commits the staged writes.</param>
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

        EventPage page = await eventsRepository.GetEventsAsync(
            request.Skip,
            request.Take,
            cancellationToken);

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

        GameType? gameType = await gameTypesRepository.GetGameTypeByNameAsync(
            request.GameType.Name,
            cancellationToken);

        if (gameType is null)
        {
            return EventWriteResult.Failure(EventErrors.GameTypeNotFound);
        }

        Event @event;

        try
        {
            @event = Event.Create(
                request.Name,
                request.Description,
                gameType,
                request.StartDateTime,
                request.EndDateTime);
        }
        catch (DomainException exception)
        {
            return EventWriteResult.Failure(EventErrors.Invalid(exception.Message));
        }

        await eventsRepository.AddEventAsync(@event, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return EventWriteResult.Success(new EventResponse(@event));
    }
}
