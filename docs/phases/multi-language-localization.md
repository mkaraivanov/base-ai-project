# Multi-Language & Localization Support — Cinema Booking System

**Date:** 20 February 2026

---

## Overview

This plan introduces full-stack internationalization (i18n) and localization (l10n) for the cinema booking platform. English and Bulgarian will be supported initially. Backend error/validation messages and frontend UI strings will be translated. Users can switch language via a navbar picker, which persists their choice and propagates it to the backend for localized responses.

---

### Phases

#### Phase 1 — Backend Infrastructure

1. Add `Microsoft.Extensions.Localization` to Backend/Backend.csproj (confirm package reference).
2. Register localization services and middleware in Backend/Program.cs:
   - `builder.Services.AddLocalization(o => o.ResourcesPath = "Resources")`
   - `app.UseRequestLocalization(...)` with SupportedCultures `["en", "bg"]`, default `"en"`, AcceptLanguageHeaderRequestCultureProvider
3. Create Backend/Resources/SharedResource.cs (marker class).
4. Create Backend/Resources/SharedResource.resx (English) and SharedResource.bg.resx (Bulgarian) with all message keys from validators, services, and endpoints.

#### Phase 2 — Backend: Inject IStringLocalizer

5. Update all validators to accept `IStringLocalizer<SharedResource>` and use `.WithMessage(localizer["Key"])`.
6. Update all services to accept `IStringLocalizer<SharedResource>` and use `Result<T>.Failure(localizer["Key"])`.
7. Update endpoints to accept `IStringLocalizer<SharedResource>` and replace inline error strings.

#### Phase 3 — Frontend Infrastructure

8. Add `i18next`, `react-i18next`, `i18next-browser-languagedetector`, `i18next-http-backend` to frontend/package.json.
9. Create frontend/src/i18n.ts — initialize i18next with LanguageDetector, HttpBackend, supportedLngs `["en", "bg"]`, fallbackLng `"en"`, namespaces per feature.
10. Create translation files under frontend/public/locales/en/ and frontend/public/locales/bg/ (one JSON per namespace).
11. Wrap <App /> in frontend/src/main.tsx with <Suspense> and import "./i18n".

#### Phase 4 — Frontend: Extract Strings

12. Replace hardcoded strings in auth pages with `useTranslation("auth")` + `t("key")`.
13. Repeat for customer and admin pages using respective namespaces.
14. Extract common strings from App.tsx using `common` namespace.

#### Phase 5 — Language Switcher & API Integration

15. Create LanguageSwitcher.tsx — dropdown (EN/BG), calls `i18next.changeLanguage(lang)`, stores in localStorage, add to navbar.
16. Update API client to attach `Accept-Language: <lang>` to requests.
17. Update all date-fns format() calls to use locale object based on i18next.language.

#### Phase 6 — Tests

18. Add unit tests for validators asserting Bulgarian messages.
19. Add integration tests for services confirming Result.Error messages reflect culture.
20. Update playwright.config.ts for BG locale; add BG assertions to e2e/auth-flow.spec.ts and e2e/customer-booking-flow.spec.ts.

---

### Verification

- Backend: `dotnet test`
- Frontend: `npx tsc --noEmit && npm run dev`
- E2E: `npx playwright test`

Manual checks:
- Switch to Bulgarian in navbar → UI strings change
- Submit invalid login → validation error in Bulgarian
- Reload → Bulgarian persists
- Send Accept-Language: bg via curl → backend returns Bulgarian error

---

### Decisions

- Shared resource file for backend
- Namespace-per-feature JSON for frontend
- Accept-Language header for API calls
- i18next-http-backend for lazy loading

---

**End of plan**
