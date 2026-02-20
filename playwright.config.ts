// Playwright configuration for E2E tests
// See https://playwright.dev/docs/test-configuration
import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  testMatch: '**/*.spec.ts',
  timeout: 15000, // Set global test timeout to 15 seconds
  retries: 1,
  use: {
    baseURL: 'http://localhost:5173',
    headless: true,
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    // Default project: English locale (runs all specs)
    {
      name: 'en-US',
      use: { locale: 'en-US' },
    },
    // Bulgarian locale project: runs auth and booking flows with bg-BG headers so
    // the backend RequestLocalizationMiddleware selects the Bulgarian resource strings.
    {
      name: 'bg-BG',
      use: { locale: 'bg-BG' },
      testMatch: [
        '**/auth-flow.spec.ts',
        '**/customer-booking-flow.spec.ts',
      ],
    },
  ],
});
