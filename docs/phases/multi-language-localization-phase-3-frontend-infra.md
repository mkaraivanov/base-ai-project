# Phase 3 — Frontend Infrastructure

8. Add `i18next`, `react-i18next`, `i18next-browser-languagedetector`, `i18next-http-backend` to frontend/package.json.
9. Create frontend/src/i18n.ts — initialize i18next with LanguageDetector, HttpBackend, supportedLngs `["en", "bg"]`, fallbackLng `"en"`, namespaces per feature.
10. Create translation files under frontend/public/locales/en/ and frontend/public/locales/bg/ (one JSON per namespace).
11. Wrap <App /> in frontend/src/main.tsx with <Suspense> and import "./i18n".
