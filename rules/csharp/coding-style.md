---
paths:
  - "**/*.cs"
  - "**/*.csproj"
---
# C# Coding Style

> This file extends [common/coding-style.md](../common/coding-style.md) with C# specific content.

## Immutability (CRITICAL)

- Use `record` types for data objects; use `with` expressions to produce new copies
- Use `init`-only properties on classes where mutation must be prevented
- Never mutate an object passed into a method — return a new instance

## Nullable Reference Types

- Always enable `<Nullable>enable</Nullable>` in every project
- Handle `null` explicitly — avoid `!` (null-forgiving) unless the value is provably non-null
- Use `is null` / `is not null` pattern checks, not `== null`

## Async / Await

- Return `async Task` (never `async void`, except event handlers)
- Every async public method must accept `CancellationToken cancellationToken = default`
- No `ConfigureAwait(false)` in ASP.NET Core code (no sync context); use it in library code

## LINQ

- Materialise queries once: call `.ToList()` / `.ToArrayAsync()` before iterating multiple times
- Use `Any()` for existence checks, not `Count() > 0`
- Prefer method syntax over query syntax for consistency

## Naming Conventions

- PascalCase: types, public members, methods, properties
- `_camelCase`: private fields
- `Async` suffix on all async methods
- File-scoped namespaces (`namespace MyApp.Services;`)

## Error Handling

- Catch specific exceptions; log with `ILogger` structured logging
- Return `Result<T>` for expected failures — never throw for flow control
- Never log passwords, tokens, or PII

## Code Quality Checklist

Before marking work complete:
- [ ] Nullable reference types enabled and properly annotated
- [ ] No `async void` methods (except event handlers)
- [ ] CancellationToken parameters on async methods
- [ ] No multiple LINQ enumerations
- [ ] Pattern matching used where appropriate
- [ ] Records used for immutable data
- [ ] Private fields use `_camelCase`
- [ ] Async methods have `Async` suffix
- [ ] File-scoped namespaces used
- [ ] Proper exception handling with logging

