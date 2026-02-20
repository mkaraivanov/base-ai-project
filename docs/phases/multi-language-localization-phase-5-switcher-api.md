# Phase 5 — Language Switcher & API Integration

15. Create LanguageSwitcher.tsx — dropdown (EN/BG), calls `i18next.changeLanguage(lang)`, stores in localStorage, add to navbar.
16. Update API client to attach `Accept-Language: <lang>` to requests.
17. Update all date-fns format() calls to use locale object based on i18next.language.
