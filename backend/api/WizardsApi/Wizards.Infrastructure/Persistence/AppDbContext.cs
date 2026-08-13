using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Wizards.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core context for the Wizards SQLite database.
/// </summary>
/// <remarks>
/// Instances are registered with a scoped lifetime and are not safe to share across threads or
/// concurrent requests.
/// </remarks>
/// <param name="options">
/// The configured context options, carrying the SQLite provider and the connection string to use.
/// Supplied by dependency injection; never <see langword="null"/>.
/// </param>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private const string CaseInsensitiveCollation = "NOCASE";

    /// <summary>The stored events.</summary>
    internal DbSet<Records.Event> Events => this.Set<Records.Event>();

    /// <summary>The stored game types the events reference.</summary>
    internal DbSet<Records.GameType> GameTypes => this.Set<Records.GameType>();

    /// <inheritdoc />
    /// <remarks>
    /// Applies <see cref="UtcDateTimeConverter"/> to every <see cref="DateTime"/> and nullable
    /// <see cref="DateTime"/> property in the model, so no mapping site has to remember to restore the
    /// kind of an instant it reads.
    /// </remarks>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Records.Event>(entity =>
        {
            entity.ToTable("events");
            entity.HasKey(storedEvent => storedEvent.Id);
            entity.HasIndex(storedEvent => storedEvent.PublicId).IsUnique();
            entity.HasIndex(storedEvent => new { storedEvent.StartDateTime, storedEvent.Id });
            entity.Property(storedEvent => storedEvent.PublicId).IsRequired();
            entity.Property(storedEvent => storedEvent.Name)
                .IsRequired()
                .HasMaxLength(Domain.Entities.Event.MaxNameLength);
            entity.Property(storedEvent => storedEvent.Description)
                .IsRequired(false)
                .HasMaxLength(Domain.Entities.Event.MaxDescriptionLength);
            entity.Property(storedEvent => storedEvent.StartDateTime).IsRequired();
            entity.Property(storedEvent => storedEvent.EndDateTime).IsRequired(false);
            entity.Property(storedEvent => storedEvent.RegistrationLimit).IsRequired();
            entity.HasOne(storedEvent => storedEvent.GameType)
                .WithMany()
                .HasForeignKey(storedEvent => storedEvent.GameTypeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Records.GameType>(entity =>
        {
            entity.ToTable("game_types");
            entity.HasKey(gameType => gameType.Id);
            entity.HasIndex(gameType => gameType.PublicId).IsUnique();
            entity.Property(gameType => gameType.PublicId).IsRequired();
            entity.Property(gameType => gameType.Name)
                .IsRequired()
                .HasMaxLength(Domain.Entities.GameType.MaxNameLength)
                .UseCollation(CaseInsensitiveCollation);
            entity.HasIndex(gameType => gameType.Name).IsUnique();
        });
    }

    /// <summary>
    /// Marks every instant read out of the database as UTC, leaving the clock reading itself untouched
    /// in both directions.
    /// </summary>
    /// <remarks>
    /// Only the clock reading survives a round trip through SQLite, so a value read back is UTC in
    /// substance but <see cref="DateTimeKind.Unspecified"/> in kind. Restoring the kind on the way out
    /// keeps the promise the domain entities make that their instants are always UTC, which they
    /// themselves enforce on every other path in. Nothing is converted on the way to the database,
    /// because a value arriving here is already UTC and reinterpreting it would shift it.
    /// </remarks>
    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        utcInstant => utcInstant,
        storedInstant => DateTime.SpecifyKind(storedInstant, DateTimeKind.Utc));
}
