using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    // TODO: This entry is hand-written because .claude/skills/seed-data/references/data.json, the
    // source of the game type reference data, is still empty. Take the remaining game types from
    // there once it is populated. This list is the only place they need to land.
    private static readonly IReadOnlyList<string> SeedGameTypeNames = ["Magic: The Gathering"];

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
    /// <exception cref="ArgumentException">
    /// Thrown when a configured seed value violates the invariants of the entity it creates. This is a
    /// defect in the seed data itself and is deliberately allowed to fail the host.
    /// </exception>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await this.SeedGameTypesAsync(cancellationToken);
    }

    private async Task SeedGameTypesAsync(CancellationToken cancellationToken)
    {
        if (SeedGameTypeNames.Count == 0)
        {
            return;
        }

        // The unique index folds case with NOCASE, which covers only ASCII A-Z, where
        // OrdinalIgnoreCase folds the whole of Unicode. The two agree on ASCII names, and on
        // everything else this comparer treats more names as equal than the index does, so the
        // mismatch can only ever skip an insert rather than let a duplicate reach the index.
        HashSet<string> storedGameTypeNames = await dbContext.GameTypes
            .AsNoTracking()
            .Select(gameType => gameType.Name)
            .ToHashSetAsync(GameTypeNameComparer, cancellationToken);

        // Create trims, so deduplicating on the entity's name rather than the raw seed value also
        // collapses entries that differ only by surrounding whitespace.
        List<Domain.Entities.GameType> missingGameTypes = SeedGameTypeNames
            .Select(Domain.Entities.GameType.Create)
            .DistinctBy(gameType => gameType.Name, GameTypeNameComparer)
            .Where(gameType => !storedGameTypeNames.Contains(gameType.Name))
            .ToList();

        if (missingGameTypes.Count == 0)
        {
            return;
        }

        dbContext.GameTypes.AddRange(missingGameTypes.Select(gameType => gameType.ToRecord()));

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {MissingGameTypeCount} game types.", missingGameTypes.Count);
    }
}
