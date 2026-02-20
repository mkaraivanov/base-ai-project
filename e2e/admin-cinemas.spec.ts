import { test, expect } from '@playwright/test';

test.describe('Admin Cinemas Management', () => {
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

    await page.goto('/admin/cinemas');
    await page.waitForLoadState('networkidle');
  });

  // ──────────────────────────────────────────────
  // Page structure
  // ──────────────────────────────────────────────

  test('should display cinemas management page', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Cinemas Management');
    await expect(page.locator('button:has-text("Add Cinema")')).toBeVisible();
  });

  test('should display cinemas table with expected columns', async ({ page }) => {
    const headers = page.locator('table thead th');
    await expect(headers).toContainText(['Cinema', 'Location', 'Hours', 'Halls', 'Status', 'Actions']);
  });

  test('should show a cinema card link on the admin dashboard', async ({ page }) => {
    await page.goto('/admin');
    await page.waitForLoadState('networkidle');
    const card = page.locator('a[href="/admin/cinemas"]');
    await expect(card).toBeVisible();
  });

  // ──────────────────────────────────────────────
  // Create cinema
  // ──────────────────────────────────────────────

  test('should open create cinema form', async ({ page }) => {
    await page.click('button:has-text("Add Cinema")');
    await expect(page.locator('h2:has-text("Add Cinema")')).toBeVisible();
    await expect(page.locator('input[name="name"]')).toBeVisible();
    await expect(page.locator('input[name="address"]')).toBeVisible();
    await expect(page.locator('input[name="city"]')).toBeVisible();
    await expect(page.locator('input[name="country"]')).toBeVisible();
    await expect(page.locator('input[name="openTime"]')).toBeVisible();
    await expect(page.locator('input[name="closeTime"]')).toBeVisible();
  });

  test('should require mandatory fields', async ({ page }) => {
    await page.click('button:has-text("Add Cinema")');

    await expect(page.locator('input[name="name"]')).toHaveAttribute('required');
    await expect(page.locator('input[name="address"]')).toHaveAttribute('required');
    await expect(page.locator('input[name="city"]')).toHaveAttribute('required');
    await expect(page.locator('input[name="country"]')).toHaveAttribute('required');
    await expect(page.locator('input[name="openTime"]')).toHaveAttribute('required');
    await expect(page.locator('input[name="closeTime"]')).toHaveAttribute('required');
  });

  test('should create a new cinema successfully', async ({ page }) => {
    await page.click('button:has-text("Add Cinema")');

    const timestamp = Date.now();
    const cinemaName = `Test Cinema ${timestamp}`;

    await page.fill('input[name="name"]', cinemaName);
    await page.fill('input[name="address"]', '123 Main Street');
    await page.fill('input[name="city"]', 'Sofia');
    await page.fill('input[name="country"]', 'Bulgaria');
    await page.fill('input[name="openTime"]', '09:00');
    await page.fill('input[name="closeTime"]', '23:00');

    await page.click('button[type="submit"]');

    // Modal should close
    await expect(page.locator('h2:has-text("Add Cinema")')).not.toBeVisible({ timeout: 10000 });

    // New cinema should appear in the table
    await expect(page.locator(`td:has-text("${cinemaName}")`)).toBeVisible({ timeout: 10000 });
  });

  test('should pre-fill opening and closing time defaults', async ({ page }) => {
    await page.click('button:has-text("Add Cinema")');
    await expect(page.locator('input[name="openTime"]')).toHaveValue('09:00');
    await expect(page.locator('input[name="closeTime"]')).toHaveValue('23:00');
  });

  test('should cancel cinema creation', async ({ page }) => {
    await page.click('button:has-text("Add Cinema")');
    await expect(page.locator('h2:has-text("Add Cinema")')).toBeVisible();

    await page.click('button:has-text("Cancel")');
    await expect(page.locator('h2:has-text("Add Cinema")')).not.toBeVisible();
  });

  test('should close modal when clicking Cancel', async ({ page }) => {
    await page.click('button:has-text("Add Cinema")');
    await expect(page.locator('h2:has-text("Add Cinema")')).toBeVisible();

    // Close by clicking Cancel
    await page.click('button:has-text("Cancel")');
    await expect(page.locator('h2:has-text("Add Cinema")')).not.toBeVisible();
  });

  // ──────────────────────────────────────────────
  // Edit cinema
  // ──────────────────────────────────────────────

  test('should open edit form with pre-filled values', async ({ page }) => {
    // Create a cinema first so we can edit it
    await page.click('button:has-text("Add Cinema")');
    const timestamp = Date.now();
    const cinemaName = `Edit Target ${timestamp}`;
    await page.fill('input[name="name"]', cinemaName);
    await page.fill('input[name="address"]', '1 Edit Ave');
    await page.fill('input[name="city"]', 'Plovdiv');
    await page.fill('input[name="country"]', 'Bulgaria');
    await page.click('button[type="submit"]');
    await expect(page.locator(`td:has-text("${cinemaName}")`)).toBeVisible({ timeout: 10000 });

    // Click Edit
    const row = page.locator('tr', { hasText: cinemaName });
    await row.locator('button[aria-label="Edit"]').click();

    await expect(page.locator('h2:has-text("Edit Cinema")')).toBeVisible();
    await expect(page.locator('input[name="name"]')).toHaveValue(cinemaName);
    await expect(page.locator('input[name="city"]')).toHaveValue('Plovdiv');
  });

  test('should update a cinema successfully', async ({ page }) => {
    // Create
    await page.click('button:has-text("Add Cinema")');
    const timestamp = Date.now();
    const originalName = `Update Me ${timestamp}`;
    await page.fill('input[name="name"]', originalName);
    await page.fill('input[name="address"]', '99 Old Road');
    await page.fill('input[name="city"]', 'Varna');
    await page.fill('input[name="country"]', 'Bulgaria');
    await page.click('button[type="submit"]');
    await expect(page.locator(`td:has-text("${originalName}")`)).toBeVisible({ timeout: 10000 });

    // Edit
    const row = page.locator('tr', { hasText: originalName });
    await row.locator('button[aria-label="Edit"]').click();

    const updatedName = `Updated ${timestamp}`;
    await page.fill('input[name="name"]', updatedName);
    await page.click('button[type="submit"]');

    await expect(page.locator('h2:has-text("Edit Cinema")')).not.toBeVisible({ timeout: 10000 });
    await expect(page.locator(`td:has-text("${updatedName}")`)).toBeVisible({ timeout: 10000 });
  });

  test('should show Active checkbox only when editing', async ({ page }) => {
    // Create form — no Active checkbox
    await page.click('button:has-text("Add Cinema")');
    await expect(page.locator('input[name="isActive"]')).not.toBeVisible();
    await page.click('button:has-text("Cancel")');

    // Create a cinema so we can edit
    await page.click('button:has-text("Add Cinema")');
    const timestamp = Date.now();
    const cinemaName = `Active Toggle ${timestamp}`;
    await page.fill('input[name="name"]', cinemaName);
    await page.fill('input[name="address"]', '5 Toggle Rd');
    await page.fill('input[name="city"]', 'Burgas');
    await page.fill('input[name="country"]', 'Bulgaria');
    await page.click('button[type="submit"]');
    await expect(page.locator(`td:has-text("${cinemaName}")`)).toBeVisible({ timeout: 10000 });

    // Edit form — Active checkbox visible
    const row = page.locator('tr', { hasText: cinemaName });
    await row.locator('button[aria-label="Edit"]').click();
    await expect(page.locator('input[name="isActive"]')).toBeVisible();
    await page.click('button:has-text("Cancel")');
  });

  // ──────────────────────────────────────────────
  // Status badge
  // ──────────────────────────────────────────────

  test('should display Active status badge for new cinemas', async ({ page }) => {
    await page.click('button:has-text("Add Cinema")');
    const timestamp = Date.now();
    const cinemaName = `Status Test ${timestamp}`;
    await page.fill('input[name="name"]', cinemaName);
    await page.fill('input[name="address"]', '7 Status Lane');
    await page.fill('input[name="city"]', 'Stara Zagora');
    await page.fill('input[name="country"]', 'Bulgaria');
    await page.click('button[type="submit"]');
    await expect(page.locator(`td:has-text("${cinemaName}")`)).toBeVisible({ timeout: 10000 });

    const row = page.locator('tr', { hasText: cinemaName });
    // Status is rendered as a MUI Chip — find the chip within the row containing 'Active'
    const statusChip = row.locator('.MuiChip-label').filter({ hasText: /active/i });
    await expect(statusChip).toBeVisible({ timeout: 5000 });
  });
});
