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
- **Testing**: TDD (RED → GREEN → REFACTOR), 80%+ coverage; **unit + integration + E2E tests ALL required** — E2E alone is never sufficient; feature is INCOMPLETE without non-E2E tests
- **Secrets**: environment variables only — never hardcode
- **CORS**: re-verify after every backend rebuild
- **Localization**: ALL user-visible strings must use `t()` — never hardcode text in components (see Localization section below)

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
