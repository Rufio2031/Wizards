using Microsoft.EntityFrameworkCore;

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
}
