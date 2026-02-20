---
paths:
  - "**/*.cs"
  - "**/Tests/**"
  - "**/*Tests.cs"
  - "**/*Test.cs"
---
# C# Testing

> Principles only. Full examples (WebApplicationFactory, TestContainers, Builders): `@skill: dotnet-testing`
> This file extends [common/testing.md](../common/testing.md) with C# specific content.

## Test Naming Convention

All test methods must follow: `MethodName_Scenario_ExpectedBehavior`

```csharp
[Fact]
public async Task CreateUser_WithValidData_ReturnsSuccess() { }

[Fact]
public async Task CreateUser_WithDuplicateEmail_ReturnsFailure() { }
```

## Tools

- **xUnit** — `[Fact]` for single tests, `[Theory]` + `[InlineData]` / `[MemberData]` for parameterised tests
- **FluentAssertions** — `result.Should().BeTrue()`, `users.Should().HaveCount(3)`, etc.
- **Moq** — mock interfaces; verify calls with `.Verify(..., Times.Once)`
- **WebApplicationFactory** — integration tests against the real ASP.NET Core pipeline with in-memory DB
- **TestContainers** — repository tests against a real SQL Server / Postgres container

## Requirements

- 80%+ code coverage (`dotnet test --collect:"XPlat Code Coverage"`)
- Unit tests for all business logic
- Integration tests for all API endpoints
- Test both success **and** failure paths; cover edge cases (null, empty, invalid)
- Async methods must be tested with `CancellationToken` handling

## Testing Checklist

- [ ] Unit tests for all business logic (80%+ coverage)
- [ ] Integration tests for API endpoints
- [ ] Database tests for repositories
- [ ] Test naming follows `MethodName_Scenario_ExpectedBehavior`
- [ ] FluentAssertions used for readable assertions
- [ ] Mocks created for external dependencies
- [ ] Both success and failure scenarios covered
- [ ] Edge cases tested (null, empty, invalid data)

