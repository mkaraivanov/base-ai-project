---
paths:
  - "**/*.cs"
---
# C# Security

> Principles only. Full code examples: `@skill: dotnet-security`
> This file extends [common/security.md](../common/security.md) with C# specific content.

## Secret Management

- Inject secrets via `IConfiguration` — never hardcode
- Use `dotnet user-secrets` in development; environment variables in production
- Throw `InvalidOperationException` at startup if a required key is missing
- `appsettings.json` must never contain real secret values

## Input Validation

- Prefer **FluentValidation** for complex validation logic; use Data Annotations for simple DTOs
- Register `AddFluentValidationAutoValidation()` so `[ApiController]` handles errors automatically
- Return `400 Bad Request` with structured error details on validation failure

## SQL Injection Prevention

- Use EF Core LINQ queries exclusively for data access — parameterized by default
- When raw SQL is unavoidable, always use `FromSqlRaw("{0}", param)` never string interpolation

## Authentication & Authorization

- Use JWT bearer tokens; validate issuer, audience, lifetime, and signing key
- Apply `[Authorize]` or `.RequireAuthorization()` on every non-public endpoint
- Define named authorization policies for role/claim requirements

## CORS

- Register CORS **before** `UseRouting()` and `UseAuthentication()`
- Load allowed origins from `IConfiguration`, never hardcode URLs
- Restrict origins/methods/headers to what the frontend actually needs

## Rate Limiting

- Apply `AddRateLimiter` + `UseRateLimiter()` on all public endpoints
- Use fixed or sliding window; configure via `IConfiguration` not constants

## Security Headers

- Enable `UseHttpsRedirection()` and `UseHsts()` in production
- Add `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, CSP headers via middleware

## Security Checklist

- [ ] No hardcoded secrets (use configuration + environment variables)
- [ ] All user inputs validated (FluentValidation or DataAnnotations)
- [ ] SQL injection prevented (EF Core parameterized queries)
- [ ] XSS prevention (proper encoding, CSP headers)
- [ ] CSRF protection enabled for cookie auth
- [ ] Authentication implemented (JWT or Cookie)
- [ ] Authorization policies configured
- [ ] Rate limiting on all public endpoints
- [ ] CORS properly configured (restrict origins)
- [ ] HTTPS enforced (UseHttpsRedirection, HSTS)
- [ ] Security headers configured
- [ ] Error messages don't leak sensitive data
- [ ] Logging excludes sensitive information

## Agent Support

- Use **security-reviewer** agent for comprehensive security audits before commits

