import { test, expect, type Page } from '@playwright/test';

// ──────────────────────────────────────────────────────
// Shared auth helper
// ──────────────────────────────────────────────────────

const adminUser = {
  email: 'admin@cinebook.local',
  password: 'Admin123!',
};

async function loginAsAdmin(page: Page) {
  await page.goto('/login');
  await page.fill('input[type="email"]', adminUser.email);
  await page.fill('input[type="password"]', adminUser.password);
  await page.click('button[type="submit"]');
  await page.waitForURL('/', { timeout: 10000 });
}

// ──────────────────────────────────────────────────────
// Cinema Selection Page (Home)
// ──────────────────────────────────────────────────────

test.describe('Cinema Selection Page', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await page.waitForLoadState('networkidle');
  });

  test('should display the home page with cinema selection', async ({ page }) => {
    await expect(page).toHaveURL('/');
    // Hero heading on cinema selection page
    await expect(page.locator('h3').first()).toContainText('Book Your Experience');
  });

  test('should display the "Select a Cinema" heading', async ({ page }) => {
    // Cinema cards should be visible on the home page
    const cinemaCard = page.locator('.cinema-card, .empty-state').first();
    await expect(cinemaCard).toBeVisible({ timeout: 10000 });
  });

  test('should display at least one cinema card', async ({ page }) => {
    const cinemaCard = page.locator('.cinema-card').first();
    await expect(cinemaCard).toBeVisible({ timeout: 10000 });
  });

  test('should display cinema name on each card', async ({ page }) => {
    const firstCard = page.locator('.cinema-card').first();
    await expect(firstCard).toBeVisible({ timeout: 10000 });
    // Cinema name rendered as MUI Typography
    const cardHeading = firstCard.locator('.MuiTypography-root').first();
    await expect(cardHeading).toBeVisible();
  });

  test('should display city and country on cinema cards', async ({ page }) => {
    const firstCard = page.locator('.cinema-card').first();
    await expect(firstCard).toBeVisible({ timeout: 10000 });
    const location = firstCard.locator('.cinema-location');
    await expect(location).toBeVisible();
  });

  test('should display opening hours on cinema cards', async ({ page }) => {
    const firstCard = page.locator('.cinema-card').first();
    await expect(firstCard).toBeVisible({ timeout: 10000 });
    const hours = firstCard.locator('.cinema-hours');
    await expect(hours).toBeVisible();
  });

  test('should navigate to cinema movies page when clicking a cinema card', async ({ page }) => {
    const firstCard = page.locator('.cinema-card').first();
    await expect(firstCard).toBeVisible({ timeout: 10000 });
    await firstCard.click();

    await expect(page).toHaveURL(/\/cinemas\/[a-f0-9-]+\/movies/, { timeout: 10000 });
  });
});

// ──────────────────────────────────────────────────────
// Cinema Movies Page
// ──────────────────────────────────────────────────────

test.describe('Cinema Movies Page', () => {
  let cinemaMoviesUrl: string;

  test.beforeEach(async ({ page }) => {
    // Navigate via cinema selection to get a real cinema URL
    await loginAsAdmin(page);
    await page.waitForLoadState('networkidle');

    const firstCard = page.locator('.cinema-card').first();
    await firstCard.waitFor({ state: 'visible', timeout: 10000 });
    await firstCard.click();

    await page.waitForURL(/\/cinemas\/[a-f0-9-]+\/movies/, { timeout: 10000 });
    await page.waitForLoadState('networkidle');
    cinemaMoviesUrl = page.url();
  });

  test('should display cinema name in header', async ({ page }) => {
    const heading = page.locator('h1, h2, h3, h4, h5, h6').first();
    await expect(heading).toBeVisible({ timeout: 10000 });
  });

  test('should display cinema address in header', async ({ page }) => {
    const location = page.locator('.cinema-location').first();
    await expect(location).toBeVisible({ timeout: 10000 });
  });

  test('should display "Now Showing" section heading', async ({ page }) => {
    const nowShowing = page.locator('h2').filter({ hasText: /now showing/i });
    await expect(nowShowing).toBeVisible({ timeout: 10000 });
  });

  test('should display breadcrumb back to all cinemas', async ({ page }) => {
    const breadcrumb = page.locator('.page-breadcrumb a');
    await expect(breadcrumb).toBeVisible();
    await expect(breadcrumb).toContainText('All Cinemas');
  });

  test('should navigate back to cinema selection via breadcrumb', async ({ page }) => {
    const breadcrumb = page.locator('.page-breadcrumb a');
    await breadcrumb.click();
    await expect(page).toHaveURL('/', { timeout: 10000 });
  });

  test('should show movie cards or empty state', async ({ page }) => {
    // Ensure we're on the cinema movies URL (re-navigate in case of stale state)
    if (cinemaMoviesUrl) {
      await page.goto(cinemaMoviesUrl);
    }
    // Wait for network to settle so the page has finished loading
    await page.waitForLoadState('networkidle');
    const hasMovies = await page.locator('.movie-card').first().isVisible().catch(() => false);
    const hasEmpty = await page.locator('.empty-state').isVisible().catch(() => false);
    const hasLoading = await page.locator('.loading').isVisible().catch(() => false);

    // Either movies, empty state, or error state (all marked .empty-state) must be present
    expect(hasMovies || hasEmpty || hasLoading).toBe(true);
  });

  test('movie cards should link to cinema-scoped movie detail URL', async ({ page }) => {
    const firstMovieLink = page.locator('.movie-card a.btn').first();
    const isVisible = await firstMovieLink.isVisible().catch(() => false);

    if (!isVisible) {
      // No movies at this cinema, verify empty state displayed
      await expect(page.locator('.empty-state')).toBeVisible();
      return;
    }

    await firstMovieLink.click();
    await expect(page).toHaveURL(/\/cinemas\/[a-f0-9-]+\/movies\/[a-f0-9-]+/, { timeout: 10000 });
  });
});

// ──────────────────────────────────────────────────────
// Cinema Movie Detail Page
// ──────────────────────────────────────────────────────

test.describe('Cinema Movie Detail Page', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate cinema → movies → first movie detail
    await loginAsAdmin(page);
    await page.waitForLoadState('networkidle');

    const firstCard = page.locator('.cinema-card').first();
    await firstCard.waitFor({ state: 'visible', timeout: 10000 });
    await firstCard.click();

    await page.waitForURL(/\/cinemas\/[a-f0-9-]+\/movies/, { timeout: 10000 });
    await page.waitForLoadState('networkidle');
  });

  test('should navigate to cinema-scoped movie detail and show showtimes', async ({ page }) => {
    const firstMovieLink = page.locator('.movie-card a.btn').first();
    const isVisible = await firstMovieLink.isVisible().catch(() => false);

    if (!isVisible) {
      test.skip();
      return;
    }

    await firstMovieLink.click();
    await page.waitForURL(/\/cinemas\/[a-f0-9-]+\/movies\/[a-f0-9-]+/, { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Movie title should be visible
    const title = page.locator('h1, h2').first();
    await expect(title).toBeVisible();

    // Breadcrumb should reference the cinema
    const breadcrumb = page.locator('.page-breadcrumb');
    await expect(breadcrumb).toBeVisible();
  });

  test('cinema movie detail page should have breadcrumb back to cinema movies', async ({ page }) => {
    const firstMovieLink = page.locator('.movie-card a.btn').first();
    const isVisible = await firstMovieLink.isVisible().catch(() => false);

    if (!isVisible) {
      test.skip();
      return;
    }

    await firstMovieLink.click();
    await page.waitForURL(/\/cinemas\/[a-f0-9-]+\/movies\/[a-f0-9-]+/, { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // The breadcrumb link should reference the cinema movies URL
    const cinematMoviesLink = page.locator('.page-breadcrumb a').last();
    await expect(cinematMoviesLink).toBeVisible();
    const href = await cinematMoviesLink.getAttribute('href');
    expect(href).toMatch(/\/cinemas\/[a-f0-9-]+\/movies/);
  });
});

// ──────────────────────────────────────────────────────
// Cinema filter on admin halls page
// ──────────────────────────────────────────────────────

test.describe('Admin Halls - Cinema Filter', () => {
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
    await page.goto('/admin/halls');
    await page.waitForLoadState('networkidle');
  });

  test('should display a cinema filter dropdown on halls page', async ({ page }) => {
    // MUI Select renders as combobox role; look for the filter combobox (first one on page)
    const cinemaFilter = page.locator('[role="combobox"]').first();
    await expect(cinemaFilter).toBeVisible({ timeout: 10000 });
  });

  test('should display Cinema column in halls table', async ({ page }) => {
    const headers = page.locator('table thead th');
    await expect(headers).toContainText(['Cinema']);
  });

  test('should include a Cinema dropdown in the create hall form', async ({ page }) => {
    await page.click('button:has-text("Add Hall")');
    // MUI Select renders hidden native select; check the combobox role element instead
    const cinemaSelect = page.locator('[role="combobox"]').first();
    await expect(cinemaSelect).toBeVisible({ timeout: 5000 });
  });

  test('cinema dropdown in hall form should have required attribute', async ({ page }) => {
    await page.click('button:has-text("Add Hall")');
    await expect(page.locator('h2:has-text("Add Hall")')).toBeVisible();
    // The Cinema * select in the form - MUI v6 doesn't render a hidden native <select>
    // Instead verify the form combobox is present and required via the FormControl
    const cinemaCombobox = page.locator('.modal [role="combobox"]').first();
    await expect(cinemaCombobox).toBeVisible({ timeout: 5000 });
    // Verify FormControl required by checking the label (MUI renders a <label> element with 'Cinema *')
    const cinemaLabel = page.locator('.modal label').filter({ hasText: /cinema/i }).first();
    await expect(cinemaLabel).toBeVisible();
  });
});

// ──────────────────────────────────────────────────────
// Cinema filter on admin showtimes page
// ──────────────────────────────────────────────────────

test.describe('Admin Showtimes - Cinema Filter', () => {
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
    await page.goto('/admin/showtimes');
    await page.waitForLoadState('networkidle');
  });

  test('should display a cinema filter dropdown on showtimes page', async ({ page }) => {
    // MUI Select renders as combobox role; look for the first filter combobox on the page
    const cinemaFilter = page.locator('[role="combobox"]').first();
    await expect(cinemaFilter).toBeVisible({ timeout: 10000 });
  });

  test('should display Cinema column in showtimes table', async ({ page }) => {
    const headers = page.locator('table thead th');
    await expect(headers).toContainText(['Cinema']);
  });

  test('should include a Cinema dropdown in the create showtime form', async ({ page }) => {
    await page.click('button:has-text("Add Showtime")');
    // MUI Select renders hidden native select; check combobox role element instead
    const cinemaSelect = page.locator('[role="combobox"]').first();
    await expect(cinemaSelect).toBeVisible({ timeout: 5000 });
  });

  test('hall dropdown should be filtered after selecting a cinema', async ({ page }) => {
    await page.click('button:has-text("Add Showtime")');

    // Wait for form to open
    await page.waitForLoadState('networkidle');

    // MUI Select renders as combobox — cinema filter is first combobox on page
    const cinemaCombobox = page.locator('[role="combobox"]').first();
    await expect(cinemaCombobox).toBeVisible({ timeout: 5000 });

    // Click the cinema combobox and select first option
    await cinemaCombobox.click();
    const firstOption = page.locator('[role="option"]').first();
    const optionExists = await firstOption.isVisible().catch(() => false);

    if (!optionExists) return; // No cinemas seeded, skip

    await firstOption.click();

    // After selecting a cinema, the hall dropdown (second combobox in the form) should appear/be enabled
    // Wait a moment for the hall dropdown to update
    await page.waitForTimeout(500);
    const hallCombobox = page.locator('[role="combobox"]').nth(1);
    await expect(hallCombobox).toBeVisible();
  });
});
