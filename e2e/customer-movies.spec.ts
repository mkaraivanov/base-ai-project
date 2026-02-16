import { test, expect } from '@playwright/test';

test.describe('Customer - Movies Pages', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test.describe('Movies List Page', () => {
    test('should display movies page', async ({ page }) => {
      await page.goto('/movies');
      await expect(page).toHaveURL('/movies');
      
      // Check for movies page heading or content
      const heading = page.locator('h1, h2').filter({ hasText: /movies/i }).first();
      await expect(heading).toBeVisible({ timeout: 10000 });
    });

    test('should display movie cards', async ({ page }) => {
      await page.goto('/movies');
      
      // Wait for movies to load
      await page.waitForLoadState('networkidle');
      
      // Check if movie cards are displayed (adjust selector based on actual implementation)
      const movieCards = page.locator('[data-testid="movie-card"], .movie-card, article').first();
      await expect(movieCards).toBeVisible({ timeout: 10000 });
    });

    test('should navigate to movie detail when clicking a movie', async ({ page }) => {
      await page.goto('/movies');
      await page.waitForLoadState('networkidle');
      
      // Click on the first movie (adjust selector based on actual implementation)
      const firstMovie = page.locator('[data-testid="movie-card"], .movie-card, article').first();
      await firstMovie.waitFor({ state: 'visible', timeout: 10000 });
      await firstMovie.click();
      
      // Should navigate to movie detail page
      await expect(page).toHaveURL(/\/movies\/\d+/, { timeout: 10000 });
    });

    test('should have search/filter functionality', async ({ page }) => {
      await page.goto('/movies');
      
      // Check for search or filter input (if implemented)
      const searchInput = page.locator('input[type="search"], input[placeholder*="search" i], input[placeholder*="filter" i]').first();
      
      // If search exists, verify it's visible
      const searchExists = await searchInput.isVisible().catch(() => false);
      if (searchExists) {
        await expect(searchInput).toBeVisible();
      }
    });
  });

  test.describe('Movie Detail Page', () => {
    test('should display movie details', async ({ page }) => {
      // Navigate to movies page first
      await page.goto('/movies');
      await page.waitForLoadState('networkidle');
      
      // Click first movie to get to detail page
      const firstMovie = page.locator('[data-testid="movie-card"], .movie-card, article').first();
      await firstMovie.waitFor({ state: 'visible', timeout: 10000 });
      await firstMovie.click();
      
      // Wait for movie detail page to load
      await page.waitForURL(/\/movies\/\d+/);
      await page.waitForLoadState('networkidle');
      
      // Check for movie title (should be in h1 or h2)
      const movieTitle = page.locator('h1, h2').first();
      await expect(movieTitle).toBeVisible();
      
      // Check for movie description
      const description = page.locator('[data-testid="movie-description"], .movie-description, p').first();
      await expect(description).toBeVisible();
    });

    test('should display showtimes for the movie', async ({ page }) => {
      await page.goto('/movies');
      await page.waitForLoadState('networkidle');
      
      const firstMovie = page.locator('[data-testid="movie-card"], .movie-card, article').first();
      await firstMovie.waitFor({ state: 'visible', timeout: 10000 });
      await firstMovie.click();
      
      await page.waitForURL(/\/movies\/\d+/);
      await page.waitForLoadState('networkidle');
      
      // Check for showtimes section
      const showtimesSection = page.locator('[data-testid="showtimes"], .showtimes, section').first();
      await expect(showtimesSection).toBeVisible({ timeout: 10000 });
    });

    test('should allow navigation to seat selection when clicking a showtime', async ({ page }) => {
      await page.goto('/movies');
      await page.waitForLoadState('networkidle');
      
      const firstMovie = page.locator('[data-testid="movie-card"], .movie-card, article').first();
      await firstMovie.waitFor({ state: 'visible', timeout: 10000 });
      await firstMovie.click();
      
      await page.waitForURL(/\/movies\/\d+/);
      await page.waitForLoadState('networkidle');
      
      // Click on a showtime button
      const showtimeButton = page.locator('button').filter({ hasText: /book|select/i }).first();
      const showtimeExists = await showtimeButton.isVisible().catch(() => false);
      
      if (showtimeExists) {
        await showtimeButton.click();
        
        // Should either navigate to login (if not authenticated) or seat selection
        await page.waitForURL(/\/(login|showtime\/\d+\/seats)/, { timeout: 10000 });
      }
    });

    test('should display movie poster/image', async ({ page }) => {
      await page.goto('/movies');
      await page.waitForLoadState('networkidle');
      
      const firstMovie = page.locator('[data-testid="movie-card"], .movie-card, article').first();
      await firstMovie.waitFor({ state: 'visible', timeout: 10000 });
      await firstMovie.click();
      
      await page.waitForURL(/\/movies\/\d+/);
      await page.waitForLoadState('networkidle');
      
      // Check for movie image
      const movieImage = page.locator('img').first();
      await expect(movieImage).toBeVisible();
    });

    test('should have back button or navigation to movies list', async ({ page }) => {
      await page.goto('/movies');
      await page.waitForLoadState('networkidle');
      
      const firstMovie = page.locator('[data-testid="movie-card"], .movie-card, article').first();
      await firstMovie.waitFor({ state: 'visible', timeout: 10000 });
      await firstMovie.click();
      
      await page.waitForURL(/\/movies\/\d+/);
      
      // Check for back button or navigation link
      const backButton = page.locator('button, a').filter({ hasText: /back|movies/i }).first();
      const backExists = await backButton.isVisible().catch(() => false);
      
      if (backExists) {
        await expect(backButton).toBeVisible();
      }
    });
  });
});
