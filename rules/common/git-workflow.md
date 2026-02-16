# Git Workflow

## Commit Message Format

```
<type>: <description>

<optional body>
```

Types: feat, fix, refactor, docs, test, chore, perf, ci

Note: Attribution disabled globally via ~/.claude/settings.json.

## Pull Request Workflow

When creating PRs:
1. Analyze full commit history (not just latest commit)
2. Use `git diff [base-branch]...HEAD` to see all changes
3. Draft comprehensive PR summary
4. Include test plan with TODOs
5. Push with `-u` flag if new branch

## Feature Implementation Workflow

1. **Plan First**
   - Use **planner** agent to create implementation plan
   - Identify dependencies and risks
   - Break down into phases

2. **TDD Approach**
   - Use **tdd-guide** agent
   - Write tests first (RED)
   - Implement to pass tests (GREEN)
   - Refactor (IMPROVE)
   - Verify 80%+ coverage

3. **E2E Testing (MANDATORY)**
   - Run ALL E2E tests after implementing feature
   - Create new E2E tests for new user flows
   - Verify no regressions in existing flows
   - Command: `npx playwright test`

4. **Code Review**
   - Use **code-reviewer** agent immediately after writing code
   - Address CRITICAL and HIGH issues
   - Fix MEDIUM issues when possible

5. **Commit & Push**
   - Detailed commit messages
   - Follow conventional commits format
   - Include E2E test results in PR description

## Pre-Commit Checklist

Before every commit:
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] **All E2E tests pass** (`npx playwright test`)
- [ ] Code reviewed (use **code-reviewer** agent)
- [ ] No linting errors
- [ ] Coverage is 80%+

## E2E Test Workflow

```bash
# 1. Run all E2E tests
npx playwright test

# 2. If tests fail, debug
npx playwright test --ui  # or --headed

# 3. Fix issues and re-run
npx playwright test

# 4. Only commit when all tests pass
git add . && git commit -m "feat: new feature"
```
