---
paths:
  - "**/*.cs"
  - "**/Tests/**"
  - "**/*Tests.cs"
  - "**/*Test.cs"
---
# C# Testing

> This file extends [common/testing.md](../common/testing.md) with C# specific content.

## xUnit Conventions

xUnit is the recommended testing framework for .NET projects.

### Basic Test Structure

```csharp
public class UserServiceTests
{
    [Fact]
    public async Task CreateUser_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var repository = new Mock<IUserRepository>();
        var service = new UserService(repository.Object);
        var dto = new CreateUserDto { Email = "test@example.com", Name = "Test User" };

        // Act
        var result = await service.CreateUserAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be("test@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("@@@")]
    public async Task CreateUser_WithInvalidEmail_ReturnsFailure(string email)
    {
        // Arrange
        var repository = new Mock<IUserRepository>();
        var service = new UserService(repository.Object);
        var dto = new CreateUserDto { Email = email, Name = "Test User" };

        // Act
        var result = await service.CreateUserAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("email");
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
public async Task CreateUser_WithDuplicateEmail_ReturnsFailure() { }
```

### Theory with Data Sources

```csharp
// InlineData - Simple values
[Theory]
[InlineData(1, 2, 3)]
[InlineData(5, 10, 15)]
public void Add_ValidNumbers_ReturnsSum(int a, int b, int expected)
{
    var result = Calculator.Add(a, b);
    result.Should().Be(expected);
}

// MemberData - Complex objects
public class UserServiceTests
{
    [Theory]
    [MemberData(nameof(GetValidUsers))]
    public async Task CreateUser_WithValidUser_ReturnsSuccess(CreateUserDto dto)
    {
        // Test implementation
    }

    public static IEnumerable<object[]> GetValidUsers()
    {
        yield return new object[] { new CreateUserDto { Email = "user1@test.com", Name = "User 1" } };
        yield return new object[] { new CreateUserDto { Email = "user2@test.com", Name = "User 2" } };
    }
}

// ClassData - Reusable test data
public class ValidUserData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { new CreateUserDto { Email = "user1@test.com", Name = "User 1" } };
        yield return new object[] { new CreateUserDto { Email = "user2@test.com", Name = "User 2" } };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

[Theory]
[ClassData(typeof(ValidUserData))]
public async Task CreateUser_WithValidUser_ReturnsSuccess(CreateUserDto dto) { }
```

## FluentAssertions

Use FluentAssertions for readable and powerful assertions:

```csharp
// Basic assertions
result.Should().NotBeNull();
result.Should().Be(expected);
result.Should().BeEquivalentTo(expected); // Deep comparison

// String assertions
name.Should().StartWith("John");
email.Should().Contain("@").And.EndWith(".com");

// Collection assertions
users.Should().HaveCount(3);
users.Should().Contain(u => u.Email == "test@example.com");
users.Should().OnlyContain(u => u.IsActive);
users.Should().BeInAscendingOrder(u => u.Name);

// Exception assertions
var act = async () => await service.CreateUserAsync(invalidDto);
await act.Should().ThrowAsync<ValidationException>()
    .WithMessage("*email*");

// Object assertions
user.Should().BeEquivalentTo(expected, options => options
    .Excluding(u => u.Id)
    .Excluding(u => u.CreatedAt));
```

## Moq for Mocking

```csharp
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _service = new UserService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetUser_WhenCalled_CallsRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = new User { Id = userId, Email = "test@example.com" };
        
        _mockRepository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _service.GetUserAsync(userId);

        // Assert
        result.Should().BeEquivalentTo(expectedUser);
        _mockRepository.Verify(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUser_WhenDuplicateEmail_DoesNotCallCreate()
    {
        // Arrange
        var dto = new CreateUserDto { Email = "existing@example.com" };
        _mockRepository
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User());

        // Act
        await _service.CreateUserAsync(dto);

        // Assert
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

## Integration Testing with WebApplicationFactory

```csharp
public class UserApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UserApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace DbContext with in-memory database
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task CreateUser_WithValidData_ReturnsCreated()
    {
        // Arrange
        var newUser = new { Email = "new@example.com", Name = "New User" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", newUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(newUser.Email);
    }
}
```

## Database Testing with TestContainers

```csharp
public class UserRepositoryTests : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    private AppDbContext _context = null!;
    private UserRepository _repository = null!;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .Build();

        await _container.StartAsync();

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
    public async Task CreateUser_SavesUserToDatabase()
    {
        // Arrange
        var user = new User { Email = "test@example.com", Name = "Test User" };

        // Act
        var result = await _repository.CreateAsync(user);

        // Assert
        result.Id.Should().NotBeEmpty();
        var savedUser = await _repository.GetByIdAsync(result.Id);
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be(user.Email);
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

    public User Build()
    {
        return new User
        {
            Id = _id,
            Email = _email,
            Name = _name,
            IsActive = _isActive
        };
    }
}

// Usage in tests
[Fact]
public async Task GetActiveUsers_OnlyReturnsActiveUsers()
{
    // Arrange
    var activeUser = new UserBuilder().WithEmail("active@test.com").Build();
    var inactiveUser = new UserBuilder().WithEmail("inactive@test.com").Inactive().Build();
    
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

Use coverlet for code coverage:

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html

# Enforce 80% threshold
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Threshold=80
```

## Testing Checklist

- [ ] Unit tests for all business logic (80%+ coverage)
- [ ] Integration tests for API endpoints
- [ ] Database tests for repositories
- [ ] Test naming follows `MethodName_Scenario_ExpectedBehavior`
- [ ] FluentAssertions used for readable assertions
- [ ] Mocks created for external dependencies
- [ ] Async methods tested with proper cancellation token handling
- [ ] Both success and failure scenarios covered
- [ ] Edge cases tested (null, empty, invalid data)
