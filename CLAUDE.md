# Cinema Booking System

A cinema booking platform built with **ASP.NET Core** (C#) backend and **Vite/React/TypeScript** frontend using SQL Server for storage.

## Directory Layout

```
Backend/          → ASP.NET Core Minimal API (C#)
  Application/    → Services, use cases, DTOs, validators
  Domain/         → Entities, value objects, common abstractions
  Infrastructure/ → EF Core, repositories, migrations, caching
  Endpoints/      → Minimal API route handlers
  Middleware/     → Auth, error handling
  Models/         → Shared request/response models
  Tests/
    Tests.Unit/   → xUnit unit tests (Services, Validators, Builders)
frontend/src/     → Vite + React + TypeScript UI
e2e/              → Playwright end-to-end tests
rules/            → Always-on coding conventions (loaded per file type)
  csharp/         → C# principles (see skills/ for detailed examples)
  typescript/     → TypeScript principles (see skills/ for detailed examples)
  common/         → Language-agnostic rules (git, testing, security, etc.)
agents/           → Specialist AI agents (invoke explicitly)
skills/           → On-demand reference libraries (agents load when needed)
commands/         → Slash command workflows
```

## Tech Stack

| Concern | Technology |
|---|---|
| Backend API | ASP.NET Core Minimal API (.NET 9) |
| ORM | Entity Framework Core + SQL Server |
| Rate Limiting | AspNetCoreRateLimit |
| Frontend | React 18, Vite, TypeScript |
| Auth | JWT (bearer tokens) |
| Validation | FluentValidation (C#), Zod (TS) |
| Testing (unit) | xUnit + FluentAssertions + Moq |
| Testing (E2E) | Playwright (`npx playwright test`) |

## Quick Commands

| Slash command | When to use |
|---|---|
| `/plan` | Before writing code — create implementation plan and wait for approval |
| `/tdd` | Starting any new feature or bug fix |
| `/code-review` | Before opening a PR |
| `/e2e` | Generate or run Playwright tests |
| `/build-fix` | When `dotnet build` or `tsc` fails |
| `/orchestrate` | Full feature workflow (plan → implement → review → E2E) |
| `/verify` | Final check before commit |
| `/checkpoint` | Save a named progress point during long tasks |
| `/refactor-clean` | Remove dead code / unused exports safely |
| `/test-coverage` | Analyze coverage gaps and generate missing tests |
| `/update-docs` | Update READMEs and codemaps after changes |

## Development Workflow

See `commands/orchestrate.md` for the canonical multi-step workflow.

```bash
# Backend — build & test
dotnet build
dotnet test
dotnet test --collect:"XPlat Code Coverage"   # with coverage report
dotnet run --project Backend

# Backend — database migrations
dotnet ef migrations add <MigrationName> --project Backend
dotnet ef database update --project Backend

# Frontend — dev server & checks
cd frontend && npm run dev
cd frontend && npm run lint    # ESLint — must pass before commit
cd frontend && npm run build   # Type-check + bundle

# E2E
npx playwright test
npx playwright test --ui   # debug mode
npx playwright test e2e/customer-booking-flow.spec.ts   # single file
```

> **Note:** The frontend has no standalone `npm test` script — unit/integration testing
> happens on the backend (xUnit); browser behavior is covered by Playwright E2E tests.

## Available Agents

Invoke agents explicitly when needed. All agents are in `agents/`.

| Agent | When to invoke |
|---|---|
| `planner` | Complex new features or architectural changes — creates a phased plan before coding |
| `architect` | System design decisions, scalability questions, technology trade-offs (uses Opus) |
| `tdd-guide` | Enforces RED→GREEN→REFACTOR; use for every new feature or bug fix |
| `code-reviewer` | After writing or modifying any code — checks quality, style, correctness |
| `security-reviewer` | After auth, user-input, or API endpoint changes — checks OWASP Top 10 |
| `database-reviewer` | SQL queries, EF Core schema changes, migration design, performance |
| `e2e-runner` | Generating, running, or debugging Playwright test suites |
| `build-error-resolver` | When `dotnet build` or `tsc` fails — fixes errors with minimal diffs |
| `refactor-cleaner` | Removes dead code using static analysis (knip, ts-prune) |
| `doc-updater` | Keeps READMEs and codemaps in sync after implementation |

## Key Rules (summary — full detail in rules/)

- **Immutability**: C# → records with `with`; TS → spread operator
- **Async**: always `async Task`, always accept `CancellationToken`; no `async void`
- **Error handling**: never swallow silently; return `Result<T>` from services
- **Validation**: FluentValidation on DTOs (C#), Zod at API boundaries (TS)
- **Testing**: TDD (RED → GREEN → REFACTOR), 80%+ coverage; **unit + integration + E2E tests ALL required** — E2E alone is never sufficient; feature is INCOMPLETE without non-E2E tests
- **Secrets**: environment variables only — never hardcode; `appsettings.json` holds development defaults only
- **CORS**: re-verify after every backend rebuild
- **Localization**: ALL user-visible strings must use `t()` — never hardcode text in components (see Localization section below)
- **Naming (C#)**: PascalCase for types/methods/properties, `_camelCase` for private fields, `Async` suffix on async methods, file-scoped namespaces
- **Naming (TS)**: camelCase for variables/functions, PascalCase for components/types/interfaces
- **LINQ**: use `Any()` not `Count() > 0`; materialise queries once with `.ToListAsync()` before iterating

## Environment Configuration

`appsettings.json` contains **development defaults** (local SQL Server, placeholder JWT key).
In production, override via environment variables — never commit real secrets:

```
ConnectionStrings__DefaultConnection=<prod-connection-string>
JwtSettings__SecretKey=<strong-random-key-32+-chars>
```

## Localization

This project uses **i18next + react-i18next**. Every new feature MUST include translations for all supported languages.

### Supported Languages
- `en` — English (fallback)
- `bg` — Bulgarian

### Namespaces & Files
Translation files live in `/frontend/public/locales/{lang}/{namespace}.json`:

| Namespace | File | Scope |
|---|---|---|
| `common` | `common.json` | Global: nav, actions, status badges, pagination, footer |
| `auth` | `auth.json` | Login, register, auth errors |
| `admin` | `admin.json` | All admin management pages |
| `customer` | `customer.json` | Customer-facing pages: movies, seats, checkout, bookings |

### Rules (MANDATORY)

1. **Never hardcode user-visible strings** in React components — always use `const { t } = useTranslation('namespace')` and `t('key')`.
2. **Add translations to all languages** when adding any new key. A feature is INCOMPLETE if any supported language is missing a translation.
3. **Form field labels** must use translation keys (e.g. `label={t('cinemas.form.name')}`).
4. **Toast messages** (success and error) must use `t()` calls — no raw English strings in `toast.success(...)` or `toast.error(...)`.
5. **Tooltip titles** must use `t()` (e.g. `title={t('common.edit')}`, `title={t('common.delete')}`).
6. **Count/plural strings** must use i18next `count_one`/`count_other` suffix pattern:
   ```json
   "count_one": "{{count}} item",
   "count_other": "{{count}} items"
   ```
   Called as: `t('section.count', { count: items.length })`
7. **Dynamic values** use i18next interpolation: `"copyright": "© {{year}} CineBook."` → `t('footer.copyright', { year })`.
8. **Namespace selection**: choose the most specific namespace for each string; use `common` only for truly global strings shared across multiple pages.

### Adding Translations Checklist

When implementing a new feature or page:
- [ ] All visible labels, titles, tooltips added to `/frontend/public/locales/en/{ns}.json`
- [ ] Same keys added with Bulgarian translations to `/frontend/public/locales/bg/{ns}.json`
- [ ] Toast success/error messages translated
- [ ] Form field labels (including `*` required markers) translated
- [ ] Empty state messages translated
- [ ] Count/plural keys use `count_one`/`count_other` pattern
- [ ] No raw English strings remain in JSX or `toast.*()` calls

## Context Management

- Compact context at logical boundaries (after completing a feature; before switching backend ↔ frontend)
- Use `skills/strategic-compact/` when approaching 80% context usage
- Subagents should use `skills/iterative-retrieval/` — pull context progressively

## Active MCP Servers

See `mcp-configs/mcp-servers.json` for full list. Recommended active set (≤8):
`github`, `memory`, `sequential-thinking`, `context7`, `filesystem`, `supabase`, `firecrawl`, `magic`
