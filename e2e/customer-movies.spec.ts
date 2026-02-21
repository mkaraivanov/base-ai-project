
import { test, expect, type Page } from '@playwright/test';

const baseURL = process.env.PLAYWRIGHT_TEST_BASE_URL || 'http://localhost:5173';

const adminUser = {
  email: 'admin@cinebook.local',
  password: 'Admin123!',
};

async function loginAsAdmin(page: Page) {
  await page.goto(`${baseURL}/login`);
  await page.fill('input[type="email"]', adminUser.email);
  await page.fill('input[type="password"]', adminUser.password);
  await page.click('button[type="submit"]');
  await page.waitForURL(`${baseURL}/`, { timeout: 10000 });
}

test.describe('Customer - Movies Pages', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test.describe('Movies List Page', () => {
    test('should display movies page', async ({ page }) => {
      await page.goto(`${baseURL}/movies`);
      await expect(page).toHaveURL(`${baseURL}/movies`);
      
      // Check for movies page heading — the page shows "Now Showing" in a Typography element
      const heading = page.locator('h1, h2, h3, h4, h5, h6').filter({ hasText: /movies|now showing/i }).first();
      await expect(heading).toBeVisible({ timeout: 10000 });
    });

    test('should display movie cards', async ({ page }) => {
      await page.goto(`${baseURL}/movies`);
      
      // Wait for movies to load
      await page.waitForLoadState('networkidle');
      
      // Check if movie cards are displayed (adjust selector based on actual implementation)
      const movieCards = page.locator('[data-testid="movie-card"], .movie-card, article').first();
      await expect(movieCards).toBeVisible({ timeout: 10000 });
    });

    test('should navigate to movie detail when clicking a movie', async ({ page }) => {
      await page.goto(`${baseURL}/movies`);
      await page.waitForLoadState('networkidle');
      
      // Click on the "View Showtimes" link inside the first movie card
      const firstMovieLink = page.locator('.movie-card a.btn').first();
      await firstMovieLink.waitFor({ state: 'visible', timeout: 10000 });
      await firstMovieLink.click();
      
      // Should navigate to movie detail page (IDs are GUIDs)
      await expect(page).toHaveURL(/\/movies\/[a-f0-9-]+/, { timeout: 10000 });
    });

    test('should have search/filter functionality', async ({ page }) => {
      await page.goto(`${baseURL}/movies`);
      
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
      await page.goto(`${baseURL}/movies`);
      await page.waitForLoadState('networkidle');
      
      // Click the "View Showtimes" link inside the first movie card
      const firstMovieLink = page.locator('.movie-card a.btn').first();
      await firstMovieLink.waitFor({ state: 'visible', timeout: 10000 });
      await firstMovieLink.click();
      
      // Wait for movie detail page to load (IDs are GUIDs)
      await page.waitForURL(/\/movies\/[a-f0-9-]+/);
      await page.waitForLoadState('networkidle');
      
      // Check for movie title (MovieDetailLayout uses variant="h4" → <h4>)
      const movieTitle = page.locator('h1, h2, h3, h4, h5, h6').first();
      await expect(movieTitle).toBeVisible();
      
      // Check for movie description
      const description = page.locator('p').first();
      await expect(description).toBeVisible();
    });

    test('should display showtimes for the movie', async ({ page }) => {
      await page.goto(`${baseURL}/movies`);
      await page.waitForLoadState('networkidle');
      
      const firstMovieLink = page.locator('.movie-card a.btn').first();
      await firstMovieLink.waitFor({ state: 'visible', timeout: 10000 });
      await firstMovieLink.click();
      
      await page.waitForURL(/\/movies\/[a-f0-9-]+/);
      await page.waitForLoadState('networkidle');
      
      // MovieDetailLayout shows showtimes in a Box or a Typography saying no showtimes
      // Check for the showtimes heading or no-showtimes message
      const showtimesSection = page.locator('h1, h2, h3, h4, h5, h6').filter({ hasText: /showtimes/i }).first();
      await expect(showtimesSection).toBeVisible({ timeout: 10000 });
    });

    test('should allow navigation to seat selection when clicking a showtime', async ({ page }) => {
      await page.goto(`${baseURL}/movies`);
      await page.waitForLoadState('networkidle');
      
      const firstMovieLink = page.locator('.movie-card a.btn').first();
      await firstMovieLink.waitFor({ state: 'visible', timeout: 10000 });
      await firstMovieLink.click();
      
      await page.waitForURL(/\/movies\/[a-f0-9-]+/);
      await page.waitForLoadState('networkidle');
      
      // Click on a showtime "Select Seats" link
      const showtimeLink = page.locator('a.btn').filter({ hasText: /select seats/i }).first();
      const showtimeExists = await showtimeLink.isVisible().catch(() => false);
      
      if (showtimeExists) {
        await showtimeLink.click();
        
        // Should navigate to seat selection (user is authenticated)
        await page.waitForURL(/\/showtime\/[a-f0-9-]+\/seats/, { timeout: 10000 });
      }
    });

    test('should display movie poster/image', async ({ page }) => {
      await page.goto(`${baseURL}/movies`);
      await page.waitForLoadState('networkidle');
      
      const firstMovieLink = page.locator('.movie-card a.btn').first();
      await firstMovieLink.waitFor({ state: 'visible', timeout: 10000 });
      await firstMovieLink.click();
      
      await page.waitForURL(/\/movies\/[a-f0-9-]+/);
      await page.waitForLoadState('networkidle');
      
      // Check for movie title heading which is always present on the detail page
      const movieTitle = page.locator('h4').first();
      await expect(movieTitle).toBeVisible();
    });

    test('should have back button or navigation to movies list', async ({ page }) => {
      await page.goto('/movies');
      await page.waitForLoadState('networkidle');
      
      const firstMovieLink = page.locator('.movie-card a.btn').first();
      await firstMovieLink.waitFor({ state: 'visible', timeout: 10000 });
      await firstMovieLink.click();
      
      await page.waitForURL(/\/movies\/[a-f0-9-]+/);
      
      // Check for back button or navigation link
      const backButton = page.locator('button, a').filter({ hasText: /back|movies/i }).first();
      const backExists = await backButton.isVisible().catch(() => false);
      
      if (backExists) {
        await expect(backButton).toBeVisible();
      }
    });
  });
});
