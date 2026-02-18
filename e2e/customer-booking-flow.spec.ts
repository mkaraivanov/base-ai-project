import { test, expect } from '@playwright/test';

test.describe('Customer - Complete Booking Flow', () => {
  const timestamp = Date.now();
  const testUser = {
    email: `booking.test+${timestamp}@example.com`,
    password: 'Test123456',
    firstName: 'Booking',
    lastName: 'Tester',
    phoneNumber: '+15559876543',
  };

  test.beforeAll(async ({ browser }) => {
    // Register a test user for booking flow via API (more reliable than form-based registration)
    const context = await browser.newContext();
    const page = await context.newPage();
    
    const response = await page.request.post('http://localhost:5076/api/auth/register', {
      data: {
        email: testUser.email,
        password: testUser.password,
        firstName: testUser.firstName,
        lastName: testUser.lastName,
        phoneNumber: testUser.phoneNumber,
      },
    });
    
    if (!response.ok()) {
      console.error('Registration failed:', await response.text());
    }
    
    await context.close();
  });

  test('should complete full booking flow: login → browse → select seats → checkout → confirmation', async ({ page }) => {
    // Step 1: Login
    await page.goto('/login');
    await page.waitForLoadState('networkidle');
    
    await page.locator('input#email, input[type="email"]').fill(testUser.email);
    await page.locator('input#password, input[type="password"]').fill(testUser.password);
    await page.locator('button[type="submit"]').click();
    
    // Wait for successful login - should redirect to home page    
    await page.waitForURL('/', { timeout: 15000 });

    // Step 2: Navigate via cinema selection → cinema movies
    await page.waitForLoadState('networkidle');
    const firstCinema = page.locator('.cinema-card').first();
    await firstCinema.waitFor({ state: 'visible', timeout: 10000 });
    await firstCinema.click();
    await page.waitForURL(/\/cinemas\/[a-f0-9-]+\/movies/, { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Step 3: Select a movie (click on "View Showtimes" link)
    const firstMovieLink = page.locator('a.btn').filter({ hasText: /view showtimes/i }).first();
    const movieLinkVisible = await firstMovieLink.isVisible().catch(() => false);
    if (!movieLinkVisible) {
      // No movies with showtimes at this cinema — skip gracefully
      return;
    }
    await firstMovieLink.waitFor({ state: 'visible', timeout: 10000 });
    await firstMovieLink.click();

    await page.waitForURL(/\/cinemas\/[a-f0-9-]+\/movies\/[a-f0-9-]+/, { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Step 4: Select a showtime (click "Select Seats" link)
    const showtimeLink = page.locator('a.btn').filter({ hasText: /select seats/i }).first();
    await showtimeLink.waitFor({ state: 'visible', timeout: 10000 });
    await showtimeLink.click();
    
    // Should navigate to seat selection
    await page.waitForURL(/\/showtime\/[a-f0-9-]+\/seats/, { timeout: 10000 });
    
    // Step 5: Select seats
    await page.waitForLoadState('networkidle');
    
    // Select a seat (adjust selector based on actual implementation)
    const availableSeat = page.locator('button.seat.seat-available').first();
    await availableSeat.waitFor({ state: 'visible', timeout: 10000 });
    await availableSeat.click();
    
    // Step 6: Reserve the selected seats
    const reserveButton = page.locator('button').filter({ hasText: /reserve seats/i }).first();
    await reserveButton.waitFor({ state: 'visible', timeout: 10000 });
    await reserveButton.click();
    
    // Wait for reservation to be created
    await page.waitForTimeout(1000);
    
    // Step 7: Proceed to checkout
    const proceedButton = page.locator('button').filter({ hasText: /proceed to checkout/i }).first();
    await proceedButton.waitFor({ state: 'visible', timeout: 10000 });
    await proceedButton.click();
    
    // Should navigate to checkout
    await page.waitForURL(/\/checkout\//, { timeout: 10000 });
    
    // Step 8: Complete checkout
    await page.waitForLoadState('networkidle');
    
    // Fill payment details
    await page.locator('#cardHolderName').fill('Booking Tester');
    await page.locator('#cardNumber').fill('4111111111111111');
    await page.locator('#expiryDate').fill('12/28');
    await page.locator('#cvv').fill('123');
    
    // Confirm booking
    const confirmButton = page.locator('button[type="submit"]').first();
    await confirmButton.waitFor({ state: 'visible', timeout: 10000 });
    await confirmButton.click();
    
    // Step 9: Verify confirmation page
    await page.waitForURL(/\/confirmation\//, { timeout: 15000 });
    
    // Check for booking confirmation
    const confirmationMessage = page.locator('h1, h2').filter({ hasText: /confirm|success|thank you|booking/i }).first();
    await expect(confirmationMessage).toBeVisible({ timeout: 10000 });
    
    // Check for booking number/reference
    const bookingRef = page.locator('.detail-value, .booking-number, code, strong').first();
    await expect(bookingRef).toBeVisible();
  });

  test.describe('Seat Selection Page', () => {
    test.beforeEach(async ({ page }) => {
      // Login first
      await page.goto('/login');
      await page.waitForLoadState('networkidle');
      
      await page.locator('input#email, input[type="email"]').fill(testUser.email);
      await page.locator('input#password, input[type="password"]').fill(testUser.password);
      await page.locator('button[type="submit"]').click();
      
      await page.waitForURL('/', { timeout: 15000 });
    });

    test('should display seat layout', async ({ page }) => {
      // Navigate via cinema selection → cinema movies → movie detail → seat selection
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      const firstCinema = page.locator('.cinema-card').first();
      await firstCinema.waitFor({ state: 'visible', timeout: 10000 });
      await firstCinema.click();
      await page.waitForURL(/\/cinemas\/[a-f0-9-]+\/movies/, { timeout: 10000 });
      await page.waitForLoadState('networkidle');

      const firstMovieLink = page.locator('a.btn').filter({ hasText: /view showtimes/i }).first();
      const movieLinkVisible = await firstMovieLink.isVisible().catch(() => false);
      if (!movieLinkVisible) {
        test.skip();
        return;
      }
      await firstMovieLink.click();
      await page.waitForURL(/\/cinemas\/[a-f0-9-]+\/movies\/[a-f0-9-]+/, { timeout: 10000 });
      await page.waitForLoadState('networkidle');

      const showtimeLink = page.locator('a.btn').filter({ hasText: /select seats/i }).first();
      await showtimeLink.waitFor({ state: 'visible', timeout: 10000 });
      await showtimeLink.click();

      await page.waitForURL(/\/showtime\/[a-f0-9-]+\/seats/, { timeout: 10000 });
      await page.waitForLoadState('networkidle');

      // Verify seat layout is visible
      const seatLayout = page.locator('.seats-container').first();
      await expect(seatLayout).toBeVisible({ timeout: 10000 });
    });

    test('should allow selecting and deselecting seats', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      const firstCinema = page.locator('.cinema-card').first();
      await firstCinema.waitFor({ state: 'visible', timeout: 10000 });
      await firstCinema.click();
      await page.waitForURL(/\/cinemas\/[a-f0-9-]+\/movies/, { timeout: 10000 });
      await page.waitForLoadState('networkidle');

      const firstMovieLink = page.locator('a.btn').filter({ hasText: /view showtimes/i }).first();
      const movieLinkVisible = await firstMovieLink.isVisible().catch(() => false);
      if (!movieLinkVisible) {
        test.skip();
        return;
      }
      await firstMovieLink.click();
      await page.waitForURL(/\/cinemas\/[a-f0-9-]+\/movies\/[a-f0-9-]+/, { timeout: 10000 });

      const showtimeLink = page.locator('a.btn').filter({ hasText: /select seats/i }).first();
      await showtimeLink.waitFor({ state: 'visible', timeout: 10000 });
      await showtimeLink.click();

      await page.waitForURL(/\/showtime\/[a-f0-9-]+\/seats/, { timeout: 10000 });
      await page.waitForLoadState('networkidle');

      // Select a seat - capture its title so we can track it after class changes
      const availableSeat = page.locator('button.seat.seat-available').first();
      await availableSeat.waitFor({ state: 'visible', timeout: 10000 });
      const seatTitle = await availableSeat.getAttribute('title');
      await availableSeat.click();

      // Re-locate the same seat by its title attribute
      const clickedSeat = page.locator(`button.seat[title="${seatTitle}"]`);

      // Verify seat is selected
      await expect(clickedSeat).toHaveClass(/seat-selected/);

      // Deselect the seat
      await clickedSeat.click();

      // Verify seat is deselected
      await expect(clickedSeat).not.toHaveClass(/seat-selected/);
    });

    test('should display total price when seats are selected', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      const firstCinema = page.locator('.cinema-card').first();
      await firstCinema.waitFor({ state: 'visible', timeout: 10000 });
      await firstCinema.click();
      await page.waitForURL(/\/cinemas\/[a-f0-9-]+\/movies/, { timeout: 10000 });
      await page.waitForLoadState('networkidle');

      const firstMovieLink = page.locator('a.btn').filter({ hasText: /view showtimes/i }).first();
      const movieLinkVisible = await firstMovieLink.isVisible().catch(() => false);
      if (!movieLinkVisible) {
        test.skip();
        return;
      }
      await firstMovieLink.click();
      await page.waitForURL(/\/cinemas\/[a-f0-9-]+\/movies\/[a-f0-9-]+/, { timeout: 10000 });

      const showtimeLink = page.locator('a.btn').filter({ hasText: /select seats/i }).first();
      await showtimeLink.waitFor({ state: 'visible', timeout: 10000 });
      await showtimeLink.click();

      await page.waitForURL(/\/showtime\/[a-f0-9-]+\/seats/, { timeout: 10000 });
      await page.waitForLoadState('networkidle');

      // Select a seat
      const availableSeat = page.locator('button.seat.seat-available').first();
      await availableSeat.waitFor({ state: 'visible', timeout: 10000 });
      await availableSeat.click();

      // Check for total price display in booking summary
      const totalPrice = page.locator('.booking-summary').filter({ hasText: /total/i }).first();
      await expect(totalPrice).toBeVisible();
    });
  });

  test.describe('Checkout Page', () => {
    test('should require authentication to access checkout', async ({ page }) => {
      // Try to access checkout directly without login
      await page.goto('/checkout/123');
      
      // Should redirect to login
      await expect(page).toHaveURL(/\/login/);
    });
  });
});
