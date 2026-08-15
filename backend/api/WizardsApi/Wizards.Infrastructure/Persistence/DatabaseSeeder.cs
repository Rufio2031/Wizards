using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Wizards.Domain.Entities;
using Wizards.Domain.Enums;
using Wizards.Infrastructure.Extensions;

namespace Wizards.Infrastructure.Persistence;

/// <summary>
/// Writes the reference data the application cannot run without into the Wizards database.
/// </summary>
/// <remarks>
/// <para>
/// Seeding is idempotent and is expected to run on every host start, including against a database
/// that is already populated: each entity type is compared against what is already stored and only
/// the missing rows are inserted.
/// </para>
/// <para>
/// Each entity type is inserted in a single save, so its rows either all land or none of them do,
/// and a failure leaves that type exactly as it was. Nothing is caught here, so the caller decides
/// what a failure means for the host.
/// </para>
/// <para>
/// The read of what is already stored and the insert of what is missing are not one transaction,
/// which is safe only because a single host owns the database file. Two hosts seeding the same file
/// concurrently can both observe the same missing row and race to insert it, and the loser fails on
/// the unique index rather than skipping the row.
/// </para>
/// </remarks>
/// <param name="dbContext">The context to write the reference data through.</param>
/// <param name="logger">Records what was inserted, so an unexpected insert on a warm database is visible.</param>
internal sealed class DatabaseSeeder(AppDbContext dbContext, ILogger<DatabaseSeeder> logger)
{
    private static readonly IReadOnlyList<Func<GameType>> SeedGameTypes =
    [
        () => GameType.Create(
            "Magic: The Gathering",
            [
                GameTypeSetting.Create("format", "Format", SettingType.Enum, "Standard",
                    description: "Which card pool and deck-building rules the event is played under.",
                    options: ["Standard", "Modern", "Pioneer", "Commander", "Draft"]),
                GameTypeSetting.Create("deckSize", "Deck size", SettingType.Int, "60", 40, 250,
                    "Minimum cards in a player's deck. Commander decks are exactly 100."),
                GameTypeSetting.Create("minPlayersToStart", "Minimum players to start", SettingType.Int, "4", 2, 30)
            ]),
        () => GameType.Create(
            "Yu-Gi-Oh!",
            [
                GameTypeSetting.Create("format", "Format", SettingType.Enum, "Advanced",
                    description: "Which ban list and rule set the event is played under.",
                    options: ["Advanced", "Traditional", "Speed Duel"]),
                GameTypeSetting.Create("deckSize", "Main deck size", SettingType.Int, "40", 40, 60),
                GameTypeSetting.Create("minPlayersToStart", "Minimum players to start", SettingType.Int, "4", 2, 30)
            ]),
        () => GameType.Create(
            "Pokémon TCG",
            [
                GameTypeSetting.Create("format", "Format", SettingType.Enum, "Standard",
                    description: "Which sets are legal at the event.",
                    options: ["Standard", "Expanded", "Unlimited"]),
                GameTypeSetting.Create("deckSize", "Deck size", SettingType.Int, "60", 60, 60,
                    "Pokémon decks are exactly 60 cards, so this is fixed."),
                GameTypeSetting.Create("minPlayersToStart", "Minimum players to start", SettingType.Int, "4", 2, 30)
            ]),

        // Not a card game, and so exposes no deck size at all. Seeded to keep the claim that a new game
        // is data rather than code honest, since nothing else here would catch a setting shape that
        // only works for trading card games.
        () => GameType.Create(
            "Catan",
            [
                GameTypeSetting.Create("minPlayersToStart", "Minimum players to start", SettingType.Int, "3", 3, 6),
                GameTypeSetting.Create("maxPlayers", "Maximum players", SettingType.Int, "4", 3, 6,
                    "The base game seats four. Five and six need an expansion."),
                GameTypeSetting.Create("victoryPointsToWin", "Victory points to win", SettingType.Int, "10", 8, 15,
                    "Raise this for a longer game."),
                GameTypeSetting.Create("usesExpansion", "Uses an expansion", SettingType.Bool, "false")
            ])
    ];

    /// <summary>
    /// The events written when sample data is asked for, described relative to the moment the seed
    /// runs so that a seed on any day places each one the same distance from the day it ran.
    /// </summary>
    /// <remarks>
    /// Between them these cover the three states the registration screens have to render: an event
    /// with room, one that is full, and one nobody has registered for. One sample is dated before the
    /// seed runs, so that whatever only lists events still to come can be seen leaving it out.
    /// </remarks>
    private static readonly IReadOnlyList<SampleEvent> SampleEvents =
    [
        new SampleEvent(
            "Friday Night Magic",
            "Weekly in-store tournament. Doors open thirty minutes before the first round.",
            "The Wizard's Table, 412 Main Street",
            "Magic: The Gathering",
            TimeSpan.FromDays(2),
            TimeSpan.FromHours(4),
            16,
            new Dictionary<string, string> { ["format"] = "Modern" },
            ["Ada Lovelace", "Grace Hopper", "Alan Turing", "Katherine Johnson", "Edsger Dijkstra"]),

        new SampleEvent(
            "Commander Pod Night",
            "One pod, four seats, no substitutions.",
            "The Wizard's Table, 412 Main Street",
            "Magic: The Gathering",
            TimeSpan.FromDays(5),
            TimeSpan.FromHours(3),
            4,
            new Dictionary<string, string> { ["format"] = "Commander", ["deckSize"] = "100" },
            ["Barbara Liskov", "Donald Knuth", "Margaret Hamilton", "Ken Thompson"]),

        new SampleEvent(
            "Catan Saturday",
            null,
            "The Wizard's Table, back room",
            "Catan",
            TimeSpan.FromDays(6),
            TimeSpan.FromHours(4),
            6,
            null,
            []),

        new SampleEvent(
            "Past Event: Draft Night",
            "This event has already finished and should not appear among upcoming events.",
            "The Wizard's Table, 412 Main Street",
            "Magic: The Gathering",
            TimeSpan.FromDays(-3),
            TimeSpan.FromHours(3),
            8,
            new Dictionary<string, string> { ["format"] = "Draft", ["deckSize"] = "40" },
            ["Ada Lovelace", "Grace Hopper", "Alan Turing"])
    ];

    private static readonly StringComparer GameTypeNameComparer = StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Inserts every piece of reference data that is not already stored.
    /// </summary>
    /// <remarks>
    /// Seed values that name the same thing collapse to a single row. Names are compared without
    /// regard to case or surrounding whitespace, so a hand-maintained source listing a name twice
    /// under two spellings inserts it once rather than failing on the unique index.
    /// </remarks>
    /// <param name="cancellationToken">Cancels the seed before it completes.</param>
    /// <returns>A task that completes once the inserted rows, if any, are durable.</returns>
    /// <exception cref="Domain.Exceptions.DomainException">
    /// Thrown when a configured seed value violates the invariants of the entity it creates. This is a
    /// defect in the seed data itself and is deliberately allowed to fail the host.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a sample event names a game type that is not seeded, which is the same kind of
    /// defect in the seed data.
    /// </exception>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await this.SeedGameTypesAsync(cancellationToken);
        await this.SeedSampleEventsAsync(cancellationToken);
    }

    private async Task SeedGameTypesAsync(CancellationToken cancellationToken)
    {
        if (SeedGameTypes.Count == 0)
        {
            return;
        }

        List<Records.GameType> storedGameTypes = await dbContext.GameTypes
            .Include(gameType => gameType.Settings)
            .ToListAsync(cancellationToken);

        // The unique index folds case with NOCASE, which covers only ASCII A-Z, where
        // OrdinalIgnoreCase folds the whole of Unicode. The two agree on ASCII names, and on
        // everything else this comparer treats more names as equal than the index does, so the
        // mismatch can only ever skip an insert rather than let a duplicate reach the index.
        Dictionary<string, Records.GameType> storedByName = new(GameTypeNameComparer);

        foreach (Records.GameType storedGameType in storedGameTypes)
        {
            storedByName.TryAdd(storedGameType.Name, storedGameType);
        }

        // Create trims, so deduplicating on the entity's name rather than the raw seed value also
        // collapses entries that differ only by surrounding whitespace.
        List<GameType> seedGameTypes = SeedGameTypes
            .Select(createGameType => createGameType())
            .DistinctBy(gameType => gameType.Name, GameTypeNameComparer)
            .ToList();

        int insertedGameTypes = 0;
        int insertedSettings = 0;

        foreach (GameType seedGameType in seedGameTypes)
        {
            Records.GameType seedRecord = seedGameType.ToRecord();

            if (!storedByName.TryGetValue(seedGameType.Name, out Records.GameType? storedGameType))
            {
                dbContext.GameTypes.Add(seedRecord);
                insertedGameTypes++;

                continue;
            }

            // A game type stored before it exposed any settings keeps its row and gains them here.
            // Settings are only ever added to a game type that has none, so a stored setting an
            // organizer's events already reference is never rewritten by a later seed.
            if (storedGameType.Settings.Count > 0)
            {
                continue;
            }

            foreach (Records.GameTypeSetting seedSetting in seedRecord.Settings)
            {
                seedSetting.GameTypeId = storedGameType.Id;
                dbContext.GameTypeSettings.Add(seedSetting);
                insertedSettings++;
            }
        }

        if (insertedGameTypes == 0 && insertedSettings == 0)
        {
            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded {InsertedGameTypeCount} game types and {InsertedSettingCount} settings for game types already stored.",
            insertedGameTypes,
            insertedSettings);
    }

    /// <summary>
    /// Writes the sample events, and the registrations held against them, into a database that holds
    /// no events at all.
    /// </summary>
    /// <remarks>
    /// An event has no natural key, and two events may legitimately share a name, so there is nothing
    /// to compare a sample against to decide whether it is already stored. Seeding only into an empty
    /// table is what makes this safe to run on every start: a database with any event in it, including
    /// one left after the samples were deleted on purpose, is left exactly as it is.
    /// </remarks>
    private async Task SeedSampleEventsAsync(CancellationToken cancellationToken)
    {
        if (SampleEvents.Count == 0 || await dbContext.Events.AnyAsync(cancellationToken))
        {
            return;
        }

        Dictionary<string, GameType> gameTypesByName = await this.ReadGameTypesByNameAsync(cancellationToken);

        // Every sample is dated from one reading, so events seeded together stay in the order they are
        // listed rather than drifting apart by however long the seed takes.
        DateTime seededAt = DateTime.UtcNow;

        List<(Records.Event Record, IReadOnlyList<EventRegistration> Registrations)> pendingEvents = [];

        foreach (SampleEvent sampleEvent in SampleEvents)
        {
            if (!gameTypesByName.TryGetValue(sampleEvent.GameTypeName, out GameType? gameType))
            {
                throw new InvalidOperationException(
                    $"Sample event '{sampleEvent.Name}' names the game type '{sampleEvent.GameTypeName}', which is not seeded.");
            }

            Event @event = BuildSampleEvent(sampleEvent, gameType, seededAt);

            Records.Event eventRecord = @event.ToRecord();

            dbContext.Events.Add(eventRecord);

            pendingEvents.Add((
                eventRecord,
                sampleEvent.RegisteredPlayers
                    .Select(player => EventRegistration.Create(@event, player))
                    .ToList()));
        }

        // The events are saved on their own so that the database assigns the keys the registrations
        // point at. The entities the registrations were created from carry no key, since an entity is
        // never handed the one its record was given.
        await dbContext.SaveChangesAsync(cancellationToken);

        int insertedRegistrations = 0;

        foreach ((Records.Event eventRecord, IReadOnlyList<EventRegistration> registrations) in pendingEvents)
        {
            foreach (EventRegistration registration in registrations)
            {
                dbContext.EventRegistrations.Add(new Records.EventRegistration
                {
                    EventId = eventRecord.Id,
                    Name = registration.Name
                });

                insertedRegistrations++;
            }
        }

        if (insertedRegistrations > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "Seeded {InsertedEventCount} sample events and {InsertedRegistrationCount} registrations held against them.",
            pendingEvents.Count,
            insertedRegistrations);
    }

    /// <summary>
    /// Builds the event a sample describes, dated from the instant the whole seed was read.
    /// </summary>
    /// <remarks>
    /// A sample dated before the seed ran is rebuilt rather than created, because an event that has
    /// already begun is a state the entity refuses to be created in and only ever reaches by being read
    /// back. It is built with no key, exactly as a created one is, so the database still assigns it.
    /// </remarks>
    /// <param name="sampleEvent">The sample to build.</param>
    /// <param name="gameType">The game type the sample names, already rehydrated with its settings.</param>
    /// <param name="seededAt">The instant every sample in this seed is dated from.</param>
    /// <returns>The event entity, carrying one selection per setting its game type exposes.</returns>
    private static Event BuildSampleEvent(SampleEvent sampleEvent, GameType gameType, DateTime seededAt)
    {
        DateTime startDateTime = seededAt + sampleEvent.StartsIn;
        DateTime endDateTime = startDateTime + sampleEvent.Runs;

        IReadOnlyList<EventGameTypeSelection> selections = gameType.Validate(
            sampleEvent.Selections?.Select(
                selection => EventGameTypeSelection.Create(selection.Key, selection.Value)));

        if (sampleEvent.StartsIn < TimeSpan.Zero)
        {
            return Event.Reconstitute(
                0,
                Guid.CreateVersion7(),
                sampleEvent.Name,
                sampleEvent.Description,
                sampleEvent.Location,
                startDateTime,
                endDateTime,
                gameType,
                sampleEvent.RegistrationLimit,
                selections);
        }

        return Event.Create(
            sampleEvent.Name,
            sampleEvent.Description,
            sampleEvent.Location,
            gameType,
            startDateTime,
            endDateTime,
            sampleEvent.RegistrationLimit,
            selections);
    }

    private async Task<Dictionary<string, GameType>> ReadGameTypesByNameAsync(CancellationToken cancellationToken)
    {
        List<Records.GameType> storedGameTypes = await dbContext.GameTypes
            .AsNoTracking()
            .Include(gameType => gameType.Settings)
                .ThenInclude(setting => setting.Options)
            .ToListAsync(cancellationToken);

        Dictionary<string, GameType> gameTypesByName = new(GameTypeNameComparer);

        foreach (Records.GameType storedGameType in storedGameTypes)
        {
            gameTypesByName.TryAdd(storedGameType.Name, storedGameType.ToEntity());
        }

        return gameTypesByName;
    }

    /// <param name="StartsIn">
    /// How long after the seed runs the event begins. Negative for an event that had already begun by
    /// the time it was seeded.
    /// </param>
    /// <param name="Runs">How long the event lasts once it has begun.</param>
    /// <param name="Selections">
    /// The settings to settle for the event, or <see langword="null"/> to leave every setting the game
    /// type exposes at its default.
    /// </param>
    /// <param name="RegisteredPlayers">
    /// The names to register, in order. Must not outnumber <paramref name="RegistrationLimit"/>, which
    /// the database enforces on the way in regardless.
    /// </param>
    private sealed record SampleEvent(
        string Name,
        string? Description,
        string Location,
        string GameTypeName,
        TimeSpan StartsIn,
        TimeSpan Runs,
        int RegistrationLimit,
        IReadOnlyDictionary<string, string>? Selections,
        IReadOnlyList<string> RegisteredPlayers);
}
