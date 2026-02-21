# Phase 1 — Backend Domain & Infrastructure

## 1. AuditLog Entity
- Create `AuditLog` record in Backend/Domain/Entities/AuditLog.cs
- Fields: Guid Id, string EntityName, string EntityId, string Action (Created/Updated/Deleted), Guid? UserId, string? UserEmail, string? UserRole, string? IpAddress, string? OldValues (JSON), string? NewValues (JSON), DateTime Timestamp

## 2. DbContext
- Add DbSet<AuditLog> to CinemaDbContext
- Configure entity: PK, indexes (Timestamp, UserId, EntityName, Action)
- Enforce immutability: no update/delete

## 3. EF Core Audit Interceptor
- Create AuditInterceptor (SaveChangesInterceptor)
- Capture Added/Modified/Deleted entities (skip AuditLog itself)
- Resolve user info from IHttpContextAccessor
- Exclude sensitive fields (e.g. PasswordHash)

## 4. Repository
- Create IAuditLogRepository and AuditLogRepository
- Only read methods: GetPagedAsync, GetByIdAsync

## 5. Service
- Create IAuditLogService and AuditLogService
- Methods: GetLogsAsync, GetLogByIdAsync, ExportCsvAsync

## 6. DTOs
- AuditLogDto, AuditLogFilterDto, PagedResult<T>

## 7. Endpoints
- MapAuditEndpoints: GET /api/audit (filter, pagination), GET /api/audit/{id}, GET /api/audit/export (CSV)
- RequireAuthorization("Admin")

## 8. DI Registration
- Register AuditInterceptor, repositories, services
- Add interceptor to DbContextOptions

## 9. EF Core Migration
- Add migration for AuditLogs table
