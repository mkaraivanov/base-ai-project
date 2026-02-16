import { test, expect } from '@playwright/test';

test.describe('Admin Showtimes Management', () => {
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
    
    // Navigate to showtimes management
    await page.goto('/admin/showtimes');
    await page.waitForLoadState('networkidle');
  });

  test('should display showtimes management page', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Showtimes Management');
    await expect(page.locator('button:has-text("Add Showtime")')).toBeVisible();
  });

  test('should open create showtime form', async ({ page }) => {
    await page.click('button:has-text("Add Showtime")');
    await expect(page.locator('h2:has-text("Schedule Showtime")')).toBeVisible();
    await expect(page.locator('select[name="movieId"]')).toBeVisible();
  });

  test('should show validation error for missing required fields', async ({ page }) => {
    await page.click('button:has-text("Add Showtime")');
    
    // Verify required fields exist
    const movieSelect = page.locator('select[name="movieId"]');
    const hallSelect = page.locator('select[name="cinemaHallId"]');
    const timeInput = page.locator('input[name="startTime"]');
    const priceInput = page.locator('input[name="basePrice"]');
    
    await expect(movieSelect).toHaveAttribute('required');
    await expect(hallSelect).toHaveAttribute('required');
    await expect(timeInput).toHaveAttribute('required');
    await expect(priceInput).toHaveAttribute('required');
  });

  test('should show validation error for invalid price', async ({ page }) => {
    await page.click('button:has-text("Add Showtime")');
    
    // HTML5 validation should prevent negative prices
    const priceInput = page.locator('input[name="basePrice"]');
    await expect(priceInput).toHaveAttribute('type', 'number');
    await expect(priceInput).toHaveAttribute('min', '0');
  });

  test('should cancel showtime creation', async ({ page }) => {
    await page.click('button:has-text("Add Showtime")');
    await expect(page.locator('h2:has-text("Schedule Showtime")')).toBeVisible();
    
    await page.click('button:has-text("Cancel")');
    await expect(page.locator('h2:has-text("Schedule Showtime")')).not.toBeVisible();
  });
});
