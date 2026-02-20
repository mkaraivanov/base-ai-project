# GitHub Copilot Instructions

This project uses an ASP.NET Core (C#) backend and a TypeScript/React (Vite) frontend.
Apply the rules below for all code suggestions and completions.

---

## CORS – Post-Rebuild Requirement

**After every backend rebuild, verify CORS is correctly configured.**

- Confirm CORS middleware is registered and ordered before routing/auth middleware.
- Allowed origins, methods, and headers must match the current environment (dev vs. prod).
- Use environment variables — never hardcode origins.
- Test all API endpoints from the frontend after each rebuild to confirm CORS headers are present.

```csharp
// Program.cs — must appear before app.UseRouting() / app.UseAuthentication()
app.UseCors(policy =>
    policy.WithOrigins(builder.Configuration["Cors:AllowedOrigins"]!.Split(','))
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials());
```

---

## Code Style

### Immutability (CRITICAL)

Never mutate objects or arrays — always return new copies.

**C#**
```csharp
// WRONG
public void UpdateUser(User user, string name) { user.Name = name; }

// CORRECT — use records with `with`
public record User(string Id, string Name, string Email);
public User UpdateUser(User user, string name) => user with { Name = name };
```

**TypeScript**
```typescript
// WRONG
function updateUser(user, name) { user.name = name; return user; }

// CORRECT
function updateUser(user, name) { return { ...user, name }; }
```

### File Organisation

- Many small files over few large files
- 200–400 lines typical; hard max 800 lines per file
- Organise by feature/domain, not by type
- High cohesion, low coupling

### Error Handling

- Handle errors explicitly at every level; never silently swallow them
- Return user-friendly messages in UI-facing code; log full context server-side

**C#**
```csharp
public async Task<Result<User>> GetUserAsync(string id, CancellationToken ct = default)
{
    try
    {
        var user = await _repository.GetByIdAsync(id, ct);
        return user is null
            ? Result<User>.Failure("User not found")
            : Result<User>.Success(user);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get user {UserId}", id);
        return Result<User>.Failure("An unexpected error occurred");
    }
}
```

**TypeScript**
```typescript
try {
  const result = await riskyOperation();
  return { success: true, data: result };
} catch (error) {
  console.error('Operation failed:', error);
  return { success: false, error: 'Something went wrong. Please try again.' };
}
```

### Input Validation

- Validate all input at system boundaries before processing
- Use `[ApiController]` + Data Annotations or FluentValidation in C#
- Use Zod in TypeScript

**TypeScript**
```typescript
import { z } from 'zod';
const schema = z.object({ email: z.string().email(), age: z.number().int().min(0) });
const validated = schema.parse(input);
```

### C# Async/Nullable

- Always `async Task`, never `async void` (except event handlers)
- Always accept `CancellationToken` in async methods
- Enable `<Nullable>enable</Nullable>` — handle nullability explicitly

### No Console.log / Console.WriteLine in Production

Use structured logging (`ILogger` in C#, a logging library in TypeScript).

---

## Design Patterns

- **Repository pattern** — all data access goes through `IRepository<T>` (C#) or a typed `Repository<T>` interface (TS). See `rules/csharp/patterns.md` and `rules/typescript/patterns.md`.
- **Result pattern (C#)** — services return `Result<T>.Success(value)` or `Result<T>.Failure(error)`; never throw for expected failures. See `rules/csharp/patterns.md`.
- **API response envelope (TS)** — wrap all responses in `{ success, data?, error?, meta? }`. See `rules/typescript/patterns.md`.

---

## Security

Before suggesting or committing any code, verify:

- [ ] No hardcoded secrets, API keys, passwords, or tokens
- [ ] All user inputs validated (Data Annotations / FluentValidation / Zod)
- [ ] SQL injection prevented — parameterized queries / EF Core only
- [ ] XSS prevented — sanitize HTML output
- [ ] CSRF protection enabled
- [ ] Authentication and authorization verified on every endpoint
- [ ] Rate limiting applied to all public endpoints
- [ ] Error messages do not leak stack traces or internal details

**Always use environment variables for secrets:**

```csharp
// NEVER
private const string ApiKey = "sk-1234567890abcdef";

// ALWAYS
private readonly string _apiKey = configuration["Service:ApiKey"]
    ?? throw new InvalidOperationException("Service:ApiKey not configured");
```

```typescript
// NEVER
const apiKey = 'sk-proj-xxxxx';

// ALWAYS
const apiKey = process.env.SERVICE_API_KEY;
if (!apiKey) throw new Error('SERVICE_API_KEY not configured');
```

---

## Testing

### Requirements

- Minimum 80% code coverage
- TDD: write the test first (RED → GREEN → REFACTOR)
- All three test types are required:
  1. **Unit** — individual functions, utilities, components
  2. **Integration** — API endpoints, database operations
  3. **E2E** — critical user flows (Playwright)

### C# Test Naming Convention

`MethodName_Scenario_ExpectedBehavior`

```csharp
[Fact]
public async Task CreateUser_WithValidData_ReturnsSuccess() { }

[Fact]
public async Task CreateUser_WithDuplicateEmail_ReturnsFailure() { }
```

Use xUnit (`[Fact]` / `[Theory]`), FluentAssertions (`.Should().Be()`), and Moq for mocking.

### E2E Tests (Playwright) — MANDATORY

Run after every feature, bug fix, or before any PR:

```bash
npx playwright test                    # all tests
npx playwright test e2e/auth-flow.spec.ts  # specific suite
npx playwright test --ui               # debug mode
```

E2E tests must cover:
- Authentication: login, logout, registration
- Customer flows: browse, book, view bookings
- Admin flows: manage movies, halls, showtimes
- Error scenarios: invalid input, permissions, failures

---

## Git Workflow

### Commit Message Format

```
<type>: <description>

<optional body>
```

Types: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `perf`, `ci`

### Pre-Commit Checklist

- [ ] All unit and integration tests pass
- [ ] All E2E tests pass (`npx playwright test`)
- [ ] No linting errors
- [ ] Coverage is 80%+
- [ ] No hardcoded secrets
- [ ] CORS verified (after backend changes)

### PR Rules

- Never commit directly to `main`
- All tests must pass before merge
- PRs require review
- Include E2E test results in PR description


