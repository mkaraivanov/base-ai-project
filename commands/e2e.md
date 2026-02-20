---
description: Generate and run end-to-end tests with Playwright. Creates test journeys, runs tests, captures screenshots/videos/traces. Mandatory before every PR.
---

# E2E Command

This command invokes the **e2e-runner** agent to generate, run, and maintain Playwright tests.
For Page Object Model patterns and CI setup: `@skill: e2e-testing`

## When to Use

- After implementing any feature that touches user-facing flows
- Before opening a PR (mandatory)
- When adding new user journeys (auth, booking, admin flows)
- When debugging production regressions

## Core Flows to Cover

- **Auth**: login, logout, registration
- **Customer**: browse movies, book seats, view bookings
- **Admin**: manage movies, halls, showtimes
- **Errors**: invalid input, permission denied, network failures

## Run Commands

```bash
npx playwright test                          # all tests
npx playwright test e2e/auth-flow.spec.ts    # specific file
npx playwright test --ui                     # visual debug mode
npx playwright test --reporter=html          # HTML report
npx playwright show-report                   # view last report
npx playwright codegen http://localhost:3000  # record new test
```

## Agent Invocation

The **e2e-runner** agent will:
1. Analyze user journeys from the feature description
2. Generate tests using Page Object Model (see `@skill: e2e-testing`)
3. Use semantic locators (`getByRole`, `getByLabel`) not CSS selectors
4. Run across browsers; capture screenshots/traces on failure
5. Flag flaky tests and recommend fixes

## Skill Reference

`@skill: e2e-testing` — POM structure, fixture setup, CI artifact upload, flaky test quarantine
