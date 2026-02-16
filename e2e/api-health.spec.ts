import { test, expect } from '@playwright/test';

test.describe('API Health Check', () => {
  test('Backend health endpoint returns 200', async ({ request }) => {
    const response = await request.get('http://localhost:5076/health');
    expect(response.status()).toBe(200);
  });
});
