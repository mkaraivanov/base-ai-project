# E2E Tests

This directory contains end-to-end tests for the CineBook application using Playwright.

## Running Tests

### Prerequisites
1. Ensure backend is running: `cd Backend && dotnet run`
2. Ensure frontend is running: `cd frontend && npm run dev`

### Run All Tests
```bash
npx playwright test
```

### Run Specific Test File
```bash
npx playwright test e2e/customer-movies.spec.ts
```

### Run Tests in UI Mode (Debugging)
```bash
npx playwright test --ui
```

### Run Tests in Headed Mode
```bash
npx playwright test --headed
```

### Run Admin Tests Only
```bash
npx playwright test --grep "Admin"
```

## Test Structure

### Authentication Tests (`auth-flow.spec.ts`)
- ✅ Display login page and form
- ✅ Validate login with invalid credentials
- ✅ Successful login with valid credentials
- ✅ Logout functionality
- ✅ Protected route access
- ✅ Session persistence

### Registration Tests (`signup-flow.spec.ts`)
- ✅ Display registration form
- ✅ Password mismatch validation
- ✅ Password length validation
- ✅ Successful user registration
- ✅ Link to login page
- ✅ Required field validation

### Customer Tests

#### Movies Pages (`customer-movies.spec.ts`)
- ✅ Display movies list page
- ✅ Display movie cards
- ✅ Navigate to movie detail
- ✅ Display movie details and showtimes
- ✅ Navigate to seat selection

#### Booking Flow (`customer-booking-flow.spec.ts`)
- ✅ Complete booking: login → browse → select seats → checkout → confirmation
- ✅ Display seat layout
- ✅ Select and deselect seats
- ✅ Display total price
- ✅ Require authentication for checkout

#### My Bookings (`customer-my-bookings.spec.ts`)
- ✅ Require authentication
- ✅ Display bookings or empty state
- ✅ Show booking details
- ✅ Show booking reference number
- ✅ Allow booking cancellation

### Admin Tests

#### Dashboard (`admin-dashboard.spec.ts`)
- ✅ Require admin role
- ✅ Display statistics
- ✅ Navigation to management pages
- ✅ Display recent activity

#### Movies Management (`admin-movies.spec.ts`)
- ✅ Display movies list
- ✅ Create new movie
- ✅ Edit existing movie
- ✅ Delete movie (with validation)
- ✅ Cancel operations
- ✅ Show validation errors

#### Halls Management (`admin-halls.spec.ts`)
- ✅ Display halls list
- ✅ Create new hall
- ✅ Edit existing hall
- ✅ Delete hall
- ✅ Cancel operations
- ✅ Show validation errors

#### Showtimes Management (`admin-showtimes.spec.ts`)
- ✅ Display showtimes list
- ✅ Create new showtime
- ✅ Edit existing showtime
- ✅ Cancel operations
- ✅ Show validation errors

### Infrastructure Tests

#### API Health (`api-health.spec.ts`)
- ✅ Backend health endpoint returns 200

#### Main Flows (`main-flows.spec.ts`)
- ✅ Homepage loads
- ✅ Navigation consistency
- ✅ 404 handling
- ✅ Performance metrics

## Test Data

### Admin Account (Seeded)
```
Email: admin@cinebook.local
Password: Admin123!
```

### Test User Registration
Tests create unique users using timestamps to avoid conflicts:
```typescript
const testUser = {
  email: `test.user+${Date.now()}@example.com`,
  password: 'Test123456',
};
```

## Best Practices

### 1. Test Isolation
- Each test should be independent
- Use `beforeEach` hooks for setup
- Clean up test data when possible

### 2. Selectors
Prefer in order:
1. `data-testid` attributes
2. Semantic roles
3. Text content
4. CSS classes (least preferred)

Example:
```typescript
// Good
const button = page.locator('[data-testid="submit-button"]');
const heading = page.locator('h1').filter({ hasText: 'Movies' });

// Avoid
const button = page.locator('.btn.primary.large');
```

### 3. Waiting Strategies
- Use `waitForLoadState('networkidle')` for API-heavy pages
- Use `waitForURL()` for navigation assertions
- Use `waitFor({ state: 'visible' })` for dynamic content

### 4. Error Handling
- Always test error scenarios
- Verify error messages are user-friendly
- Test validation on forms

### 5. Test Organization
```typescript
test.describe('Feature Name', () => {
  test.beforeEach(async ({ page }) => {
    // Setup
  });

  test('should handle happy path', async ({ page }) => {
    // Test
  });

  test('should handle error case', async ({ page }) => {
    // Test
  });
});
```

## Adding New E2E Tests

When adding a new feature:

1. **Identify User Flows**
   - What screens are affected?
   - What user interactions are possible?
   - What error states exist?

2. **Create Test File**
   ```bash
   # Create new test file
   touch e2e/feature-name.spec.ts
   ```

3. **Write Tests**
   - Import Playwright test utilities
   - Use descriptive test names
   - Test happy paths first
   - Add error scenario tests
   - Verify error messages

4. **Run Tests Locally**
   ```bash
   npx playwright test e2e/feature-name.spec.ts
   ```

5. **Run All Tests**
   ```bash
   npx playwright test
   ```

6. **Commit Only After All Tests Pass**

## Debugging Failed Tests

### View Test Report
```bash
npx playwright show-report
```

### Run Single Test
```bash
npx playwright test -g "test name pattern"
```

### Enable Debug Mode
```bash
PWDEBUG=1 npx playwright test
```

### Take Screenshots on Failure
Screenshots are automatically captured on failure in `test-results/`

### Record Video
Update `playwright.config.ts`:
```typescript
use: {
  video: 'on-first-retry',
}
```

## CI/CD Integration

E2E tests should run:
- ✅ Before every commit (pre-commit hook)
- ✅ On every pull request
- ✅ Before deployment to staging/production

### GitHub Actions Example
```yaml
- name: Run E2E tests
  run: |
    npm install -D @playwright/test
    npx playwright install --with-deps
    npx playwright test
```

## Coverage Requirements

- **All user-facing features** must have E2E tests
- **Critical flows** (auth, booking, payment) require comprehensive coverage
- **Admin features** must be tested with proper role checks
- **Error handling** must be verified

## MANDATORY: Run E2E Tests After Every Feature

**BEFORE COMMITTING ANY NEW FEATURE:**
```bash
# 1. Ensure servers are running
cd Backend && dotnet run  # Terminal 1
cd frontend && npm run dev  # Terminal 2

# 2. Run ALL E2E tests
npx playwright test  # Terminal 3

# 3. Verify all tests pass
# 4. Only commit if all tests pass
```

## Questions?

If you have questions about E2E testing:
1. Check Playwright documentation: https://playwright.dev
2. Review existing tests for patterns
3. Ask in pull request comments
