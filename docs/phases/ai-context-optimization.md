# AI Context Optimization Action Plan

> **Goal:** Reduce tokens loaded per Claude Code / GitHub Copilot session by 40–60%
> while preserving full developer experience.
>
> **Audit date:** 2026-02-20
> **Files audited:** 69 configuration files across `.github/`, `rules/`, `agents/`, `commands/`, `skills/`, `contexts/`, `hooks/`

---

## Background

The AI configuration has grown organically into a multi-layer system. The same
concepts (immutability, TDD, security checklist, Repository pattern, etc.) now
appear in 3–7 files each, causing redundant token cost on every session and
agent invocation.

**Layering intent (design goal):**

| Layer | File location | Load timing | Target size |
|---|---|---|---|
| Project entrypoint | `CLAUDE.md` (root) | Every session | ≤ 150 lines |
| Always-on principles | `rules/*/` | Every relevant file open | ≤ 80 lines/file |
| On-demand reference | `skills/*/SKILL.md` | When agent requests | Unlimited |
| Behavioural triggers | `agents/*.md` | When explicitly invoked | ≤ 150 lines |
| User workflows | `commands/*.md` | When slash command run | ≤ 100 lines |

---

## Phase 1 — Create a Root `CLAUDE.md` *(Highest Impact)*

**Problem:** No `CLAUDE.md` exists at the project root. Claude Code has no authoritative
project entrypoint, so every session starts with exploratory file searches.

**Actions:**
1. Create `CLAUDE.md` at the project root (≤ 150 lines)
2. Include:
   - Project overview (2–3 sentences): ASP.NET Core backend, Vite/React/TS frontend, SQL Server
   - Directory map: `Backend/` → API, `frontend/src/` → UI, `e2e/` → Playwright tests, `rules/` → conventions
   - Pointer to `rules/csharp/` and `rules/typescript/` for language conventions
   - Pointer to `commands/orchestrate.md` as the canonical development workflow
   - Active MCP servers (list the 8 in use — see Phase 6)
3. Do **not** duplicate any rule content inline — reference layers instead
4. Add a `## Quick Commands` section listing the 5–7 most-used slash commands

**Expected savings:** Eliminates session-start file crawls; Claude Code has immediate
project orientation without reading multiple files.

---

## Phase 2 — Trim `copilot-instructions.md` to Principles Only *(High Impact for Copilot)*

**Problem:** `.github/copilot-instructions.md` is ~240 lines and contains full code
block implementations of patterns that are documented in `rules/` and `skills/`.
GitHub Copilot cannot access `rules/`, so some content must stay — but code examples
should be condensed to single-line principles.

**Actions:**
1. Collapse the **Repository pattern** (C# + TS code blocks) to:
   *"Use the Repository pattern for all data access — `IRepository<T>` in C#, typed `Repository<T>` in TS."*
2. Collapse the **Result pattern** C# code block to:
   *"Return `Result<T>` for all service methods — `Result<T>.Success(value)` / `Result<T>.Failure(error)`."*
3. Collapse the **API response envelope** TS interface to:
   *"Wrap all API responses in `{ success, data?, error?, meta? }`."*
4. Remove the duplicate **Code Quality Checklist** section (identical content appears above it in Code Style)
5. Keep intact: CORS reminder, 8-item security checklist, pre-commit checklist, xUnit naming convention, git commit format
6. Target: reduce from ~240 lines → ~120 lines

**Expected savings:** ~120 lines removed from every Copilot inline context injection.

---

## Phase 3 — Thin Out `rules/csharp/` *(High Impact for Claude Code)*

**Problem:** `rules/csharp/` totals ~1,820 lines — almost entirely detailed code examples
that duplicate `skills/dotnet-*/SKILL.md`. The rules layer is **always-on** (loaded for
every `.cs` file); skills are loaded **on demand**. Detailed code belongs in skills only.

**Target: ~320 lines total (from ~1,820) — an 82% reduction**

| File | Current | Target | Action |
|---|---|---|---|
| `rules/csharp/security.md` | 451 lines | ~80 lines | Strip all code blocks; keep 8-item checklist + `@skill: dotnet-security` pointer |
| `rules/csharp/patterns.md` | 397 lines | ~60 lines | Remove EF Core implementations; keep pattern names + `@skill: dotnet-patterns`, `@skill: efcore-patterns` |
| `rules/csharp/testing.md` | 405 lines | ~60 lines | Remove WebApplicationFactory/TestContainers examples; keep naming convention + `@skill: dotnet-testing` |
| `rules/csharp/hooks.md` | 308 lines | ~40 lines | Keep hook trigger events; move Roslyn/StyleCop setup to `docs/` |
| `rules/csharp/coding-style.md` | 259 lines | ~80 lines | Keep principles + LINQ constraints; collapse ConfigureAwait to one rule |

**Expected savings:** ~1,500 lines removed from always-on C# context per session.

---

## Phase 4 — Remove Irrelevant Skills *(Quick Win)*

**Problem:** Several skills are loaded during `semantic_search` results and agent planning
but serve no purpose for an ASP.NET Core + React project.

**Actions:**
1. **Delete** `skills/nutrient-document-processing/` — PDF/OCR API, not used in this project
2. **Delete** `skills/clickhouse-io/` — ClickHouse analytics engine, not used in this project
3. **Archive to `examples/`**: `skills/project-guidelines-example/` — a template, not functional
4. **Delete** `skills/continuous-learning/` (v1, 119 lines) — superseded entirely by `skills/continuous-learning-v2/`
5. **Move to `examples/`**: `skills/configure-ecc/` — setup wizard for new projects, not a development skill

**Expected savings:** ~1,362 lines of irrelevant content removed from semantic search scope,
improving result quality and reducing false-positive file reads.

---

## Phase 5 — Trim Large Commands *(Medium Impact)*

**Problem:** Several slash commands contain inline skill content, making the skills
layer redundant when the command file is in context.

**Target: ~900 lines removed**

**Actions:**
1. `commands/tdd.md` (327 lines → ~80 lines): Replace inline scaffolding examples with
   `@skill: tdd-workflow`; keep orchestration steps and the RED/GREEN/REFACTOR checklist only
2. `commands/e2e.md` (364 lines → ~80 lines): Replace POM examples with `@skill: e2e-testing`;
   keep run commands (`npx playwright test`) and CI integration steps
3. `commands/python-review.md` (298 lines): Archive — Python review is not relevant to a
   .NET/TS project (move to `archive/`)
4. `commands/sessions.md` (306 lines → ~60 lines): Extract detailed implementation to
   `docs/`; keep the command as a workflow trigger only
5. `commands/multi-execute.md` (311 lines → ~80 lines): Same — move deep implementation
   docs to `docs/`

---

## Phase 6 — Structural & Tooling Improvements *(Ongoing)*

### 6a. Cap Active MCP Servers
`mcp-configs/mcp-servers.json` lists 16 servers. The file itself notes to keep under 10.
- Identify the 8 servers actively used by this project
- Comment out the rest with a note marker
- Document the active list in the root `CLAUDE.md`

### 6b. Generate Architecture Codemaps
Run `commands/update-codemaps.md` to produce token-lean architecture summaries in `docs/codemaps/`.
Claude Code loads a codemap (~2 KB) instead of crawling `Backend/` or `frontend/src/` at session start.

### 6c. Enable Strategic Compaction
`skills/strategic-compact/` exists but is not referenced in daily workflows.
- Add to root `CLAUDE.md`: *"Compact context at logical boundaries: after completing a full feature,
  before switching between backend and frontend work."*
- Ensures sessions don't hit the 80% context window limit unexpectedly.

### 6d. Apply `iterative-retrieval` in Subagent Prompts
When spawning subagents via orchestration, instruct them to follow `skills/iterative-retrieval/` —
pull context progressively (high-level first, then targeted reads) rather than front-loading entire
directories.

### 6e. Common Rules `applyTo` Scoping
`rules/common/` files currently load for all file types. Add explicit `applyTo` path hints to help
IDEs and Copilot scope them correctly:
- `rules/common/agents.md` → no path filter (apply globally)
- `rules/common/testing.md` → add hint for test file paths
- `rules/common/performance.md` → no path filter (apply globally)

---

## Summary: Expected Impact

| Phase | Lines Removed | Affected Tool |
|---|---|---|
| 1. Root CLAUDE.md | +150 (new file) | Claude Code |
| 2. Trim copilot-instructions.md | -120 | GitHub Copilot |
| 3. Thin rules/csharp/ | -1,500 | Claude Code |
| 4. Remove irrelevant skills | -1,362 | Claude Code |
| 5. Trim large commands | -900 | Claude Code |
| **Total net reduction** | **~-3,882 lines** | |

Sessions become faster, cheaper, and more focused. The rich detail remains fully available
in `skills/` — just not loaded unless an agent explicitly needs it.

---

## Verification Checklist

After completing each phase, validate by starting a fresh Claude Code session and asking:
*"What are the coding conventions for this project?"*

The answer must be complete and accurate. Then confirm:

- [ ] `dotnet build` succeeds
- [ ] `npx playwright test` passes all E2E tests
- [ ] `npm run lint` passes
- [ ] A minimal TDD cycle (red → green → refactor) works end-to-end
- [ ] GitHub Copilot inline suggestions still respect immutability and naming conventions
