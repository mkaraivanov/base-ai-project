# E2E Test Instructions

## Running E2E Tests

1. Ensure your app is running locally (e.g., `npm run dev` or `dotnet run` for backend).
2. In a separate terminal, run:

```
npx playwright test
```

## Structure
- All E2E tests are in the `e2e/` directory.
- Main flows are covered in `main-flows.spec.ts`.

## Adding More Tests
- Add new `.spec.ts` files in the `e2e/` directory for additional flows.
- See [Playwright docs](https://playwright.dev/docs/test-intro) for more info.
