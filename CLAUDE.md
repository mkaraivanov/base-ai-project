# Cinema Booking System

A cinema booking platform built with **ASP.NET Core** (C#) backend and **Vite/React/TypeScript** frontend using SQL Server for storage.

## Directory Layout

```
Backend/          → ASP.NET Core Minimal API (C#)
  Application/    → Services, use cases
  Domain/         → Entities, value objects
  Infrastructure/ → EF Core, repositories, external integrations
  Endpoints/      → Minimal API route handlers
  Middleware/     → Auth, error handling
frontend/src/     → Vite + React + TypeScript UI
e2e/              → Playwright end-to-end tests
rules/            → Always-on coding conventions (loaded per file type)
  csharp/         → C# principles (see skills/ for detailed examples)
  typescript/     → TypeScript principles (see skills/ for detailed examples)
  common/         → Language-agnostic rules
agents/           → Specialist AI agents (invoke explicitly)
skills/           → On-demand reference libraries (agents load when needed)
commands/         → Slash command workflows
```

## Tech Stack

| Concern | Technology |
|---|---|
| Backend API | ASP.NET Core Minimal API (.NET 9) |
| ORM | Entity Framework Core + SQL Server |
| Frontend | React 18, Vite, TypeScript |
| Auth | JWT (bearer tokens) |
| Validation | FluentValidation (C#), Zod (TS) |
| Testing (unit) | xUnit + FluentAssertions + Moq |
| Testing (E2E) | Playwright (`npx playwright test`) |

## Quick Commands

| Slash command | When to use |
|---|---|
| `/tdd` | Starting any new feature or bug fix |
| `/code-review` | Before opening a PR |
| `/e2e` | Generate or run Playwright tests |
| `/build-fix` | When `dotnet build` or `tsc` fails |
| `/orchestrate` | Full feature workflow (plan → implement → review → E2E) |
| `/verify` | Final check before commit |

## Development Workflow

See `commands/orchestrate.md` for the canonical multi-step workflow.

```bash
# Backend
dotnet build
dotnet test
dotnet run --project Backend

# Frontend
cd frontend && npm run dev

# E2E
npx playwright test
npx playwright test --ui   # debug mode
```

## Key Rules (summary — full detail in rules/)

- **Immutability**: C# → records with `with`; TS → spread operator
- **Async**: always `async Task`, always accept `CancellationToken`; no `async void`
- **Error handling**: never swallow silently; return `Result<T>` from services
- **Validation**: FluentValidation on DTOs (C#), Zod at API boundaries (TS)
- **Testing**: TDD (RED → GREEN → REFACTOR), 80%+ coverage, E2E before PR
- **Secrets**: environment variables only — never hardcode
- **CORS**: re-verify after every backend rebuild

## Context Management

- Compact context at logical boundaries (after completing a feature; before switching backend ↔ frontend)
- Use `skills/strategic-compact/` when approaching 80% context usage
- Subagents should use `skills/iterative-retrieval/` — pull context progressively

## Active MCP Servers

See `mcp-configs/mcp-servers.json` for full list. Recommended active set (≤8):
`github`, `memory`, `sequential-thinking`, `context7`, `filesystem`, `supabase`, `firecrawl`, `magic`
