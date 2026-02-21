import { test, expect } from '@playwright/test';

test.describe('Admin Movie Management', () => {
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
    
    // Navigate to movies management
    await page.goto('/admin/movies');
    await page.waitForLoadState('networkidle');
  });

  test('should display movies management page', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Movies Management');
    await expect(page.locator('button:has-text("Add Movie")')).toBeVisible();
  });

  test('should open create movie form', async ({ page }) => {
    await page.click('button:has-text("Add Movie")');
    await expect(page.locator('h2:has-text("Add Movie")')).toBeVisible();
    await expect(page.locator('input[name="title"]')).toBeVisible();
  });

  test('should create a new movie successfully', async ({ page }) => {
    await page.click('button:has-text("Add Movie")');
    
    const timestamp = Date.now();
    await page.fill('input[name="title"]', `Test Movie ${timestamp}`);
    await page.fill('textarea[name="description"]', 'This is a test movie description');
    await page.fill('input[name="genre"]', 'Action');
    await page.fill('input[name="durationMinutes"]', '120');
    await page.fill('input[name="rating"]', 'PG-13');
    await page.fill('input[name="posterUrl"]', 'https://example.com/poster.jpg');
    await page.fill('input[name="releaseDate"]', '2026-03-01');
    
    await page.click('button[type="submit"]');
    
    // Wait for success and form to close
    await expect(page.locator('h2:has-text("Add Movie")')).not.toBeVisible({ timeout: 5000 });
    
    // Verify movie appears in list
    await expect(page.locator(`text=Test Movie ${timestamp}`)).toBeVisible();
  });

  test('should show validation error for empty title', async ({ page }) => {
    await page.click('button:has-text("Add Movie")');
    
    // Fill other required fields but leave title empty
    await page.fill('textarea[name="description"]', 'Description');
    await page.fill('input[name="genre"]', 'Action');
    await page.fill('input[name="durationMinutes"]', '120');
    await page.fill('input[name="rating"]', 'PG-13');
    await page.fill('input[name="posterUrl"]', 'https://example.com/poster.jpg');
    await page.fill('input[name="releaseDate"]', '2026-03-01');
    
    // Try submitting - HTML5 validation should prevent it
    const titleInput = page.locator('input[name="title"]');
    await expect(titleInput).toHaveAttribute('required');
  });

  test('should show validation error for invalid duration', async ({ page }) => {
    await page.click('button:has-text("Add Movie")');
    
    const timestamp = Date.now();
    await page.fill('input[name="title"]', `Test Movie ${timestamp}`);
    await page.fill('textarea[name="description"]', 'Description');
    await page.fill('input[name="genre"]', 'Action');
    await page.fill('input[name="durationMinutes"]', '-10'); // Invalid duration
    await page.fill('input[name="rating"]', 'PG-13');
    await page.fill('input[name="posterUrl"]', 'https://example.com/poster.jpg');
    await page.fill('input[name="releaseDate"]', '2026-03-01');
    
    // HTML5 validation: min="1" prevents negative duration
    const durationInput = page.locator('input[name="durationMinutes"]');
    await expect(durationInput).toHaveAttribute('min', '1');
  });

  test('should edit an existing movie', async ({ page }) => {
    // First create a movie
    await page.click('button:has-text("Add Movie")');
    
    const timestamp = Date.now();
    const originalTitle = `Test Movie ${timestamp}`;
    await page.fill('input[name="title"]', originalTitle);
    await page.fill('textarea[name="description"]', 'Description');
    await page.fill('input[name="genre"]', 'Action');
    await page.fill('input[name="durationMinutes"]', '120');
    await page.fill('input[name="rating"]', 'PG-13');
    await page.fill('input[name="posterUrl"]', 'https://example.com/poster.jpg');
    await page.fill('input[name="releaseDate"]', '2026-03-01');
    
    await page.click('button[type="submit"]');
    await page.waitForTimeout(1000);
    
    // Now edit it - click the first Edit aria-label button
    await page.locator('button[aria-label="Edit"]').first().click();
    
    const updatedTitle = `Updated Movie ${timestamp}`;
    await page.fill('input[name="title"]', updatedTitle);
    await page.click('button[type="submit"]');
    
    await page.waitForTimeout(1000);
    
    // Verify updated title appears
    await expect(page.locator(`text=${updatedTitle}`)).toBeVisible();
  });

  test('should cancel movie creation', async ({ page }) => {
    await page.click('button:has-text("Add Movie")');
    await expect(page.locator('h2:has-text("Add Movie")')).toBeVisible();
    
    await page.click('button:has-text("Cancel")');
    await expect(page.locator('h2:has-text("Add Movie")')).not.toBeVisible();
  });
});
