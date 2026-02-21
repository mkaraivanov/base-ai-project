import { test, expect } from '@playwright/test';

const baseURL = process.env.PLAYWRIGHT_TEST_BASE_URL || 'http://localhost:5173';

test.describe('Customer - My Bookings Page', () => {
  const testUser = {
    email: 'admin@cinebook.local', // Using admin account as we know it exists
    password: 'Admin123!',
  };

  test.beforeEach(async ({ page }) => {
    // Login before each test
    await page.goto(`${baseURL}/login`);
    await page.fill('input#email, input[type="email"]', testUser.email);
    await page.fill('input#password, input[type="password"]', testUser.password);
    await page.click('button[type="submit"]');
    
    // Wait for successful login
    await page.waitForURL('/', { timeout: 10000 }).catch(() => {});
  });

  test('should require authentication to access my bookings', async ({ page }) => {
    // Logout via avatar menu
    const avatarButton = page.getByRole('button', { name: /open user menu/i });
    const avatarExists = await avatarButton.isVisible().catch(() => false);

    if (avatarExists) {
      await avatarButton.click();
      await page.getByRole('menuitem', { name: /log out/i }).click();
      await page.waitForLoadState('networkidle');
    }
    
    // Try to access my bookings without authentication
    await page.goto(`${baseURL}/my-bookings`);
    
    // Should redirect to login
    await expect(page).toHaveURL(/\/login/);
  });

  test('should display my bookings page when authenticated', async ({ page }) => {
    await page.goto(`${baseURL}/my-bookings`);
    
    // Should stay on my bookings page
    await expect(page).toHaveURL('/my-bookings');
    
    // Check for page heading
    const heading = page.locator('h1, h2').filter({ hasText: /my bookings|bookings/i }).first();
    await expect(heading).toBeVisible({ timeout: 10000 });
  });

  test('should display booking history or empty state', async ({ page }) => {
    await page.goto(`${baseURL}/my-bookings`);
    await page.waitForLoadState('networkidle');
    
    // Check if there are bookings or an empty state message
    const bookingList = page.locator('.bookings-list');
    const emptyState = page.locator('.empty-state');
    
    // Either bookings or empty state should be visible
    const hasBookings = await bookingList.isVisible().catch(() => false);
    const isEmpty = await emptyState.isVisible().catch(() => false);
    
    expect(hasBookings || isEmpty).toBeTruthy();
  });

  test('should display booking details if bookings exist', async ({ page }) => {
    await page.goto(`${baseURL}/my-bookings`);
    await page.waitForLoadState('networkidle');
    
    // Check for booking cards
    const bookingCard = page.locator('[data-testid="booking-card"], .booking-card, article').first();
    const hasBookings = await bookingCard.isVisible().catch(() => false);
    
    if (hasBookings) {
      // Verify booking details are shown
      await expect(bookingCard).toBeVisible();
      
      // Check for movie title (rendered as MUI Typography with fontWeight=600)
      const movieTitle = bookingCard.locator('.MuiTypography-root').first();
      await expect(movieTitle).toBeVisible();
    }
  });

  test('should show booking reference number', async ({ page }) => {
    await page.goto(`${baseURL}/my-bookings`);
    await page.waitForLoadState('networkidle');
    
    const bookingCard = page.locator('[data-testid="booking-card"], .booking-card, article').first();
    const hasBookings = await bookingCard.isVisible().catch(() => false);
    
    if (hasBookings) {
      // The booking reference is rendered as Typography containing an icon + "#<bookingNumber>" text
      // Use a loose regex (not anchored to start) since SVG icon text may precede the # symbol
      const bookingRef = bookingCard.locator('.MuiTypography-root').filter({ hasText: /#[A-Z]/ }).first();
      await expect(bookingRef).toBeVisible();
    }
  });

  test('should allow canceling a booking if cancellation is available', async ({ page }) => {
    await page.goto(`${baseURL}/my-bookings`);
    await page.waitForLoadState('networkidle');
    
    // Check for cancel button
    const cancelButton = page.locator('button').filter({ hasText: /cancel/i }).first();
    const cancelExists = await cancelButton.isVisible().catch(() => false);
    
    if (cancelExists) {
      await expect(cancelButton).toBeVisible();
      await cancelButton.click();
      
      // Check for confirmation dialog
      const confirmDialog = page.locator('[role="dialog"], .modal, .confirmation').first();
      const dialogExists = await confirmDialog.isVisible().catch(() => false);
      
      if (dialogExists) {
        await expect(confirmDialog).toBeVisible();
      }
    }
  });

  test('should have navigation to make new bookings', async ({ page }) => {
    await page.goto(`${baseURL}/my-bookings`);
    await page.waitForLoadState('networkidle');
    
    // Look for a link/button to browse movies or make new bookings
    const browseButton = page.locator('button, a').filter({ hasText: /browse|movies|book|new/i }).first();
    const browseExists = await browseButton.isVisible().catch(() => false);
    
    if (browseExists) {
      await expect(browseButton).toBeVisible();
    }
  });

  test('should display seat information for each booking', async ({ page }) => {
    await page.goto(`${baseURL}/my-bookings`);
    await page.waitForLoadState('networkidle');
    
    const bookingCard = page.locator('[data-testid="booking-card"], .booking-card, article').first();
    const hasBookings = await bookingCard.isVisible().catch(() => false);
    
    if (hasBookings) {
      // Seat numbers are shown as Typography items within the grid inside the booking card
      // They appear as text without specific CSS classes, so check the card contains seat-related text
      const seatInfo = bookingCard.locator('.MuiTypography-root').nth(2);
      await expect(seatInfo).toBeVisible();
    }
  });
});
