# Testing Requirements

## Minimum Test Coverage: 80%

Test Types (ALL required):
1. **Unit Tests** - Individual functions, utilities, components
2. **Integration Tests** - API endpoints, database operations
3. **E2E Tests** - Critical user flows (Playwright for web applications)

## Test-Driven Development

MANDATORY workflow:
1. Write test first (RED)
2. Run test - it should FAIL
3. Write minimal implementation (GREEN)
4. Run test - it should PASS
5. Refactor (IMPROVE)
6. Verify coverage (80%+)
7. **Run ALL E2E tests** to ensure no regressions

## E2E Testing Requirements

### When to Run E2E Tests

**MANDATORY** - Run all E2E tests:
- After implementing any new feature
- Before committing code
- After fixing bugs that affect user flows
- Before creating a pull request

### Running E2E Tests

```bash
# Run all E2E tests
npx playwright test

# Run specific test suite
npx playwright test e2e/customer-movies.spec.ts

# Run tests in UI mode for debugging
npx playwright test --ui

# Run tests in headed mode
npx playwright test --headed
```

### E2E Test Coverage

Ensure E2E tests exist for:
- **Authentication flows**: Login, logout, registration
- **Customer flows**: Browse movies, book tickets, view bookings
- **Admin flows**: Manage movies, halls, showtimes
- **Error scenarios**: Invalid input, failed operations, permissions

### Creating New E2E Tests

When adding a new feature:
1. Identify all user-facing screens/flows affected
2. Create or update E2E test file in `e2e/` directory
3. Test happy paths AND error scenarios
4. Verify error messages are user-friendly
5. Run all E2E tests to ensure no regressions

### E2E Test Structure

```typescript
import { test, expect } from '@playwright/test';

test.describe('Feature Name', () => {
  test.beforeEach(async ({ page }) => {
    // Setup code
  });

  test('should handle happy path', async ({ page }) => {
    // Test implementation
  });

  test('should handle error scenario', async ({ page }) => {
    // Test error handling
  });
});
```

## Troubleshooting Test Failures

1. Use **tdd-guide** agent
2. Check test isolation
3. Verify mocks are correct
4. Fix implementation, not tests (unless tests are wrong)
5. For E2E failures, check browser console and network logs

## Agent Support

- **tdd-guide** - Use PROACTIVELY for new features, enforces write-tests-first
- **e2e-runner** - Use for running and debugging E2E tests
