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
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await this.SeedGameTypesAsync(cancellationToken);
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

}
