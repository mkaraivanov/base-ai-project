---
description: Enforce test-driven development workflow. Scaffold interfaces, generate tests FIRST, then implement minimal code to pass. Ensure 80%+ coverage.
---

# TDD Command

This command invokes the **tdd-guide** agent to enforce test-driven development methodology.
For detailed scaffolding examples and coverage commands: `@skill: tdd-workflow`

## What This Command Does

1. **Scaffold Interfaces** - Define types/interfaces first
2. **Generate Tests First** - Write failing tests (RED)
3. **Implement Minimal Code** - Write just enough to pass (GREEN)
4. **Refactor** - Improve code while keeping tests green (REFACTOR)
5. **Verify Coverage** - Ensure 80%+ test coverage

## When to Use

- Implementing new features or components
- Fixing bugs (write test that reproduces it first)
- Refactoring code that lacks test coverage

## TDD Cycle

```
RED → GREEN → REFACTOR → REPEAT

RED:      Write a failing test
GREEN:    Write minimal code to pass
REFACTOR: Improve code, keep tests passing
REPEAT:   Next feature/scenario
```

## Run Commands

```bash
# .NET
dotnet test
dotnet test --collect:"XPlat Code Coverage"

# TypeScript / Vite
npm test
npm test -- --coverage
```

## Agent & Skill Reference

- Agent: `agents/tdd-guide.md` — enforces RED→GREEN→REFACTOR sequence
- Skill: `@skill: tdd-workflow` — full examples, mocking patterns, coverage thresholds, naming conventions
