import { test, expect } from '@playwright/test';

test.describe('Admin - Reports Page', () => {
  const adminCredentials = {
    email: 'admin@cinebook.local',
    password: 'Admin123!',
  };

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.fill('input#email, input[type="email"]', adminCredentials.email);
    await page.fill('input#password, input[type="password"]', adminCredentials.password);
    await page.click('button[type="submit"]');
    await page.waitForURL('/', { timeout: 10000 }).catch(() => {});
  });

  test('should show Reports link on admin dashboard', async ({ page }) => {
    await page.goto('/admin');
    await page.waitForLoadState('networkidle');
    const reportsCard = page.locator('.admin-card').filter({ hasText: /reports/i }).first();
    await expect(reportsCard).toBeVisible();
  });

  test('should navigate to reports page from dashboard', async ({ page }) => {
    await page.goto('/admin');
    await page.waitForLoadState('networkidle');
    await page.locator('.admin-card').filter({ hasText: /reports/i }).first().click();
    await expect(page).toHaveURL('/admin/reports');
  });

  test('should display all four tabs on reports page', async ({ page }) => {
    await page.goto('/admin/reports');
    await page.waitForLoadState('networkidle');

    await expect(page.getByTestId('tab-date')).toBeVisible();
    await expect(page.getByTestId('tab-movie')).toBeVisible();
    await expect(page.getByTestId('tab-showtime')).toBeVisible();
    await expect(page.getByTestId('tab-location')).toBeVisible();
  });

  test('should load Sales by Date tab by default', async ({ page }) => {
    await page.goto('/admin/reports');
    await page.waitForLoadState('networkidle');

    // The Date tab should be active by default
    const dateTab = page.getByTestId('tab-date');
    await expect(dateTab).toHaveClass(/active/);
  });

  test('should switch between tabs', async ({ page }) => {
    await page.goto('/admin/reports');
    await page.waitForLoadState('networkidle');

    // Switch to Movie tab
    await page.getByTestId('tab-movie').click();
    await expect(page.getByTestId('tab-movie')).toHaveClass(/active/);

    // Switch to Showtime tab
    await page.getByTestId('tab-showtime').click();
    await expect(page.getByTestId('tab-showtime')).toHaveClass(/active/);

    // Switch to Location tab
    await page.getByTestId('tab-location').click();
    await expect(page.getByTestId('tab-location')).toHaveClass(/active/);
  });

  test('should display date range controls on Sales by Date tab', async ({ page }) => {
    await page.goto('/admin/reports');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('input[type="date"]').first()).toBeVisible();
    await expect(page.locator('.granularity-selector')).toBeVisible();
    await expect(page.locator('.yoy-toggle')).toBeVisible();
  });

  test('should toggle Year-over-Year comparison', async ({ page }) => {
    await page.goto('/admin/reports');
    await page.waitForLoadState('networkidle');

    const yoyCheckbox = page.locator('.yoy-toggle input[type="checkbox"]');
    await expect(yoyCheckbox).not.toBeChecked();

    await yoyCheckbox.check();
    await expect(yoyCheckbox).toBeChecked();
  });

  test('should switch granularity options', async ({ page }) => {
    await page.goto('/admin/reports');
    await page.waitForLoadState('networkidle');

    const weeklyBtn = page.locator('.granularity-btn').filter({ hasText: /weekly/i });
    await weeklyBtn.click();
    await expect(weeklyBtn).toHaveClass(/active/);

    const monthlyBtn = page.locator('.granularity-btn').filter({ hasText: /monthly/i });
    await monthlyBtn.click();
    await expect(monthlyBtn).toHaveClass(/active/);
  });

  test('should show Export CSV button on Sales by Date tab', async ({ page }) => {
    await page.goto('/admin/reports');
    await page.waitForLoadState('networkidle');

    const exportBtn = page.locator('.export-btn');
    await expect(exportBtn).toBeVisible();
  });

  test('should redirect non-admin users away from reports', async ({ page }) => {
    // Logout first
    await page.evaluate(() => {
      localStorage.removeItem('authToken');
      localStorage.removeItem('authUser');
    });

    await page.goto('/admin/reports');
    // Should redirect to login or home
    await expect(page).not.toHaveURL('/admin/reports');
  });

  test('should apply date range preset filters', async ({ page }) => {
    await page.goto('/admin/reports');
    await page.waitForLoadState('networkidle');

    const presetBtn = page.locator('.preset-btn').filter({ hasText: /last 7 days/i });
    await expect(presetBtn).toBeVisible();
    await presetBtn.click();

    // After clicking, the preset should be active
    await expect(presetBtn).toHaveClass(/active/);
  });
});
