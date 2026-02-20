# Multi-Language & Localization Support — Cinema Booking System

**Date:** 20 February 2026

## Overview

This plan introduces full-stack internationalization (i18n) and localization (l10n) for the cinema booking platform. English and Bulgarian will be supported initially. Backend error/validation messages and frontend UI strings will be translated. Users can switch language via a navbar picker, which persists their choice and propagates it to the backend for localized responses.

## Phases

See the following docs for each phase:
- multi-language-localization-phase-1-backend.md
- multi-language-localization-phase-2-inject-localizer.md
- multi-language-localization-phase-3-frontend-infra.md
- multi-language-localization-phase-4-frontend-extract.md
- multi-language-localization-phase-5-switcher-api.md
- multi-language-localization-phase-6-tests.md

## Verification

- Backend: `dotnet test`
- Frontend: `npx tsc --noEmit && npm run dev`
- E2E: `npx playwright test`

Manual checks:
- Switch to Bulgarian in navbar → UI strings change
- Submit invalid login → validation error in Bulgarian
- Reload → Bulgarian persists
- Send Accept-Language: bg via curl → backend returns Bulgarian error

## Decisions

- Shared resource file for backend
- Namespace-per-feature JSON for frontend
- Accept-Language header for API calls
- i18next-http-backend for lazy loading

**End of overview**
