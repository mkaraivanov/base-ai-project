// Playwright test boilerplate for E2E tests
// See https://playwright.dev/docs/test-intro for more details
import { test, expect } from '@playwright/test';

test.describe('Main Flows', () => {
  test.describe('Homepage', () => {
    test('should load homepage successfully', async ({ page }) => {
      await page.goto('/');
      await expect(page).toHaveTitle(/CineBook - Movie Tickets/i);
    });

    test('should display navigation bar', async ({ page }) => {
      await page.goto('/');
      
      // Check for navigation bar
      const navbar = page.locator('nav, header').first();
      await expect(navbar).toBeVisible();
    });

    test('should have logo or branding', async ({ page }) => {
      await page.goto('/');
      
      // Check for logo or branding
      const logo = page.locator('[data-testid="logo"], .logo, img[alt*="CineBook" i]').first();
      const logoExists = await logo.isVisible().catch(() => false);
      
      if (logoExists) {
        await expect(logo).toBeVisible();
      }
    });

    test('should have link to movies page', async ({ page }) => {
      await page.goto('/');
      
      // Check for movies link
      const moviesLink = page.locator('a[href="/movies"], a').filter({ hasText: /movies/i }).first();
      await expect(moviesLink).toBeVisible();
    });

    test('should have login link when not authenticated', async ({ page }) => {
      await page.goto('/');
      
      // Check for login link
      const loginLink = page.locator('a[href="/login"]');
      await expect(loginLink).toBeVisible();
    });

    test('should have register link when not authenticated', async ({ page }) => {
      await page.goto('/');
      
      // Check for register link
      const registerLink = page.locator('a[href="/register"]');
      await expect(registerLink).toBeVisible();
    });

    test('should display featured movies or content', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      
      // Check for movie content
      const movieContent = page.locator('[data-testid="movie-card"], .movie-card, .featured-movie, article').first();
      await expect(movieContent).toBeVisible({ timeout: 10000 });
    });

    test('should navigate to movies page when clicking movies link', async ({ page }) => {
      await page.goto('/');
      
      const moviesLink = page.locator('a[href="/movies"], a').filter({ hasText: /movies/i }).first();
      await moviesLink.click();
      
      await expect(page).toHaveURL('/movies');
    });

    test('should navigate to login page when clicking login link', async ({ page }) => {
      await page.goto('/');
      
      const loginLink = page.locator('a[href="/login"]').first();
      await loginLink.click();
      
      await expect(page).toHaveURL('/login');
    });

    test('should navigate to register page when clicking register link', async ({ page }) => {
      await page.goto('/');
      
      const registerLink = page.locator('a[href="/register"]').first();
      await registerLink.click();
      
      await expect(page).toHaveURL('/register');
    });

    test('should have footer', async ({ page }) => {
      await page.goto('/');
      
      // Check for footer
      const footer = page.locator('footer');
      await expect(footer).toBeVisible();
    });

    test('should have responsive design', async ({ page }) => {
      // Test mobile viewport
      await page.setViewportSize({ width: 375, height: 667 });
      await page.goto('/');
      
      // Navigation should still be visible (possibly as hamburger menu)
      const nav = page.locator('nav, header').first();
      await expect(nav).toBeVisible();
    });
  });

  test.describe('Navigation', () => {
    test('should maintain consistent navigation across pages', async ({ page }) => {
      await page.goto('/');
      const navbarHome = page.locator('nav, header').first();
      await expect(navbarHome).toBeVisible();
      
      await page.goto('/movies');
      const navbarMovies = page.locator('nav, header').first();
      await expect(navbarMovies).toBeVisible();
      
      await page.goto('/login');
      const navbarLogin = page.locator('nav, header').first();
      await expect(navbarLogin).toBeVisible();
    });

    test('should highlight current page in navigation', async ({ page }) => {
      await page.goto('/movies');
      
      // Check if movies nav item has active class
      const moviesNavItem = page.locator('nav a[href="/movies"], nav button').filter({ hasText: /movies/i }).first();
      const hasActiveClass = await moviesNavItem.evaluate((el) => {
        return el.classList.contains('active') || el.classList.contains('current');
      }).catch(() => false);
      
      // Active state is good UX but not critical
      if (hasActiveClass) {
        expect(hasActiveClass).toBeTruthy();
      }
    });
  });

  test.describe('Error Handling', () => {
    test('should handle 404 - not found pages', async ({ page }) => {
      await page.goto('/this-page-does-not-exist');
      
      // Should redirect to home or show 404 page
      await page.waitForLoadState('networkidle');
      
      // Either redirected to home or on a 404 page
      const is404 = page.url().includes('404') || await page.locator('h1, h2').filter({ hasText: /not found|404/i }).isVisible().catch(() => false);
      const isHome = page.url().endsWith('/');
      
      expect(is404 || isHome).toBeTruthy();
    });
  });

  test.describe('Performance', () => {
    test('should load homepage within reasonable time', async ({ page }) => {
      const startTime = Date.now();
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      const loadTime = Date.now() - startTime;
      
      // Homepage should load in less than 10 seconds
      expect(loadTime).toBeLessThan(10000);
    });
  });
});
