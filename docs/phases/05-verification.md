# Phase 5 — Verification

## Backend
- dotnet build
- dotnet test
- dotnet ef migrations add AddAuditLog --project Backend --startup-project Backend
- dotnet run --project Backend
- POST a movie as admin → GET /api/audit → verify row appears

## Frontend
- cd frontend && npm run dev
- Navigate to /admin/audit — verify filter/search/export work

## E2E
- npx playwright test e2e/admin-audit.spec.ts
