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

    /// <summary>The stored settings the game types expose.</summary>
    internal DbSet<Records.GameTypeSetting> GameTypeSettings => this.Set<Records.GameTypeSetting>();

    /// <summary>The stored settings the organizers settled for their events.</summary>
    internal DbSet<Records.EventGameTypeSelection> EventGameTypeSelections =>
        this.Set<Records.EventGameTypeSelection>();

    /// <summary>The stored options the choice settings allow.</summary>
    internal DbSet<Records.GameTypeSettingOption> GameTypeSettingOptions =>
        this.Set<Records.GameTypeSettingOption>();

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

        modelBuilder.Entity<Records.EventGameTypeSelection>(entity =>
        {
            entity.ToTable("event_game_type_selections");
            entity.HasKey(selection => selection.Id);
            entity.Property(selection => selection.Key)
                .IsRequired()
                .HasMaxLength(Domain.Entities.GameTypeSetting.MaxKeyLength)
                .UseCollation(CaseInsensitiveCollation);
            entity.Property(selection => selection.Value)
                .IsRequired()
                .HasMaxLength(Domain.Entities.GameTypeSetting.MaxValueLength);
            entity.HasIndex(selection => new { selection.EventId, selection.Key }).IsUnique();
            entity.HasOne(selection => selection.Event)
                .WithMany(storedEvent => storedEvent.Selections)
                .HasForeignKey(selection => selection.EventId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<Records.GameTypeSetting>(entity =>
        {
            entity.ToTable("game_type_settings");
            entity.HasKey(setting => setting.Id);
            entity.Property(setting => setting.Key)
                .IsRequired()
                .HasMaxLength(Domain.Entities.GameTypeSetting.MaxKeyLength)
                .UseCollation(CaseInsensitiveCollation);
            entity.Property(setting => setting.Label)
                .IsRequired()
                .HasMaxLength(Domain.Entities.GameTypeSetting.MaxLabelLength);
            entity.Property(setting => setting.Description)
                .IsRequired(false)
                .HasMaxLength(Domain.Entities.GameTypeSetting.MaxDescriptionLength);
            entity.Property(setting => setting.Type).IsRequired();
            entity.Property(setting => setting.MinValue).IsRequired(false);
            entity.Property(setting => setting.MaxValue).IsRequired(false);
            entity.Property(setting => setting.DefaultValue)
                .IsRequired()
                .HasMaxLength(Domain.Entities.GameTypeSetting.MaxValueLength);

            entity.HasIndex(setting => new { setting.GameTypeId, setting.Key }).IsUnique();

            entity.HasOne(setting => setting.GameType)
                .WithMany(gameType => gameType.Settings)
                .HasForeignKey(setting => setting.GameTypeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Records.GameTypeSettingOption>(entity =>
        {
            entity.ToTable("game_type_setting_options");
            entity.HasKey(option => option.Id);
            entity.Property(option => option.Value)
                .IsRequired()
                .HasMaxLength(Domain.Entities.GameTypeSetting.MaxValueLength)
                .UseCollation(CaseInsensitiveCollation);
            entity.HasIndex(option => new { option.GameTypeSettingId, option.Value }).IsUnique();
            entity.HasOne(option => option.Setting)
                .WithMany(setting => setting.Options)
                .HasForeignKey(option => option.GameTypeSettingId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
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
