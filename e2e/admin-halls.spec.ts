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
    await expect(page.locator('h2:has-text("Add Hall")')).toBeVisible();
    
    // Select first cinema from the MUI combobox inside the form (not the filter)
    // The form cinema combobox has label "Cinema *"
    const modalForm = page.locator('.modal');
    const cinemaCombobox = modalForm.locator('[role="combobox"]').first();
    await cinemaCombobox.click();
    await page.getByRole('option').first().click();
    
    const timestamp = Date.now();
    await page.fill('input[name="name"]', `Hall ${timestamp}`);
    await page.fill('input[name="rows"]', '10');
    await page.fill('input[name="seatsPerRow"]', '12');
    
    await page.click('button[type="submit"]');
    
    // Wait for success and form to close
    await expect(page.locator('h2:has-text("Add Hall")')).not.toBeVisible({ timeout: 10000 });
    
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
    const timestamp = Date.now();
    // First create a hall
    await page.click('button:has-text("Add Hall")');
    await expect(page.locator('h2:has-text("Add Hall")')).toBeVisible();
    
    // Select first available cinema from the MUI combobox inside the form (not the filter dropdown)
    const modalForm = page.locator('.modal');
    const cinemaCombobox = modalForm.locator('[role="combobox"]').first();
    await cinemaCombobox.click();
    await page.getByRole('option').first().click();
    
    const originalName = `Hall ${timestamp}`;
    await page.fill('input[name="name"]', originalName);
    await page.fill('input[name="rows"]', '8');
    await page.fill('input[name="seatsPerRow"]', '10');
    
    await page.locator('.modal button[type="submit"]').click();
    await page.waitForTimeout(1000);

    // Now edit it - find the table row containing the hall name and click its Edit button
    // Wait for the modal to close first
    await expect(page.locator('h2:has-text("Add Hall")')).not.toBeVisible({ timeout: 5000 });
    const hallRow = page.locator('table tbody tr').filter({ hasText: originalName }).first();
    await hallRow.waitFor({ state: 'visible', timeout: 8000 });
    await hallRow.locator('button[aria-label="Edit"]').click();

    // Clear and fill the name field - use triple-click then type to work with React controlled inputs
    const nameInput = page.locator('input#name');
    await nameInput.waitFor({ state: 'visible', timeout: 5000 });
    await nameInput.click({ clickCount: 3 });
    const updatedName = `Updated Hall ${timestamp}`;
    await nameInput.fill(updatedName);
    // Click the Update submit button scoped to the modal
    await page.locator('.modal button[type="submit"]').click();

    // Wait for modal to close and table to refresh
    await expect(page.locator('h2:has-text("Edit Hall")')).not.toBeVisible({ timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Verify updated name appears in the table
    await expect(page.locator('table tbody').getByText(updatedName)).toBeVisible({ timeout: 8000 });
  });

  test('should cancel hall creation', async ({ page }) => {
    await page.click('button:has-text("Add Hall")');
    await expect(page.locator('h2:has-text("Add Hall")')).toBeVisible();
    
    await page.click('button:has-text("Cancel")');
    await expect(page.locator('h2:has-text("Add Hall")')).not.toBeVisible();
  });
});
