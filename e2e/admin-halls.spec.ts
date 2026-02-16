import { test, expect } from '@playwright/test';

test.describe('Admin Cinema Halls Management', () => {
  const adminUser = {
    email: 'admin@cinebook.local',
    password: 'Admin123!',
  };

  test.beforeEach(async ({ page }) => {
    // Login as admin
    await page.goto('/login');
    await page.fill('input#email', adminUser.email);
    await page.fill('input#password', adminUser.password);
    await page.click('button[type="submit"]');
    
    // Wait for navigation to complete
    await page.waitForURL('/');
    
    // Navigate to halls management
    await page.goto('/admin/halls');
    await page.waitForLoadState('networkidle');
  });

  test('should display halls management page', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Cinema Halls Management');
    await expect(page.locator('button:has-text("Add Hall")')).toBeVisible();
  });

  test('should open create hall form', async ({ page }) => {
    await page.click('button:has-text("Add Hall")');
    await expect(page.locator('h2:has-text("Add Hall")')).toBeVisible();
    await expect(page.locator('input[name="name"]')).toBeVisible();
  });

  test('should create a new hall successfully', async ({ page }) => {
    await page.click('button:has-text("Add Hall")');
    
    const timestamp = Date.now();
    await page.fill('input[name="name"]', `Hall ${timestamp}`);
    await page.fill('input[name="rows"]', '10');
    await page.fill('input[name="seatsPerRow"]', '12');
    
    await page.click('button[type="submit"]');
    
    // Wait for success and form to close
    await expect(page.locator('h2:has-text("Add Hall")')).not.toBeVisible({ timeout: 5000 });
    
    // Verify hall appears in list
    await expect(page.locator(`text=Hall ${timestamp}`)).toBeVisible();
  });

  test('should show validation error for empty name', async ({ page }) => {
    await page.click('button:has-text("Add Hall")');
    
    // Fill other fields but leave name empty
    await page.fill('input[name="rows"]', '8');
    await page.fill('input[name="seatsPerRow"]', '10');
    
    // Try submitting - HTML5 validation should prevent it
    const nameInput = page.locator('input[name="name"]');
    await expect(nameInput).toHaveAttribute('required');
  });

  test('should show validation error for invalid rows', async ({ page }) => {
    await page.click('button:has-text("Add Hall")');
    
    const timestamp = Date.now();
    await page.fill('input[name="name"]', `Hall ${timestamp}`);
    // HTML5 validation should prevent negative/zero values
    const rowsInput = page.locator('input[name="rows"]');
    await expect(rowsInput).toHaveAttribute('min', '1');
    await expect(rowsInput).toHaveAttribute('type', 'number');
  });

  test('should edit an existing hall', async ({ page }) => {
    // First create a hall
    await page.click('button:has-text("Add Hall")');
    
    const timestamp = Date.now();
    const originalName = `Hall ${timestamp}`;
    await page.fill('input[name="name"]', originalName);
    await page.fill('input[name="rows"]', '8');
    await page.fill('input[name="seatsPerRow"]', '10');
    
    await page.click('button[type="submit"]');
    await page.waitForTimeout(1000);
    
    // Now edit it
    await page.click(`text=${originalName} >> .. >> button:has-text("Edit")`).catch(() => {
      page.click('button:has-text("Edit")').first();
    });
    
    const updatedName = `Updated Hall ${timestamp}`;
    await page.fill('input[name="name"]', updatedName);
    await page.click('button[type="submit"]');
    
    await page.waitForTimeout(1000);
    
    // Verify updated name appears
    await expect(page.locator(`text=${updatedName}`)).toBeVisible();
  });

  test('should cancel hall creation', async ({ page }) => {
    await page.click('button:has-text("Add Hall")');
    await expect(page.locator('h2:has-text("Add Hall")')).toBeVisible();
    
    await page.click('button:has-text("Cancel")');
    await expect(page.locator('h2:has-text("Add Hall")')).not.toBeVisible();
  });
});
