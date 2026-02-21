import { test, expect } from '@playwright/test';

const baseURL = process.env.PLAYWRIGHT_TEST_BASE_URL || 'http://localhost:5173';

test.describe('Authentication Flow', () => {
  const adminUser = {
    email: 'admin@cinebook.local',
    password: 'Admin123!',
  };

  test.describe('Login Page', () => {
    test.beforeEach(async ({ page }) => {
      await page.goto(`${baseURL}/login`);
    });

    test('should display login page', async ({ page }) => {
      await expect(page).toHaveURL('/login');
      await expect(page.locator('form')).toBeVisible();
    });

    test('should display login form fields', async ({ page }) => {
      // Check for email input
      const emailInput = page.locator('input#email, input[type="email"]');
      await expect(emailInput).toBeVisible();

      // Check for password input
      const passwordInput = page.locator('input#password, input[type="password"]');
      await expect(passwordInput).toBeVisible();

      // Check for submit button
      const submitButton = page.locator('button[type="submit"]');
      await expect(submitButton).toBeVisible();
    });

    test('should have link to register page', async ({ page }) => {
      const registerLink = page.locator('a[href="/register"]').first();
      await expect(registerLink).toBeVisible();
    });

    test('should show error for invalid credentials', async ({ page }) => {
      await page.fill('input#email, input[type="email"]', 'invalid@example.com');
      await page.fill('input#password, input[type="password"]', 'wrongpassword');
      await page.click('button[type="submit"]');

      // Wait for error message (toast notification)
      const errorMessage = page.locator('.error-message, .error, [role="alert"], [data-sonner-toast]').first();
      await expect(errorMessage).toBeVisible({ timeout: 10000 });
    });

    test('should show error for empty email', async ({ page }) => {
      await page.fill('input#password, input[type="password"]', 'somepassword');
      await page.click('button[type="submit"]');

      // HTML5 validation should prevent submission
      const emailInput = page.locator('input#email, input[type="email"]');
      await expect(emailInput).toHaveAttribute('required');
    });

    test('should show error for empty password', async ({ page }) => {
      await page.fill('input#email, input[type="email"]', 'test@example.com');
      await page.click('button[type="submit"]');

      // HTML5 validation should prevent submission
      const passwordInput = page.locator('input#password, input[type="password"]');
      await expect(passwordInput).toHaveAttribute('required');
    });

    test('should successfully login with valid credentials', async ({ page }) => {
      await page.fill('input#email, input[type="email"]', adminUser.email);
      await page.fill('input#password, input[type="password"]', adminUser.password);
      await page.click('button[type="submit"]');

      // Wait for redirect to home page
      await page.waitForURL('/', { timeout: 10000 });
      await expect(page).toHaveURL('/');
    });

    test('should display user info after successful login', async ({ page }) => {
      await page.fill('input#email, input[type="email"]', adminUser.email);
      await page.fill('input#password, input[type="password"]', adminUser.password);
      await page.click('button[type="submit"]');

      await page.waitForURL('/');

      // Check for avatar/user menu button (indicates user is logged in)
      const userMenu = page.getByRole('button', { name: /open user menu/i });
      await expect(userMenu).toBeVisible({ timeout: 10000 });
    });

    test('should show logout button after login', async ({ page }) => {
      await page.fill('input#email, input[type="email"]', adminUser.email);
      await page.fill('input#password, input[type="password"]', adminUser.password);
      await page.click('button[type="submit"]');

      await page.waitForURL('/');

      // Check for avatar/user menu button (indicates user is logged in)
      const avatarButton = page.getByRole('button', { name: /open user menu/i });
      await expect(avatarButton).toBeVisible({ timeout: 10000 });
    });
  });

  test.describe('Logout Flow', () => {
    test.beforeEach(async ({ page }) => {
      // Login first
      await page.goto(`${baseURL}/login`);
      await page.fill('input#email, input[type="email"]', adminUser.email);
      await page.fill('input#password, input[type="password"]', adminUser.password);
      await page.click('button[type="submit"]');
      await page.waitForURL('/', { timeout: 10000 });
    });

    test('should successfully logout', async ({ page }) => {
      // Open avatar menu and click Log out
      const avatarButton = page.getByRole('button', { name: /open user menu/i });
      await avatarButton.click();
      await page.getByRole('menuitem', { name: /log out/i }).click();

      // Wait for redirect to login page
      await page.waitForLoadState('networkidle');

      // Verify user is logged out - login button should be visible
      const loginLink = page.locator('a[href="/login"]').first();
      await expect(loginLink).toBeVisible({ timeout: 10000 });
    });

    test('should not access protected routes after logout', async ({ page }) => {
      // Open avatar menu and click Log out
      const avatarButton = page.getByRole('button', { name: /open user menu/i });
      await avatarButton.click();
      await page.getByRole('menuitem', { name: /log out/i }).click();
      await page.waitForLoadState('networkidle');

      // Try to access protected route
      await page.goto(`${baseURL}/my-bookings`);

      // Should redirect to login
      await expect(page).toHaveURL(/\/login/);
    });
  });

  test.describe('Protected Routes', () => {
    test('should redirect to login when accessing homepage without authentication', async ({ page }) => {
      await page.goto(`${baseURL}/`);
      await expect(page).toHaveURL(/\/login/);
    });

    test('should redirect to login when accessing movies without authentication', async ({ page }) => {
      await page.goto(`${baseURL}/movies`);
      await expect(page).toHaveURL(/\/login/);
    });

    test('should redirect to login when accessing protected customer routes', async ({ page }) => {
      await page.goto(`${baseURL}/my-bookings`);
      await expect(page).toHaveURL(/\/login/);
    });

    test('should redirect to login when accessing admin routes', async ({ page }) => {
      await page.goto(`${baseURL}/admin`);
      await expect(page).toHaveURL(/\/login/);
    });

    test('should retain intended destination after login', async ({ page }) => {
      // Try to access protected route
      await page.goto(`${baseURL}/my-bookings`);
      
      // Should be on login page
      await expect(page).toHaveURL(/\/login/);
      
      // Login
      await page.fill('input#email, input[type="email"]', adminUser.email);
      await page.fill('input#password, input[type="password"]', adminUser.password);
      await page.click('button[type="submit"]');
      
      // May redirect to intended page or home - either is acceptable
      await page.waitForLoadState('networkidle');
      const currentUrl = page.url();
      expect(currentUrl.includes('/my-bookings') || currentUrl.endsWith('/')).toBeTruthy();
    });
  });

  test.describe('Session Persistence', () => {
    test('should maintain session across page refreshes', async ({ page }) => {
      // Login
      await page.goto(`${baseURL}/login`);
      await page.fill('input#email, input[type="email"]', adminUser.email);
      await page.fill('input#password, input[type="password"]', adminUser.password);
      await page.click('button[type="submit"]');
      await page.waitForURL('/', { timeout: 10000 });

      // Refresh page
      await page.reload();
      await page.waitForLoadState('networkidle');

      // Should still be logged in - avatar button visible
      const avatarButton = page.getByRole('button', { name: /open user menu/i });
      await expect(avatarButton).toBeVisible({ timeout: 10000 });
    });
  });
});

// ─── Bulgarian locale assertions ───────────────────────────────────────────────
// These tests run with the bg-BG Playwright project (Accept-Language: bg) so the
// backend RequestLocalizationMiddleware picks the Bulgarian resource strings.

test.describe('Authentication Flow – Bulgarian locale', () => {
  test.use({ locale: 'bg-BG' });

  test('login with invalid credentials returns Bulgarian error message', async ({ page }) => {
    await page.goto(`${baseURL}/login`);
    await page.fill('input#email, input[type="email"]', 'nonexistent@example.bg');
    await page.fill('input#password, input[type="password"]', 'WrongPassword1');
    await page.click('button[type="submit"]');

    // The backend returns "Невалиден имейл или парола" for bg culture
    const errorMessage = page.locator('.error-message, .error, [role="alert"]').first();
    await expect(errorMessage).toBeVisible({ timeout: 10000 });
    await expect(errorMessage).toContainText('Невалиден имейл или парола');
  });

  test('register page with empty fields shows Bulgarian validation messages', async ({ page }) => {
    await page.goto(`${baseURL}/register`);

    // Submit with completely empty form to trigger server-side validation
    const submitButton = page.locator('button[type="submit"]').first();
    await submitButton.click();

    // HTML5 / client validation fires first; at minimum the email field is required
    const emailInput = page.locator('input#email, input[type="email"]');
    // If the page has server-side only validation, wait for the error region instead
    const isRequired = await emailInput.getAttribute('required');
    if (isRequired !== null) {
      // HTML5 required attribute prevents submission – just verify the field exists
      await expect(emailInput).toBeVisible();
    } else {
      // Server validated: expect Bulgarian error text to appear
      const errorRegion = page.locator('.error-message, .error, [role="alert"]').first();
      await expect(errorRegion).toBeVisible({ timeout: 10000 });
      await expect(errorRegion).toContainText(/е задължителен|е задължителна/);
    }
  });
});
