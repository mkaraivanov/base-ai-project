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

  /** Helper: navigate to the seat selection page for the first available showtime */
  async function navigateToSeatSelection(page: import('@playwright/test').Page) {
    await page.goto('/movies');
    await page.waitForLoadState('networkidle');

    // Collect all movie link hrefs upfront before navigating
    const movieLinks = page.locator('a.btn').filter({ hasText: /view showtimes/i });
    const movieLinkCount = await movieLinks.count();
    if (movieLinkCount === 0) {
      // No movies — skip gracefully
      return;
    }

    const hrefs: string[] = [];
    for (let i = 0; i < movieLinkCount; i++) {
      const href = await movieLinks.nth(i).getAttribute('href');
      if (href) hrefs.push(href);
    }

    // Navigate to each movie and look for a "Select Seats" link
    for (const href of hrefs) {
      await page.goto(href);
      await page.waitForLoadState('networkidle');

      const showtimeLink = page.locator('a.btn').filter({ hasText: /select seats/i }).first();
      const hasSeatLink = await showtimeLink.isVisible().catch(() => false);
      if (hasSeatLink) {
        await showtimeLink.click();
        await page.waitForURL(/\/showtime\/[a-f0-9-]+\/seats/, { timeout: 10000 });
        await page.waitForLoadState('networkidle');
        return;
      }
    }
    // If we get here, no movie had an available showtime — skip gracefully
  }

  test('should complete full booking flow: login → browse → select seats → ticket type → checkout → confirmation', async ({ page }) => {
    test.setTimeout(90000); // Multi-step integration flow needs more time

    // Step 1: Login
    await page.goto('/login');
    await page.waitForLoadState('networkidle');

    await page.locator('input#email, input[type="email"]').fill(testUser.email);
    await page.locator('input#password, input[type="password"]').fill(testUser.password);
    await page.locator('button[type="submit"]').click();

    // Wait for successful login - should redirect to home page
    await page.waitForURL('/', { timeout: 15000 });

    // Step 2: Browse movies → showtime → seat selection
    await navigateToSeatSelection(page);
    if (!page.url().includes('/seats')) {
      test.skip(true, 'No active showtime with available seats found in the environment');
      return;
    }

    // Step 3: Wait for ticket types to load (they are fetched alongside availability)
    // The booking summary heading is rendered once data loads
    await page.locator('.booking-summary').waitFor({ state: 'visible', timeout: 15000 });

    // Step 4: Select a seat – the default ticket type (Adult) is auto-assigned
    const availableSeat = page.locator('button.seat.seat-available').first();
    await availableSeat.waitFor({ state: 'visible', timeout: 10000 });
    await availableSeat.click();

    // Step 5: Verify booking summary shows the seat with a ticket type
    const summaryTable = page.locator('.booking-summary table');
    await expect(summaryTable).toBeVisible({ timeout: 5000 });

    // The ticket type column should contain a named ticket type (e.g. Adult)
    const ticketTypeCell = summaryTable.locator('td').filter({ hasText: /adult|children|senior/i }).first();
    await expect(ticketTypeCell).toBeVisible({ timeout: 5000 });

    // Step 6: Reserve the selected seats (button shows count: "Reserve Seats (1)")
    const reserveButton = page.locator('button').filter({ hasText: /reserve seats/i }).first();
    await reserveButton.waitFor({ state: 'visible', timeout: 10000 });
    await reserveButton.click();

    // Wait for reservation to be created and the "Proceed to Checkout" button to appear
    const proceedButton = page.locator('button').filter({ hasText: /proceed to checkout/i }).first();
    await proceedButton.waitFor({ state: 'visible', timeout: 15000 });

    // Step 7: Proceed to checkout
    await proceedButton.click();
    await page.waitForURL(/\/checkout\//, { timeout: 10000 });

    // Step 8: Complete checkout
    await page.waitForLoadState('networkidle');

    await page.locator('#cardHolderName').fill('Booking Tester');
    await page.locator('#cardNumber').fill('4111111111111111');
    await page.locator('#expiryDate').fill('12/28');
    await page.locator('#cvv').fill('123');

    const confirmButton = page.locator('button[type="submit"]').first();
    await confirmButton.waitFor({ state: 'visible', timeout: 10000 });
    await confirmButton.click();

    // Step 9: Verify confirmation page
    await page.waitForURL(/\/confirmation\//, { timeout: 15000 });

    const confirmationMessage = page.locator('h1, h2').filter({ hasText: /confirm|success|thank you|booking/i }).first();
    await expect(confirmationMessage).toBeVisible({ timeout: 10000 });

    const bookingRef = page.locator('.detail-value, .booking-number, code, strong').first();
    await expect(bookingRef).toBeVisible();
  });

  test.describe('Seat Selection Page', () => {
    test.beforeEach(async ({ page }) => {
      test.setTimeout(60000); // Navigation to seat selection can be slow under load
      // Login first
      await page.goto('/login');
      await page.waitForLoadState('networkidle');

      await page.locator('input#email, input[type="email"]').fill(testUser.email);
      await page.locator('input#password, input[type="password"]').fill(testUser.password);
      await page.locator('button[type="submit"]').click();

      await page.waitForURL('/', { timeout: 15000 });
    });

    test('should display seat layout', async ({ page }) => {
      await navigateToSeatSelection(page);
      if (!page.url().includes('/seats')) {
        test.skip(true, 'No active showtime with available seats found in the environment');
        return;
      }

      // Verify seat layout is visible
      const seatLayout = page.locator('.seats-container').first();
      await expect(seatLayout).toBeVisible({ timeout: 15000 });
    });

    test('should allow selecting and deselecting seats', async ({ page }) => {
      await navigateToSeatSelection(page);
      if (!page.url().includes('/seats')) {
        test.skip(true, 'No active showtime with available seats found in the environment');
        return;
      }

      // Select a seat – capture its label so we can track it after the class changes
      const availableSeat = page.locator('button.seat.seat-available').first();
      await availableSeat.waitFor({ state: 'visible', timeout: 10000 });
      const seatTitle = await availableSeat.getAttribute('title');
      await availableSeat.click();

      // Re-locate the same seat by its unique title attribute to avoid partial text matches
      const clickedSeat = page.locator(`button.seat[title="${seatTitle}"]`);

      // Verify seat is selected
      await expect(clickedSeat).toHaveClass(/seat-selected/);

      // Deselect the seat
      await clickedSeat.click();

      // Verify seat is deselected
      await expect(clickedSeat).not.toHaveClass(/seat-selected/);
    });

    test('should display booking summary table with ticket type and price after seat selection', async ({ page }) => {
      await navigateToSeatSelection(page);
      if (!page.url().includes('/seats')) {
        test.skip(true, 'No active showtime with available seats found in the environment');
        return;
      }

      // Wait for ticket types to be loaded (summary section is always rendered)
      await page.locator('.booking-summary').waitFor({ state: 'visible', timeout: 15000 });

      // Select a seat
      const availableSeat = page.locator('button.seat.seat-available').first();
      await availableSeat.waitFor({ state: 'visible', timeout: 10000 });
      await availableSeat.click();

      // Booking summary table should appear with Seat / Ticket Type / Price columns
      const summaryTable = page.locator('.booking-summary table');
      await expect(summaryTable).toBeVisible({ timeout: 5000 });

      await expect(summaryTable.locator('th').filter({ hasText: /seat/i })).toBeVisible();
      await expect(summaryTable.locator('th').filter({ hasText: /ticket type/i })).toBeVisible();
      await expect(summaryTable.locator('th').filter({ hasText: /price/i })).toBeVisible();

      // The total row should be visible in the table footer
      await expect(summaryTable.locator('tfoot').filter({ hasText: /total/i })).toBeVisible();
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
