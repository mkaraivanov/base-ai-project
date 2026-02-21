using System.Security.Claims;
using System.Text.Json;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace Tests.Unit.Infrastructure;

/// <summary>
/// Integration-style tests for <see cref="AuditInterceptor"/> using an EF Core
/// in-memory database.  They verify that the interceptor persists the correct
/// <see cref="AuditLog"/> rows for Created / Updated / Deleted entity operations
/// and that sensitive properties and <see cref="AuditLog"/> itself are excluded.
/// </summary>
public class AuditInterceptorTests : IDisposable
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private CinemaDbContext _db;

    public AuditInterceptorTests()
    {
        _db = BuildContext(user: null);
    }

    public void Dispose() => _db.Dispose();

    // ── helpers ───────────────────────────────────────────────────────────────

    private CinemaDbContext BuildContext(ClaimsPrincipal? user, IAuditCaptureService? auditCapture = null)
    {
        if (user is not null)
        {
            var httpContext = new DefaultHttpContext { User = user };
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        }
        else
        {
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        }

        var interceptor = new AuditInterceptor(
            _httpContextAccessorMock.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditInterceptor>.Instance,
            auditCapture);

        var options = new DbContextOptionsBuilder<CinemaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique DB per test
            .AddInterceptors(interceptor)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new CinemaDbContext(options);
    }

    private static Movie MakeMovie(string title = "Test Movie") => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Description = "A description",
        Genre = "Drama",
        DurationMinutes = 120,
        Rating = "PG",
        PosterUrl = "https://example.com/poster.jpg",
        ReleaseDate = new DateOnly(2026, 1, 1),
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private static User MakeUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@test.com",
        PasswordHash = "super-secret-hash",
        FirstName = "Joe",
        LastName = "Smith",
        PhoneNumber = "0888000000",
        Role = UserRole.Customer,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    // ── Created ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SavingChanges_WhenEntityAdded_CreatesAuditLogWithCreatedAction()
    {
        // Act
        _db.Movies.Add(MakeMovie());
        await _db.SaveChangesAsync();

        // Assert
        var auditLogs = await _db.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);
        auditLogs[0].Action.Should().Be("Created");
        auditLogs[0].EntityName.Should().Be(nameof(Movie));
        auditLogs[0].NewValues.Should().NotBeNullOrEmpty();
        auditLogs[0].OldValues.Should().BeNull();
    }

    [Fact]
    public async Task SavingChanges_WhenEntityAdded_NewValuesContainsEntityProperties()
    {
        // Act
        _db.Movies.Add(MakeMovie("Avatar"));
        await _db.SaveChangesAsync();

        // Assert
        var auditLog = await _db.AuditLogs.SingleAsync();
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditLog.NewValues!);
        dict.Should().ContainKey("Title");
        dict!["Title"].GetString().Should().Be("Avatar");
    }

    [Fact]
    public async Task SavingChanges_WhenEntityAdded_EntityIdIsRecorded()
    {
        // Arrange
        var movie = MakeMovie();

        // Act
        _db.Movies.Add(movie);
        await _db.SaveChangesAsync();

        // Assert
        var auditLog = await _db.AuditLogs.SingleAsync();
        auditLog.EntityId.Should().Be(movie.Id.ToString());
    }

    // ── Updated ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SavingChanges_WhenEntityUpdated_CreatesAuditLogWithUpdatedAction()
    {
        // Arrange – seed entity
        var movie = MakeMovie("Original Title");
        _db.Movies.Add(movie);
        await _db.SaveChangesAsync();

        // Act – mutate through EF tracking (init-only properties require this approach)
        var movieEntry = _db.Entry(movie);
        movieEntry.Property("Title").CurrentValue = "Updated Title";
        await _db.SaveChangesAsync();

        // Assert – exactly one "Updated" log (one "Created" from seed)
        var updatedLogs = await _db.AuditLogs
            .Where(a => a.Action == "Updated")
            .ToListAsync();
        updatedLogs.Should().HaveCount(1);
        updatedLogs[0].EntityName.Should().Be(nameof(Movie));
    }

    [Fact]
    public async Task SavingChanges_WhenEntityUpdated_OldValuesAndNewValuesCaptureChange()
    {
        // Arrange
        var movie = MakeMovie("Original Title");
        _db.Movies.Add(movie);
        await _db.SaveChangesAsync();

        // Act
        _db.Entry(movie).Property("Title").CurrentValue = "Updated Title";
        await _db.SaveChangesAsync();

        // Assert
        var log = await _db.AuditLogs.SingleAsync(a => a.Action == "Updated");
        log.OldValues.Should().Contain("Original Title");
        log.NewValues.Should().Contain("Updated Title");
    }

    // ── Deleted ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SavingChanges_WhenEntityDeleted_CreatesAuditLogWithDeletedAction()
    {
        // Arrange
        var movie = MakeMovie();
        _db.Movies.Add(movie);
        await _db.SaveChangesAsync();

        // Act
        _db.Movies.Remove(movie);
        await _db.SaveChangesAsync();

        // Assert
        var deletedLogs = await _db.AuditLogs
            .Where(a => a.Action == "Deleted")
            .ToListAsync();
        deletedLogs.Should().HaveCount(1);
        deletedLogs[0].OldValues.Should().NotBeNullOrEmpty();
        deletedLogs[0].NewValues.Should().BeNull();
    }

    [Fact]
    public async Task SavingChanges_WhenEntityDeleted_OldValuesContainEntityProperties()
    {
        // Arrange
        var movie = MakeMovie("DeleteMe");
        _db.Movies.Add(movie);
        await _db.SaveChangesAsync();

        // Act
        _db.Movies.Remove(movie);
        await _db.SaveChangesAsync();

        // Assert
        var log = await _db.AuditLogs.SingleAsync(a => a.Action == "Deleted");
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(log.OldValues!);
        dict.Should().ContainKey("Title");
        dict!["Title"].GetString().Should().Be("DeleteMe");
    }

    // ── No recursion ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SavingChanges_WhenAuditLogIsAdded_DoesNotCreateRecursiveAuditLog()
    {
        // Act – adding an entity triggers the interceptor which adds an AuditLog internally
        _db.Movies.Add(MakeMovie());
        await _db.SaveChangesAsync();

        // Assert – only one AuditLog created for the Movie; no log-about-log
        var auditLogs = await _db.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);
        auditLogs.Should().NotContain(a => a.EntityName == nameof(AuditLog));
    }

    // ── Sensitive properties ──────────────────────────────────────────────────

    [Fact]
    public async Task SavingChanges_SensitiveProperties_AreExcludedFromNewValues()
    {
        // Act
        _db.Users.Add(MakeUser());
        await _db.SaveChangesAsync();

        // Assert – PasswordHash must not appear in captured values
        var auditLog = await _db.AuditLogs.SingleAsync(a => a.EntityName == nameof(User));
        auditLog.NewValues.Should().NotBeNullOrEmpty();
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditLog.NewValues!);
        dict.Should().NotContainKey("PasswordHash");
        dict.Should().ContainKey("Email");
    }

    // ── User identity captured ────────────────────────────────────────────────

    [Fact]
    public async Task SavingChanges_WhenUserAuthenticated_CapturesUserDetailsFromHttpContext()
    {
        // Arrange – rebuild context with an authenticated user in HTTP context
        var userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "admin@cinema.com"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        await _db.DisposeAsync();
        _db = BuildContext(principal);

        // Act
        _db.Movies.Add(MakeMovie());
        await _db.SaveChangesAsync();

        // Assert
        var auditLog = await _db.AuditLogs.SingleAsync();
        auditLog.UserId.Should().Be(userId);
        auditLog.UserEmail.Should().Be("admin@cinema.com");
        auditLog.UserRole.Should().Be("Admin");
    }

    [Fact]
    public async Task SavingChanges_WhenNoHttpContext_UserFieldsAreNull()
    {
        // The default context built in the constructor has null HttpContext

        // Act
        _db.Movies.Add(MakeMovie());
        await _db.SaveChangesAsync();

        // Assert
        var auditLog = await _db.AuditLogs.SingleAsync();
        auditLog.UserId.Should().BeNull();
        auditLog.UserEmail.Should().BeNull();
        auditLog.UserRole.Should().BeNull();
    }

    /// <summary>
    /// Regression test: the primary production flow uses IAuditCaptureService to
    /// register old values BEFORE CurrentValues.SetValues() is called.  This ensures
    /// correct old/new diffs even when GetDatabaseValuesAsync() would return stale data
    /// (which can happen with SQL Server and EF Core 9 record entities).
    /// </summary>
    [Fact]
    public async Task Update_WithPreCapture_AuditContainsCorrectOldAndNewValues()
    {
        // Arrange – build a context that has an IAuditCaptureService wired in
        var captureService = new AuditCaptureService();
        await _db.DisposeAsync();
        _db = BuildContext(user: null, auditCapture: captureService);

        var movie = MakeMovie("Before Title");
        _db.Movies.Add(movie);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        // Act – mirror what MovieRepository.UpdateAsync does
        var existing = await _db.Movies.FindAsync([movie.Id]);
        var entry = _db.Entry(existing!);

        // Capture OLD values exactly as the repository does, before SetValues
        var oldValues = entry.Properties
            .Where(p => !p.Metadata.IsShadowProperty())
            .ToDictionary(p => p.Metadata.Name, p => (object?)p.CurrentValue);
        captureService.RegisterPreUpdateValues(typeof(Movie), movie.Id.ToString(), oldValues);

        var updated = existing! with { Title = "After Title" };
        entry.CurrentValues.SetValues(updated);
        await _db.SaveChangesAsync();

        // Assert
        var log = await _db.AuditLogs.SingleAsync(a => a.Action == "Updated");
        var oldDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(log.OldValues!);
        var newDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(log.NewValues!);

        oldDict.Should().ContainKey("Title");
        oldDict!["Title"].GetString().Should().Be("Before Title",
            "old-values must reflect the title that was in the DB before the update");

        newDict.Should().ContainKey("Title");
        newDict!["Title"].GetString().Should().Be("After Title",
            "new-values must reflect the updated title");

        // Only the changed property should appear in the diff
        oldDict.Should().NotContainKey("Genre");
        newDict.Should().NotContainKey("Genre");
    }


    /// Before the fix, repositories called Update(detachedEntity) which caused EF Core
    /// to set OriginalValues = CurrentValues = new values, making both old and new identical.
    /// With the fix (FindAsync + CurrentValues.SetValues), only changed properties are marked
    /// Modified and the original-value snapshot is preserved.
    /// </summary>
    [Fact]
    public async Task Update_UsingCurrentValuesSetValues_AuditCapturesCorrectDiff()
    {
        // Arrange – seed entity and detach so the context is clean (simulates service layer)
        var movie = MakeMovie("Original Title");
        _db.Movies.Add(movie);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        // Act – simulate the repository UpdateAsync pattern: FindAsync then SetValues
        var existing = await _db.Movies.FindAsync([movie.Id]);
        var updated = existing! with { Title = "New Title" };
        _db.Entry(existing).CurrentValues.SetValues(updated);
        await _db.SaveChangesAsync();

        // Assert – old contains "Original Title", new contains "New Title"
        var log = await _db.AuditLogs.SingleAsync(a => a.Action == "Updated");
        var oldDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(log.OldValues!);
        var newDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(log.NewValues!);

        oldDict.Should().ContainKey("Title");
        oldDict!["Title"].GetString().Should().Be("Original Title");
        newDict.Should().ContainKey("Title");
        newDict!["Title"].GetString().Should().Be("New Title");

        // Only the changed property should appear — not the full entity snapshot
        oldDict.Should().NotContainKey("Genre");
        newDict.Should().NotContainKey("Genre");
    }

    /// <summary>
    /// Regression test for the soft-delete audit bug.
    /// Before the fix, DeleteAsync used "existing with { IsActive = false } + Update()"
    /// which caused EF Core to lose the original-value snapshot so both OldValues and
    /// NewValues showed IsActive = false.  With the fix (property API on tracked entity),
    /// the audit log correctly shows IsActive: true → false.
    /// </summary>
    [Fact]
    public async Task SoftDelete_UsingPropertyApi_AuditShowsIsActiveChangeFromTrueToFalse()
    {
        // Arrange – seed an active movie
        var movie = MakeMovie();
        movie = movie with { IsActive = true };
        _db.Movies.Add(movie);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        // Act – simulate the fixed repository DeleteAsync pattern: FindAsync + property API
        var existing = await _db.Movies.FindAsync([movie.Id]);
        _db.Entry(existing!).Property(m => m.IsActive).CurrentValue = false;
        await _db.SaveChangesAsync();

        // Assert – audit shows only IsActive in diff, with correct before/after values
        var log = await _db.AuditLogs.SingleAsync(a => a.Action == "Updated");
        var oldDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(log.OldValues!);
        var newDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(log.NewValues!);

        oldDict.Should().ContainKey("IsActive");
        oldDict!["IsActive"].GetBoolean().Should().BeTrue("old value must be the active state before deactivation");
        newDict.Should().ContainKey("IsActive");
        newDict!["IsActive"].GetBoolean().Should().BeFalse("new value must reflect deactivation");

        // Only IsActive changed — other fields must not appear in the diff
        oldDict.Should().NotContainKey("Title");
        newDict.Should().NotContainKey("Title");
    }
}
