import { test, expect } from '@playwright/test';

test.describe('Customer - Complete Booking Flow', () => {
  const timestamp = Date.now();
  const testUser = {
    email: `booking.test+${timestamp}@example.com`,
    password: 'Test123456',
    firstName: 'Booking',
    lastName: 'Tester',
    phoneNumber: '+1 (555) 987-6543',
  };

  test.beforeAll(async ({ browser }) => {
    // Register a test user for booking flow
    const context = await browser.newContext();
    const page = await context.newPage();
    
    await page.goto('/register');
    await page.fill('input#firstName', testUser.firstName);
    await page.fill('input#lastName', testUser.lastName);
    await page.fill('input#email', testUser.email);
    await page.fill('input#phoneNumber', testUser.phoneNumber);
    await page.fill('input#password', testUser.password);
    await page.fill('input#confirmPassword', testUser.password);
    await page.click('button[type="submit"]');
    
    // Wait for registration to complete
    await page.waitForURL(/\/(|login)/, { timeout: 15000 }).catch(() => {});
    
    await context.close();
  });

  test('should complete full booking flow: login → browse → select seats → checkout → confirmation', async ({ page }) => {
    // Step 1: Login
    await page.goto('/login');
    await page.fill('input#email, input[type="email"]', testUser.email);
    await page.fill('input#password, input[type="password"]', testUser.password);
    await page.click('button[type="submit"]');
    
    // Wait for successful login
    await page.waitForURL('/', { timeout: 10000 }).catch(() => {});
    
    // Step 2: Browse movies
    await page.goto('/movies');
    await page.waitForLoadState('networkidle');
    
    // Step 3: Select a movie
    const firstMovie = page.locator('[data-testid="movie-card"], .movie-card, article').first();
    await firstMovie.waitFor({ state: 'visible', timeout: 10000 });
    await firstMovie.click();
    
    await page.waitForURL(/\/movies\/\d+/);
    await page.waitForLoadState('networkidle');
    
    // Step 4: Select a showtime
    const showtimeButton = page.locator('button').filter({ hasText: /book|select/i }).first();
    await showtimeButton.waitFor({ state: 'visible', timeout: 10000 });
    await showtimeButton.click();
    
    // Should navigate to seat selection
    await page.waitForURL(/\/showtime\/\d+\/seats/, { timeout: 10000 });
    
    // Step 5: Select seats
    await page.waitForLoadState('networkidle');
    
    // Select a seat (adjust selector based on actual implementation)
    const availableSeat = page.locator('[data-testid="seat"]:not(.occupied):not(.selected), .seat:not(.occupied):not(.selected), button.seat:not(:disabled)').first();
    await availableSeat.waitFor({ state: 'visible', timeout: 10000 });
    await availableSeat.click();
    
    // Step 6: Proceed to checkout
    const proceedButton = page.locator('button').filter({ hasText: /continue|proceed|checkout|next/i }).first();
    await proceedButton.waitFor({ state: 'visible', timeout: 10000 });
    await proceedButton.click();
    
    // Should navigate to checkout
    await page.waitForURL(/\/checkout\//, { timeout: 10000 });
    
    // Step 7: Complete checkout
    await page.waitForLoadState('networkidle');
    
    // Fill payment details (if required)
    const cardNumberInput = page.locator('input[name*="card" i], input[placeholder*="card" i]').first();
    const cardExists = await cardNumberInput.isVisible().catch(() => false);
    
    if (cardExists) {
      await cardNumberInput.fill('4242424242424242');
      
      const expiryInput = page.locator('input[name*="expiry" i], input[placeholder*="expiry" i], input[placeholder*="mm/yy" i]').first();
      if (await expiryInput.isVisible().catch(() => false)) {
        await expiryInput.fill('12/25');
      }
      
      const cvcInput = page.locator('input[name*="cvc" i], input[name*="cvv" i], input[placeholder*="cvc" i]').first();
      if (await cvcInput.isVisible().catch(() => false)) {
        await cvcInput.fill('123');
      }
    }
    
    // Confirm booking
    const confirmButton = page.locator('button[type="submit"], button').filter({ hasText: /confirm|pay|complete/i }).first();
    await confirmButton.waitFor({ state: 'visible', timeout: 10000 });
    await confirmButton.click();
    
    // Step 8: Verify confirmation page
    await page.waitForURL(/\/confirmation\//, { timeout: 15000 });
    
    // Check for booking confirmation
    const confirmationMessage = page.locator('h1, h2').filter({ hasText: /confirm|success|thank you/i }).first();
    await expect(confirmationMessage).toBeVisible({ timeout: 10000 });
    
    // Check for booking number/reference
    const bookingNumber = page.locator('[data-testid="booking-number"], .booking-number, code, strong').first();
    await expect(bookingNumber).toBeVisible();
  });

  test.describe('Seat Selection Page', () => {
    test.beforeEach(async ({ page }) => {
      // Login first
      await page.goto('/login');
      await page.fill('input#email, input[type="email"]', testUser.email);
      await page.fill('input#password, input[type="password"]', testUser.password);
      await page.click('button[type="submit"]');
      await page.waitForURL('/', { timeout: 10000 }).catch(() => {});
    });

    test('should display seat layout', async ({ page }) => {
      // Navigate to a showtime seat selection (need to go through movie detail)
      await page.goto('/movies');
      await page.waitForLoadState('networkidle');
      
      const firstMovie = page.locator('[data-testid="movie-card"], .movie-card, article').first();
      await firstMovie.waitFor({ state: 'visible', timeout: 10000 });
      await firstMovie.click();
      
      await page.waitForURL(/\/movies\/\d+/);
      await page.waitForLoadState('networkidle');
      
      const showtimeButton = page.locator('button').filter({ hasText: /book|select/i }).first();
      await showtimeButton.waitFor({ state: 'visible', timeout: 10000 });
      await showtimeButton.click();
      
      await page.waitForURL(/\/showtime\/\d+\/seats/);
      await page.waitForLoadState('networkidle');
      
      // Verify seat layout is visible
      const seatLayout = page.locator('[data-testid="seat-layout"], .seat-layout, .seats-container').first();
      await expect(seatLayout).toBeVisible({ timeout: 10000 });
    });

    test('should allow selecting and deselecting seats', async ({ page }) => {
      await page.goto('/movies');
      await page.waitForLoadState('networkidle');
      
      const firstMovie = page.locator('[data-testid="movie-card"], .movie-card, article').first();
      await firstMovie.click();
      
      await page.waitForURL(/\/movies\/\d+/);
      const showtimeButton = page.locator('button').filter({ hasText: /book|select/i }).first();
      await showtimeButton.click();
      
      await page.waitForURL(/\/showtime\/\d+\/seats/);
      await page.waitForLoadState('networkidle');
      
      // Select a seat
      const availableSeat = page.locator('[data-testid="seat"]:not(.occupied):not(.selected), .seat:not(.occupied):not(.selected), button.seat:not(:disabled)').first();
      await availableSeat.waitFor({ state: 'visible', timeout: 10000 });
      await availableSeat.click();
      
      // Verify seat is selected
      await expect(availableSeat).toHaveClass(/selected|active/);
      
      // Deselect the seat
      await availableSeat.click();
      
      // Verify seat is deselected
      await expect(availableSeat).not.toHaveClass(/selected|active/);
    });

    test('should display total price when seats are selected', async ({ page }) => {
      await page.goto('/movies');
      await page.waitForLoadState('networkidle');
      
      const firstMovie = page.locator('[data-testid="movie-card"], .movie-card, article').first();
      await firstMovie.click();
      
      await page.waitForURL(/\/movies\/\d+/);
      const showtimeButton = page.locator('button').filter({ hasText: /book|select/i }).first();
      await showtimeButton.click();
      
      await page.waitForURL(/\/showtime\/\d+\/seats/);
      await page.waitForLoadState('networkidle');
      
      // Select a seat
      const availableSeat = page.locator('[data-testid="seat"]:not(.occupied):not(.selected), .seat:not(.occupied):not(.selected), button.seat:not(:disabled)').first();
      await availableSeat.click();
      
      // Check for total price display
      const totalPrice = page.locator('[data-testid="total-price"], .total-price, .price').filter({ hasText: /total|price|\$/i }).first();
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
