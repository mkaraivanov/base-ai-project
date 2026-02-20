# Execute — Multi-Model Collaborative Execution

Collaborative implementation using Claude + external models (Codex / Gemini).
External models produce a **dirty prototype** (Unified Diff); Claude refactors to production-grade code.

Run only after user confirms the plan from `/multi-plan` with "Y".

## Protocol Rules

- **Code Sovereignty**: External models have zero filesystem write access — Claude applies all changes
- **Dirty Prototype**: Treat external model output as a draft; always refactor before committing
- **Stop-Loss**: Validate each phase output before proceeding to the next
- **Language**: Use English for tool/model calls; respond to user in their language

## Workflow Phases

1. **Phase 1 — Prototype** (Codex/Gemini, parallel):
   - Pass plan + context; request Unified Diff output only
   - Models must not modify files directly

2. **Phase 2 — Refactor** (Claude):
   - Apply diffs with `git apply`
   - Refactor to meet project standards (immutability, Result pattern, tests)
   - Run `dotnet build` / `tsc --noEmit` to verify

3. **Phase 3 — Audit** (Codex/Gemini, parallel):
   - Pass `git diff` of applied changes for code review
   - Models review correctness, security, and requirements alignment

4. **Phase 4 — Delivery** (Claude):
   - Address audit findings
   - Run full test suite and E2E
   - Commit with conventional commit message

## Backend Selection

- `--backend codex` — OpenAI Codex (strong at precise code edits)
- `--backend gemini` — Google Gemini (strong at high-level reasoning)

## Relationship with Other Commands

1. `/multi-plan` generates plan + SESSION_ID
2. User confirms with "Y"
3. `/ccg:execute` reads plan, reuses SESSION_ID, executes implementation

See `commands/multi-workflow.md` for the full 6-phase workflow.
