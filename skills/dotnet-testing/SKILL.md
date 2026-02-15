---
name: dotnet-testing
description: Comprehensive .NET testing patterns with xUnit, FluentAssertions, Moq, integration tests with WebApplicationFactory, and TestContainers for database testing.
---

# .NET Testing Patterns

Test-driven development patterns and best practices for .NET applications.

## When to Activate

- Writing new features or functionality
- Fixing bugs or issues  
- Refactoring existing code
- Creating API endpoints
- Implementing business logic
- Testing database operations

## Core Testing Principles

### 1. Tests BEFORE Code (TDD)
ALWAYS write tests first, then implement code to make tests pass.

### 2. Coverage Requirements
- Minimum 80% coverage (unit + integration tests)
- All edge cases covered
- Error scenarios tested
- Boundary conditions verified

### 3. Test Pyramid
- **70%** Unit tests (fast, isolated)
- **20%** Integration tests (API, database)
- **10%** E2E tests (critical flows)

## xUnit Test Patterns

### Basic Test Structure (AAA Pattern)

```csharp
public class UserServiceTests
{
    [Fact]
    public async Task CreateUser_WithValidData_ReturnsSuccess()
    {
        // Arrange - Set up test data and mocks
        var mockRepository = new Mock<IUserRepository>();
        var mockLogger = new Mock<ILogger<UserService>>();
        var service = new UserService(mockRepository.Object, mockLogger.Object);
        
        var dto = new CreateUserDto
        {
            Email = "test@example.com",
            Name = "Test User"
        };

        mockRepository
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);  // Email doesn't exist

        // Act - Execute the method under test
        var result = await service.CreateUserAsync(dto);

        // Assert - Verify the outcome
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(dto.Email);
        result.Value.Name.Should().Be(dto.Name);
        
        // Verify repository was called
        mockRepository.Verify(
            r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var mockRepository = new Mock<IUserRepository>();
        var mockLogger = new Mock<ILogger<UserService>>();
        var service = new UserService(mockRepository.Object, mockLogger.Object);
        
        var existingUser = new User { Email = "existing@example.com" };
        var dto = new CreateUserDto { Email = "existing@example.com", Name = "Test" };

        mockRepository
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);  // Email already exists

        // Act
        var result = await service.CreateUserAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
        
        // Verify Create was never called
        mockRepository.Verify(
            r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }
}
```

### Test Naming Convention

Use: `MethodName_Scenario_ExpectedBehavior`

```csharp
[Fact]
public async Task GetUser_WhenUserExists_ReturnsUser() { }

[Fact]
public async Task GetUser_WhenUserNotFound_ReturnsNull() { }

[Fact]
public async Task CreateUser_WithInvalidEmail_ReturnsValidationError() { }

[Fact]
public async Task DeleteUser_WhenUserHasOrders_ThrowsException() { }
```

### Theories for Parameterized Tests

```csharp
// Simple values with InlineData
[Theory]
[InlineData("")]
[InlineData("invalid-email")]
[InlineData("@@@")]
[InlineData("test@")]
public async Task CreateUser_WithInvalidEmail_ReturnsFailure(string email)
{
    // Arrange
    var service = CreateService();
    var dto = new CreateUserDto { Email = email, Name = "Test" };

    // Act
    var result = await service.CreateUserAsync(dto);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Error.Should().Contain("email");
}

// Multiple parameters
[Theory]
[InlineData(0, 100, 100)]
[InlineData(50, 75, 125)]
[InlineData(100, 100, 200)]
public void CalculateTax_WithVariousAmounts_ReturnsCorrectTotal(
    decimal baseAmount, 
    decimal taxAmount, 
    decimal expectedTotal)
{
    var result = TaxCalculator.CalculateTotal(baseAmount, taxAmount);
    result.Should().Be(expectedTotal);
}

// Complex data with MemberData
[Theory]
[MemberData(nameof(GetValidUsers))]
public async Task CreateUser_WithValidUser_ReturnsSuccess(CreateUserDto dto)
{
    var service = CreateService();
    var result = await service.CreateUserAsync(dto);
    result.IsSuccess.Should().BeTrue();
}

public static IEnumerable<object[]> GetValidUsers()
{
    yield return new object[] 
    { 
        new CreateUserDto { Email = "user1@test.com", Name = "User 1" } 
    };
    yield return new object[] 
    { 
        new CreateUserDto { Email = "user2@test.com", Name = "User 2" } 
    };
}

// Reusable test data with ClassData
public class ValidUserData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] 
        { 
            new CreateUserDto { Email = "user1@test.com", Name = "User 1" } 
        };
        yield return new object[] 
        { 
            new CreateUserDto { Email = "user2@test.com", Name = "User 2" } 
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

[Theory]
[ClassData(typeof(ValidUserData))]
public async Task CreateUser_WithValidUser_ReturnsSuccess(CreateUserDto dto) { }
```

### Test Fixtures for Shared Setup

```csharp
// Fixture class - created once per test class
public class DatabaseFixture : IDisposable
{
    public AppDbContext Context { get; private set; }

    public DatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
        
        // Seed test data
        SeedData();
    }

    private void SeedData()
    {
        Context.Users.AddRange(
            new User { Id = Guid.NewGuid(), Email = "user1@test.com", Name = "User 1" },
            new User { Id = Guid.NewGuid(), Email = "user2@test.com", Name = "User 2" }
        );
        Context.SaveChanges();
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}

// Use fixture in test class
public class UserRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public UserRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAllUsers_ReturnsSeededUsers()
    {
        var repository = new UserRepository(_fixture.Context);
        var users = await repository.GetAllAsync();
        users.Should().HaveCount(2);
    }
}
```

## FluentAssertions Patterns

```csharp
// Basic assertions
result.Should().NotBeNull();
result.Should().Be(expected);
result.Should().BeEquivalentTo(expected);  // Deep comparison

// Numeric assertions
count.Should().BeGreaterThan(0);
price.Should().BeInRange(10.0m, 100.0m);
percentage.Should().BeApproximately(75.5, precision: 0.1);

// String assertions
name.Should().StartWith("John");
email.Should().Contain("@").And.EndWith(".com");
message.Should().Match("Error: *");
text.Should().NotBeNullOrWhiteSpace();

// Collection assertions
users.Should().HaveCount(3);
users.Should().Contain(u => u.Email == "test@example.com");
users.Should().OnlyContain(u => u.IsActive);
users.Should().BeInAscendingOrder(u => u.Name);
users.Should().NotContainNulls();
users.Should().BeEquivalentTo(expectedUsers, options => options
    .Excluding(u => u.Id)
    .Excluding(u => u.CreatedAt));

// Exception assertions
Func<Task> act = async () => await service.CreateUserAsync(invalidDto);
await act.Should().ThrowAsync<ValidationException>()
    .WithMessage("*email*");

await act.Should().ThrowExactlyAsync<ArgumentNullException>()
    .Where(ex => ex.ParamName == "dto");

// DateTime assertions
user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(1));
user.CreatedAt.Should().BeAfter(yesterday);

// Boolean assertions
result.IsSuccess.Should().BeTrue();
user.IsActive.Should().BeFalse("because user was deactivated");
```

## Moq Mocking Patterns

```csharp
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _service = new UserService(
            _mockRepository.Object, 
            _mockEmailService.Object, 
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateUser_CallsRepositoryAndSendsEmail()
    {
        // Setup - Return value
        _mockRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken ct) => u);  // Return the input

        // Act
        var dto = new CreateUserDto { Email = "new@test.com", Name = "New User" };
        var result = await _service.CreateUserAsync(dto);

        // Verify - Method was called with specific arguments
        _mockRepository.Verify(
            r => r.CreateAsync(
                It.Is<User>(u => u.Email == dto.Email), 
                It.IsAny<CancellationToken>()), 
            Times.Once);

        _mockEmailService.Verify(
            e => e.SendWelcomeEmailAsync(dto.Email, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task CreateUser_WhenEmailServiceFails_StillReturnsSuccess()
    {
        // Setup - Throw exception
        _mockEmailService
            .Setup(e => e.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP error"));

        var dto = new CreateUserDto { Email = "new@test.com", Name = "New User" };
        var result = await _service.CreateUserAsync(dto);

        // Email failure shouldn't prevent user creation
        result.IsSuccess.Should().BeTrue();
        
        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
```

## Integration Testing with WebApplicationFactory

```csharp
public class UserApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public UserApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove real DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add InMemory database for testing
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase");
                });

                // Seed test data
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Database.EnsureCreated();
                SeedTestData(context);
            });
        });

        _client = _factory.CreateClient();
    }

    private void SeedTestData(AppDbContext context)
    {
        context.Users.Add(new User 
        { 
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = "existing@test.com", 
            Name = "Existing User" 
        });
        context.SaveChanges();
    }

    [Fact]
    public async Task GetUsers_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetUser_WhenExists_ReturnsUser()
    {
        // Act
        var response = await _client.GetAsync("/api/users/11111111-1111-1111-1111-111111111111");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be("existing@test.com");
    }

    [Fact]
    public async Task GetUser_WhenNotExists_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/users/22222222-2222-2222-2222-222222222222");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateUser_WithValidData_ReturnsCreated()
    {
        // Arrange
        var newUser = new CreateUserDto
        {
            Email = "new@example.com",
            Name = "New User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", newUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(newUser.Email);
        user.Name.Should().Be(newUser.Name);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var duplicateUser = new CreateUserDto
        {
            Email = "existing@test.com",  // Already exists in seed data
            Name = "Duplicate"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", duplicateUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

## Database Testing with TestContainers

```csharp
public class UserRepositoryIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    private AppDbContext _context = null!;
    private UserRepository _repository = null!;

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .Build();

        await _container.StartAsync();

        // Configure DbContext with container connection string
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _repository = new UserRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task CreateUser_SavesUserToRealDatabase()
    {
        // Arrange
        var user = new User 
        { 
            Id = Guid.NewGuid(),
            Email = "test@example.com", 
            Name = "Test User",
            IsActive = true
        };

        // Act
        var result = await _repository.CreateAsync(user);

        // Assert
        result.Id.Should().NotBeEmpty();
        
        var savedUser = await _repository.GetByIdAsync(result.Id);
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be(user.Email);
        savedUser.Name.Should().Be(user.Name);
    }

    [Fact]
    public async Task GetActiveUsers_OnlyReturnsActiveUsers()
    {
        // Arrange - Seed data
        await _repository.CreateAsync(new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "active@test.com", 
            Name = "Active", 
            IsActive = true 
        });
        await _repository.CreateAsync(new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "inactive@test.com", 
            Name = "Inactive", 
            IsActive = false 
        });

        // Act
        var users = await _repository.GetActiveUsersAsync();

        // Assert
        users.Should().ContainSingle()
            .Which.Email.Should().Be("active@test.com");
    }
}
```

## Test Data Builders

```csharp
public class UserBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _email = "default@example.com";
    private string _name = "Default Name";
    private bool _isActive = true;
    private DateTime _createdAt = DateTime.UtcNow;

    public UserBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UserBuilder Inactive()
    {
        _isActive = false;
        return this;
    }

    public UserBuilder CreatedOn(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public User Build()
    {
        return new User
        {
            Id = _id,
            Email = _email,
            Name = _name,
            IsActive = _isActive,
            CreatedAt = _createdAt
        };
    }
}

// Usage in tests
[Fact]
public async Task GetActiveUsers_OnlyReturnsActiveUsers()
{
    // Arrange - Readable test data construction
    var activeUser = new UserBuilder()
        .WithEmail("active@test.com")
        .WithName("Active User")
        .Build();
    
    var inactiveUser = new UserBuilder()
        .WithEmail("inactive@test.com")
        .WithName("Inactive User")
        .Inactive()
        .Build();
    
    await _repository.CreateAsync(activeUser);
    await _repository.CreateAsync(inactiveUser);

    // Act
    var users = await _repository.GetActiveUsersAsync();

    // Assert
    users.Should().ContainSingle()
        .Which.Email.Should().Be("active@test.com");
}
```

## Code Coverage

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:Html

# Open report
open coveragereport/index.html

# Enforce 80% threshold (fail build if below)
dotnet test /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line
```

## Testing Checklist

- [ ] Minimum 80% code coverage
- [ ] Tests follow AAA pattern (Arrange, Act, Assert)
- [ ] Test names follow `MethodName_Scenario_ExpectedBehavior`
- [ ] FluentAssertions used for readable assertions
- [ ] Mocks created for external dependencies
- [ ] Integration tests for all API endpoints
- [ ] Database tests use TestContainers or InMemory
- [ ] Both success and failure scenarios covered
- [ ] Edge cases tested (null, empty, invalid data)
- [ ] All async methods tested with CancellationToken

## Related Resources

- See [rules/csharp/testing.md](../../rules/csharp/testing.md) for testing requirements
- See [skills/tdd-workflow](../tdd-workflow/SKILL.md) for TDD process
- Use [agents/tdd-guide.md](../../agents/tdd-guide.md) for test-first development
