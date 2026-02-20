---
paths:
  - "**/*.cs"
---
# C# Patterns

> Principles only. Full implementations: `@skill: dotnet-patterns`, `@skill: efcore-patterns`
> This file extends [common/patterns.md](../common/patterns.md) with C# specific content.

## Repository Pattern

- Define `IRepository<T>` with async operations: `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`
- Extend with domain-specific interfaces: `IUserRepository : IRepository<User>`
- Implement using EF Core with `AsNoTracking()` for read queries
- Scoped lifetime (`AddScoped<IUserRepository, UserRepository>()`)

## Result Pattern

- Return `Result<T>` from all service methods — never throw for expected failures
- `Result<T>.Success(value)` / `Result<T>.Failure(string error)` / `Result<T>.Failure(List<string> errors)`
- Map to HTTP responses in endpoints: `IsSuccess` → 2xx; failure → 400/404 as appropriate

## Options Pattern

- Use strongly-typed options classes with a `SectionName` constant
- Register with `.ValidateDataAnnotations().ValidateOnStart()` to fail fast
- Inject via `IOptions<T>`, `IOptionsSnapshot<T>` (per-request), or `IOptionsMonitor<T>` (hot-reload)

## CQRS / Mediator Pattern

- Commands mutate state; queries only read — keep them separate
- Use MediatR: commands/queries implement `IRequest<TResponse>`; handlers implement `IRequestHandler<,>`
- Register handlers with `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))`

## Minimal API vs Controller

- **Minimal API**: prefer for focused endpoints; use `.WithName()`, `.WithOpenApi()`, `.WithValidation()`
- **Controller** (`[ApiController]`): use when heavy model binding, filters, or action-level attributes are needed

## Dependency Injection Lifetimes

- **Transient** — stateless services; new instance per resolution
- **Scoped** — one per HTTP request (DbContext, repositories)
- **Singleton** — one for app lifetime (caches, `HttpClientFactory`)
- Never inject scoped services into singletons

## Background Services

- Implement `BackgroundService`; use `IServiceScopeFactory` to create scoped services inside workers
- Handle `OperationCanceledException` from `stoppingToken` gracefully

## API Response Format

- Return `ApiResponse<T> { Success, Data?, Error?, Errors[], Meta? }` from all endpoints
- Include `PaginationMeta { Total, Page, Limit, TotalPages }` on list responses

