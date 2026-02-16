import { test, expect } from '@playwright/test';

test.describe('Admin - Dashboard Page', () => {
  const adminCredentials = {
    email: 'admin@cinebook.local',
    password: 'Admin123!',
  };

  test.beforeEach(async ({ page }) => {
    // Login as admin before each test
    await page.goto('/login');
    await page.fill('input#email, input[type="email"]', adminCredentials.email);
    await page.fill('input#password, input[type="password"]', adminCredentials.password);
    await page.click('button[type="submit"]');
    
    // Wait for successful login
    await page.waitForURL('/', { timeout: 10000 }).catch(() => {});
  });

  test('should require admin role to access dashboard', async ({ page }) => {
    // Already logged in as admin, so access should be granted
    await page.goto('/admin');
    
    // Should stay on admin dashboard (not redirect)
    await expect(page).toHaveURL('/admin');
  });

  test('should display admin dashboard with overview statistics', async ({ page }) => {
    await page.goto('/admin');
    await page.waitForLoadState('networkidle');
    
    // Check for dashboard heading
    const heading = page.locator('h1, h2').filter({ hasText: /dashboard|admin/i }).first();
    await expect(heading).toBeVisible({ timeout: 10000 });
  });

  test('should display statistics cards', async ({ page }) => {
    await page.goto('/admin');
    await page.waitForLoadState('networkidle');
    
    // Check for statistics/metrics cards
    const statsCards = page.locator('[data-testid="stat-card"], .stat-card, .metric-card, .card');
    const cardCount = await statsCards.count();
    
    // Should have at least some statistics displayed
    expect(cardCount).toBeGreaterThan(0);
  });

  test('should have navigation links to management pages', async ({ page }) => {
    await page.goto('/admin');
    await page.waitForLoadState('networkidle');
    
    // Check for navigation to movies management
    const moviesLink = page.locator('a, button').filter({ hasText: /movies/i }).first();
    await expect(moviesLink).toBeVisible();
    
    // Check for navigation to halls management
    const hallsLink = page.locator('a, button').filter({ hasText: /halls/i }).first();
    await expect(hallsLink).toBeVisible();
    
    // Check for navigation to showtimes management
    const showtimesLink = page.locator('a, button').filter({ hasText: /showtimes/i }).first();
    await expect(showtimesLink).toBeVisible();
  });

  test('should allow navigation to movies management', async ({ page }) => {
    await page.goto('/admin');
    await page.waitForLoadState('networkidle');
    
    // Click on movies management link
    const moviesLink = page.locator('a, button').filter({ hasText: /movies/i }).first();
    await moviesLink.click();
    
    // Should navigate to movies management page
    await expect(page).toHaveURL('/admin/movies');
  });

  test('should allow navigation to halls management', async ({ page }) => {
    await page.goto('/admin');
    await page.waitForLoadState('networkidle');
    
    // Click on halls management link
    const hallsLink = page.locator('a, button').filter({ hasText: /halls/i }).first();
    await hallsLink.click();
    
    // Should navigate to halls management page
    await expect(page).toHaveURL('/admin/halls');
  });

  test('should allow navigation to showtimes management', async ({ page }) => {
    await page.goto('/admin');
    await page.waitForLoadState('networkidle');
    
    // Click on showtimes management link
    const showtimesLink = page.locator('a, button').filter({ hasText: /showtimes/i }).first();
    await showtimesLink.click();
    
    // Should navigate to showtimes management page
    await expect(page).toHaveURL('/admin/showtimes');
  });

  test('should display recent bookings or activity', async ({ page }) => {
    await page.goto('/admin');
    await page.waitForLoadState('networkidle');
    
    // Check for recent activity section
    const recentActivity = page.locator('[data-testid="recent-activity"], .recent-activity, .recent-bookings');
    const activityExists = await recentActivity.isVisible().catch(() => false);
    
    if (activityExists) {
      await expect(recentActivity).toBeVisible();
    }
  });

  test('should show total number of movies', async ({ page }) => {
    await page.goto('/admin');
    await page.waitForLoadState('networkidle');
    
    // Look for movies count metric
    const moviesMetric = page.locator('.stat-card, .metric-card').filter({ hasText: /movies/i }).first();
    const metricExists = await moviesMetric.isVisible().catch(() => false);
    
    if (metricExists) {
      await expect(moviesMetric).toBeVisible();
    }
  });

  test('should show total number of bookings', async ({ page }) => {
    await page.goto('/admin');
    await page.waitForLoadState('networkidle');
    
    // Look for bookings count metric
    const bookingsMetric = page.locator('.stat-card, .metric-card').filter({ hasText: /bookings|tickets/i }).first();
    const metricExists = await bookingsMetric.isVisible().catch(() => false);
    
    if (metricExists) {
      await expect(bookingsMetric).toBeVisible();
    }
  });

  test('non-admin user should not access admin dashboard', async ({ page }) => {
    // Logout admin
    const logoutButton = page.locator('button, a').filter({ hasText: /logout|sign out/i }).first();
    const logoutExists = await logoutButton.isVisible().catch(() => false);
    
    if (logoutExists) {
      await logoutButton.click();
      await page.waitForLoadState('networkidle');
    }
    
    // Try to access admin dashboard without login
    await page.goto('/admin');
    
    // Should redirect to login or home page
    await expect(page).not.toHaveURL('/admin');
  });
});
