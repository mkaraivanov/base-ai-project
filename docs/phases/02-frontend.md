# Phase 2 — Frontend

## 10. API Module
- Create frontend/src/api/auditApi.ts
- Methods: getAuditLogs, getAuditLogById, exportAuditLogsCsv

## 11. AuditLogsPage
- Create frontend/src/pages/admin/AuditLogsPage.tsx
- Filter bar: date-range, user, action, entity type, search
- Table: Timestamp, User, Entity Type, Entity ID, Action, IP Address, Actions
- Detail drawer/dialog: JSON diff for OldValues/NewValues
- Pagination, export button
- All strings via useTranslation('admin')

## 12. Routing
- Add route /admin/audit in frontend/src/App.tsx

## 13. Admin Navigation
- Add "Audit Logs" nav item in admin sidebar/nav
