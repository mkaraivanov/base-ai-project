// Playwright test boilerplate for E2E tests
// See https://playwright.dev/docs/test-intro for more details
import { test, expect } from '@playwright/test';

test.describe('Main Flows', () => {
  test('Homepage loads', async ({ page }) => {
      await page.goto('/');
      // Adjust the title check if needed
      await expect(page).toHaveTitle(/CineBook - Movie Tickets/i);
    });

  // Add more main flow tests here
});
