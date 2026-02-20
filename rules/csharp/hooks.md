---
paths:
  - "**/*.cs"
  - "**/*.csproj"
---
# C# Hooks

> Full hook JSON configs and .editorconfig/.csproj setup: see `rules/csharp/` archive or ask the build-error-resolver agent.
> This file extends [common/hooks.md](../common/hooks.md) with C# specific content.

## PostToolUse Hooks (configure in `~/.claude/settings.json`)

- **dotnet-format** — run `dotnet format --include {file}` after every `.cs` edit
- **dotnet-build-check** — run `dotnet build /p:TreatWarningsAsErrors=true` after edits to catch analyzer warnings
- **stylecop-check** — run `dotnet build -p:EnforceCodeStyleInBuild=true` to enforce style rules

## PreCommit Hooks

- Run `dotnet test --no-build` before every commit; abort if tests fail
- Run `dotnet format --verify-no-changes` to block commits with formatting issues
- Check coverage threshold: abort if line coverage < 80%

## Stop Hooks

- Run `dotnet list package --vulnerable --include-transitive` at session end to surface CVEs
- Run `dotnet build --configuration Release` to verify release build succeeds

## Project File Requirements

Enable in `.csproj`:
- `<Nullable>enable</Nullable>`
- `<EnableNETAnalyzers>true</EnableNETAnalyzers>`
- `<AnalysisMode>All</AnalysisMode>`
- `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`

Packages: `StyleCop.Analyzers`, `Microsoft.CodeAnalysis.NetAnalyzers`

## CI (GitHub Actions)

Steps: `dotnet restore` → `dotnet format --verify-no-changes` → `dotnet build --configuration Release` → `dotnet test --collect:"XPlat Code Coverage"` → `dotnet list package --vulnerable`

## Agent Support
