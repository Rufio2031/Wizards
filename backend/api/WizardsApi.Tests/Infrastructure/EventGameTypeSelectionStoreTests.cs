using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Wizards.Domain.Entities;
using Wizards.Domain.Enums;
using Wizards.Domain.Exceptions;
using Wizards.Domain.Interfaces.Repositories;
using Wizards.Infrastructure.Extensions;
using Wizards.Infrastructure.Persistence;

namespace WizardsApi.Tests.Infrastructure;

public sealed class EventGameTypeSelectionStoreTests : IAsyncLifetime
{
    private const int SqliteConstraintForeignKey = 787;

    private const int SqliteConstraintUnique = 2067;

    private readonly string connectionString =
        $"Data Source=file:{Guid.CreateVersion7():N}?mode=memory&cache=shared";

    private readonly SqliteConnection keepAliveConnection;

    private readonly ServiceProvider serviceProvider;

    public EventGameTypeSelectionStoreTests()
    {
        // The shared in-memory database lives exactly as long as a connection to it is open, so this
        // one is held for the whole class while the context opens and closes its own.
        this.keepAliveConnection = new SqliteConnection(this.connectionString);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Wizards"] = this.connectionString
            })
            .Build();

        this.serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddPersistence(configuration)
            .BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        await this.keepAliveConnection.OpenAsync();

        await this.serviceProvider.InitializeDatabaseAsync(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        await this.serviceProvider.DisposeAsync();
        await this.keepAliveConnection.DisposeAsync();
    }

    [Fact]
    public async Task GetEventByPublicId_EventWasSavedWithSelections_ReturnsThemCarryingTheSettingsTheyWereChosenFor()
    {
        GameType gameType = await this.ReadAGameTypeAsync();

        Event @event = CreateEvent(gameType, gameType.Validate(
            new Dictionary<string, string> { ["deckSize"] = "100" }));

        await this.SaveAsync(@event);

        Event? stored = await this.ReadEventAsync(@event.PublicId);

        Assert.NotNull(stored);
        Assert.Equal(
            gameType.Settings.Select(setting => setting.Id),
            stored.Selections.Select(selection => selection.GameTypeSetting.Id));
        Assert.Equal(
            gameType.Settings.Select(setting => setting.Key),
            stored.Selections.Select(selection => selection.GameTypeSetting.Key));
        Assert.Equal(
            "100",
            stored.Selections.Single(selection => selection.GameTypeSetting.Key == "deckSize").Value);
    }

    [Fact]
    public async Task SaveChanges_EventCarriesTwoValuesForOneSetting_ViolatesTheUniqueIndex()
    {
        GameType gameType = await this.ReadAGameTypeAsync();
        GameTypeSetting deckSize = SettingNamed(gameType, "deckSize");

        Event @event = Event.Reconstitute(
            0,
            Guid.CreateVersion7(),
            "Friday Night Magic",
            null,
            "The Back Room",
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7).AddHours(3),
            gameType,
            8,
            [
                EventGameTypeSelection.Create(deckSize, "60"),
                EventGameTypeSelection.Create(deckSize, "100")
            ]);

        StoreUniquenessViolationException exception =
            await Assert.ThrowsAsync<StoreUniquenessViolationException>(() => this.SaveAsync(@event));

        Assert.Equal(
            SqliteConstraintUnique,
            Assert.IsType<SqliteException>(exception.InnerException?.InnerException).SqliteExtendedErrorCode);
    }

    [Fact]
    public async Task SaveChanges_SelectionNamesNoStoredSetting_ViolatesTheForeignKey()
    {
        GameType gameType = await this.ReadAGameTypeAsync();

        GameTypeSetting unstoredSetting = GameTypeSetting.Reconstitute(
            999_999,
            "ghost",
            "Ghost",
            null,
            SettingType.Int,
            "1",
            null,
            null);

        Event @event = CreateEvent(
            gameType,
            [EventGameTypeSelection.Create(unstoredSetting, "1")]);

        DbUpdateException exception =
            await Assert.ThrowsAsync<DbUpdateException>(() => this.SaveAsync(@event));

        Assert.Equal(
            SqliteConstraintForeignKey,
            Assert.IsType<SqliteException>(exception.InnerException).SqliteExtendedErrorCode);
    }

    [Fact]
    public async Task DeleteSetting_ASelectionReferencesIt_IsRefusedAndLeavesTheSelectionStored()
    {
        GameType gameType = await this.ReadAGameTypeAsync();

        Event @event = CreateEvent(gameType, gameType.Validate(null));

        await this.SaveAsync(@event);

        await using AsyncServiceScope scope = this.serviceProvider.CreateAsyncScope();

        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        int settingId = SettingNamed(gameType, "deckSize").Id;

        SqliteException exception = await Assert.ThrowsAsync<SqliteException>(
            () => dbContext.Database.ExecuteSqlAsync(
                $"DELETE FROM game_type_settings WHERE Id = {settingId}"));

        Assert.Contains("FOREIGN KEY constraint failed", exception.Message, StringComparison.Ordinal);

        Event? stored = await this.ReadEventAsync(@event.PublicId);

        Assert.NotNull(stored);
        Assert.Contains(stored.Selections, selection => selection.GameTypeSetting.Id == settingId);
    }

    private static Event CreateEvent(GameType gameType, IEnumerable<EventGameTypeSelection> selections)
    {
        DateTime start = DateTime.UtcNow.AddDays(7);

        return Event.Create(
            "Friday Night Magic",
            null,
            "The Back Room",
            gameType,
            start,
            start.AddHours(3),
            8,
            selections);
    }

    private static GameTypeSetting SettingNamed(GameType gameType, string key) =>
        gameType.Settings.Single(setting => setting.Key == key);

    private async Task<GameType> ReadAGameTypeAsync()
    {
        await using AsyncServiceScope scope = this.serviceProvider.CreateAsyncScope();

        IReadOnlyList<GameType> gameTypes = await scope.ServiceProvider
            .GetRequiredService<IGameTypesRepository>()
            .GetGameTypesAsync(CancellationToken.None);

        return gameTypes.First(gameType => gameType.Settings.Any(setting => setting.Key == "deckSize"));
    }

    private async Task SaveAsync(Event @event)
    {
        await using AsyncServiceScope scope = this.serviceProvider.CreateAsyncScope();

        await scope.ServiceProvider
            .GetRequiredService<IEventsRepository>()
            .AddEventAsync(@event, CancellationToken.None);

        await scope.ServiceProvider
            .GetRequiredService<IUnitOfWork>()
            .SaveChangesAsync(CancellationToken.None);
    }

    private async Task<Event?> ReadEventAsync(Guid publicId)
    {
        await using AsyncServiceScope scope = this.serviceProvider.CreateAsyncScope();

        return await scope.ServiceProvider
            .GetRequiredService<IEventsRepository>()
            .GetEventByPublicIdAsync(publicId, CancellationToken.None);
    }
}
