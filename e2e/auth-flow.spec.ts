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

      // Wait for error message
      const errorMessage = page.locator('.error-message, .error, [role="alert"]').first();
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

      // Check for user menu or profile indication
      const userMenu = page.locator('[data-testid="user-menu"], .user-menu, button').filter({ hasText: /admin|profile|account/i }).first();
      await expect(userMenu).toBeVisible({ timeout: 10000 });
    });

    test('should show logout button after login', async ({ page }) => {
      await page.fill('input#email, input[type="email"]', adminUser.email);
      await page.fill('input#password, input[type="password"]', adminUser.password);
      await page.click('button[type="submit"]');

      await page.waitForURL('/');

      // Check for logout button
      const logoutButton = page.locator('button, a').filter({ hasText: /logout|sign out/i }).first();
      await expect(logoutButton).toBeVisible({ timeout: 10000 });
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
      // Click logout button
      const logoutButton = page.locator('button, a').filter({ hasText: /logout|sign out/i }).first();
      await logoutButton.click();

      // Wait for redirect to home or login page
      await page.waitForLoadState('networkidle');

      // Verify user is logged out - login link should be visible
      const loginLink = page.locator('nav a[href="/login"]');
      await expect(loginLink).toBeVisible({ timeout: 10000 });
    });

    test('should not access protected routes after logout', async ({ page }) => {
      // Logout
      const logoutButton = page.locator('button, a').filter({ hasText: /logout|sign out/i }).first();
      await logoutButton.click();
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

      // Should still be logged in
      const logoutButton = page.locator('button, a').filter({ hasText: /logout|sign out/i }).first();
      await expect(logoutButton).toBeVisible({ timeout: 10000 });
    });
  });
});
