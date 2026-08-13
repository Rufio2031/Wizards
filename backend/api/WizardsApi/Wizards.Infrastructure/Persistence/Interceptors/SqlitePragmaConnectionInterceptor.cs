using System.Data.Common;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Wizards.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Applies the SQLite pragmas the API depends on to every connection as it is opened.
/// </summary>
/// <remarks>
/// <para>
/// SQLite defaults to a rollback journal, which takes an exclusive lock over the whole database file
/// for the duration of a write and surfaces as <c>SQLITE_BUSY</c> on concurrent requests. Write-ahead
/// logging lets readers proceed alongside a single writer instead.
/// </para>
/// <para>
/// Microsoft.Data.Sqlite's pool caches the underlying <c>sqlite3</c> handle, so pragmas do survive a
/// pooled close and reopen. The pragmas are still reissued on every open because every newly created
/// handle starts at <c>busy_timeout=0</c>, and the pool creates handles at arbitrary times as demand
/// rises, so there is no single point at which applying them once would cover every connection.
/// Reapplying is idempotent and costs an in-process call.
/// </para>
/// <para>
/// Failures are logged rather than thrown: the connection is already open and usable by the time this
/// runs, so throwing would escape before Entity Framework finishes its bookkeeping and could leak a
/// connection that never returns to the pool.
/// </para>
/// </remarks>
/// <param name="logger">The logger used to report pragmas that could not be applied or verified.</param>
/// <param name="busyTimeout">
/// How long SQLite waits for a contended write lock before failing a statement with
/// <c>SQLITE_BUSY</c>. <see cref="TimeSpan.Zero"/> disables waiting, which makes the first contended
/// write fail immediately. Sub-millisecond values truncate to zero.
/// </param>
public sealed class SqlitePragmaConnectionInterceptor(
    ILogger<SqlitePragmaConnectionInterceptor> logger,
    TimeSpan busyTimeout) : DbConnectionInterceptor
{
    private const string WriteAheadLoggingMode = "wal";

    private readonly string busyTimeoutPragma = $"PRAGMA busy_timeout={(int)busyTimeout.TotalMilliseconds};";

    /// <inheritdoc />
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        try
        {
            this.ExecutePragma(connection, this.busyTimeoutPragma);

            object? journalMode = this.ExecutePragma(connection, $"PRAGMA journal_mode={WriteAheadLoggingMode};");

            this.VerifyWriteAheadLogging(journalMode, connection);
        }
        catch (DbException exception)
        {
            this.LogPragmaFailure(exception, connection);
        }

        base.ConnectionOpened(connection, eventData);
    }

    /// <inheritdoc />
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await this.ExecutePragmaAsync(connection, this.busyTimeoutPragma, cancellationToken);

            object? journalMode = await this.ExecutePragmaAsync(
                connection,
                $"PRAGMA journal_mode={WriteAheadLoggingMode};",
                cancellationToken);

            this.VerifyWriteAheadLogging(journalMode, connection);
        }
        catch (DbException exception)
        {
            this.LogPragmaFailure(exception, connection);
        }

        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private object? ExecutePragma(DbConnection connection, string pragmaSql)
    {
        using DbCommand command = connection.CreateCommand();
        command.CommandText = pragmaSql;

        return command.ExecuteScalar();
    }

    private async Task<object?> ExecutePragmaAsync(
        DbConnection connection,
        string pragmaSql,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = pragmaSql;

        return await command.ExecuteScalarAsync(cancellationToken);
    }

    private void VerifyWriteAheadLogging(object? journalMode, DbConnection connection)
    {
        string? appliedMode = journalMode as string;

        if (string.Equals(appliedMode, WriteAheadLoggingMode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        logger.LogWarning(
            "SQLite rejected write-ahead logging for database {Database} and is running in {JournalMode} mode instead. Concurrent writes may fail with SQLITE_BUSY.",
            connection.Database,
            appliedMode ?? "unknown");
    }

    private void LogPragmaFailure(DbException exception, DbConnection connection)
    {
        logger.LogError(
            exception,
            "Failed to apply SQLite pragmas to a connection for database {Database}. The connection remains usable but is not tuned for concurrent access.",
            connection.Database);
    }
}
