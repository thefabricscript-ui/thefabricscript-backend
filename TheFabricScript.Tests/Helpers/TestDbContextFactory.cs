using Microsoft.EntityFrameworkCore;
using TheFabricScript.Infrastructure.Data;

namespace TheFabricScript.Tests.Helpers;

/// <summary>
/// Creates isolated EF Core InMemory <see cref="AppDbContext"/> instances for unit tests.
/// Each test that calls <see cref="Create"/> gets its own database keyed by a unique GUID,
/// so tests do not share state and can run in parallel safely.
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates and returns a new <see cref="AppDbContext"/> backed by an in-memory database.
    /// </summary>
    /// <remarks>
    /// <b>Important:</b> The in-memory provider does not enforce relational constraints,
    /// foreign keys, or SQL-level uniqueness. Tests are validating application-layer logic,
    /// not database integrity. Use integration tests with a real SQL Server container
    /// (see <c>.github/workflows/ci.yml</c>) for constraint verification.
    /// </remarks>
    /// <param name="dbName">
    /// Optional database name. If omitted, a random GUID is used to guarantee isolation.
    /// Pass a fixed name when multiple test methods need to share the same seeded dataset.
    /// </param>
    public static AppDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
