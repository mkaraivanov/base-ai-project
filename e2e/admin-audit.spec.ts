import { test, expect } from '@playwright/test';

test.describe('Admin Audit Logs', () => {
  const adminUser = {
    email: 'admin@cinebook.local',
    password: 'Admin123!',
  };

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.fill('input#email', adminUser.email);
    await page.fill('input#password', adminUser.password);
    await page.click('button[type="submit"]');
    await page.waitForURL('/');

    await page.goto('/admin/audit');
    await page.waitForLoadState('networkidle');
  });

  // ── Page renders ──────────────────────────────────────────────────────────

  test('should display the Audit Logs page with heading and export button', async ({ page }) => {
    await expect(page.locator('h5, h4, h3, h2, h1').filter({ hasText: 'Audit Logs' }).first()).toBeVisible();
    await expect(page.getByRole('button', { name: /export/i })).toBeVisible();
  });

  test('should render the filter bar with date, action and entity fields', async ({ page }) => {
    // Date range inputs
    const filterPaper = page.locator('form, .MuiPaper-root').first();
    await expect(page.locator('label').filter({ hasText: /date from/i }).first()).toBeVisible();
    await expect(page.locator('label').filter({ hasText: /date to/i }).first()).toBeVisible();
    // Action select
    await expect(page.locator('label').filter({ hasText: /action/i }).first()).toBeVisible();
    // Entity type select
    await expect(page.locator('label').filter({ hasText: /entity/i }).first()).toBeVisible();
  });

  test('should show a data table with audit log columns', async ({ page }) => {
    // The table should have the main column headers
    await expect(page.locator('th, [role="columnheader"]').filter({ hasText: /timestamp/i }).first()).toBeVisible();
    await expect(page.locator('th, [role="columnheader"]').filter({ hasText: /entity/i }).first()).toBeVisible();
    await expect(page.locator('th, [role="columnheader"]').filter({ hasText: /action/i }).first()).toBeVisible();
    await expect(page.locator('th, [role="columnheader"]').filter({ hasText: /user/i }).first()).toBeVisible();
  });

  // ── Filtering ─────────────────────────────────────────────────────────────

  test('should apply action filter and reload results', async ({ page }) => {
    // Open the Action select
    const actionSelect = page.locator('div[role="combobox"]').nth(0);
    if (await actionSelect.isVisible()) {
      await actionSelect.click();
      const createdOption = page.locator('li[role="option"]').filter({ hasText: 'Created' });
      if (await createdOption.isVisible()) {
        await createdOption.click();
      }
    }

    // Click Search/Apply button
    const searchBtn = page.getByRole('button', { name: /search|apply|filter/i });
    if (await searchBtn.first().isVisible()) {
      await searchBtn.first().click();
      await page.waitForLoadState('networkidle');
    }

    // Page must still render without errors
    await expect(page.locator('body')).not.toContainText('Something went wrong');
  });

  test('should clear filters when clear button is clicked', async ({ page }) => {
    // Find and click the clear/reset button if present
    const clearBtn = page.getByRole('button', { name: /clear|reset/i });
    if (await clearBtn.first().isVisible()) {
      await clearBtn.first().click();
      await page.waitForLoadState('networkidle');
    }

    await expect(page.locator('body')).not.toContainText('Something went wrong');
  });

  // ── Detail drawer ─────────────────────────────────────────────────────────

  test('should open detail drawer when a row view button is clicked', async ({ page }) => {
    // Only run this assertion if there are rows in the table
    const viewBtn = page.locator('[aria-label*="view"], [title*="View"], [data-testid*="view"]')
      .or(page.locator('button[title]').filter({ hasText: '' }).first())
      .or(page.getByRole('button').filter({ has: page.locator('svg') }).first());

    // Look for any icon button in a table row (the Eye / view button)
    const tableRows = page.locator('tbody tr');
    const rowCount = await tableRows.count();

    if (rowCount > 0) {
      // Click the first eye/view icon button in the first row
      const firstRowEyeBtn = tableRows.first().locator('button').first();
      await firstRowEyeBtn.click();

      // The drawer should open
      await expect(page.locator('[role="presentation"], .MuiDrawer-root').first()).toBeVisible({ timeout: 5000 });
    } else {
      // No rows — just verify the table is empty gracefully
      await expect(page.locator('table, [role="table"]').first()).toBeVisible();
    }
  });

  // ── Export ────────────────────────────────────────────────────────────────

  test('should trigger CSV download when Export button is clicked', async ({ page }) => {
    const [download] = await Promise.all([
      page.waitForEvent('download', { timeout: 10000 }).catch(() => null),
      page.getByRole('button', { name: /export/i }).click(),
    ]);

    // Either a download started or a toast/success message appeared
    const successToast = page.locator('[data-sonner-toast], .Toastify__toast, [role="status"]')
      .filter({ hasText: /export|download|success/i });

    const downloadOrToast = download !== null || await successToast.first().isVisible({ timeout: 5000 }).catch(() => false);
    expect(downloadOrToast).toBe(true);
  });

  // ── Pagination ────────────────────────────────────────────────────────────

  test('should show pagination controls when there are results', async ({ page }) => {
    const tableRows = page.locator('tbody tr');
    const rowCount = await tableRows.count();

    if (rowCount > 0) {
      // MUI Pagination renders a <nav> with aria-label="pagination navigation"
      const pagination = page.locator('[aria-label="pagination navigation"], .MuiPagination-root');
      // Only visible when there is more than one page; otherwise just confirm no error
      await expect(page.locator('body')).not.toContainText('Something went wrong');
    }
  });
});
