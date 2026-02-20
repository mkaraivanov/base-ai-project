# Phase 1 — Backend Infrastructure

1. Add `Microsoft.Extensions.Localization` to Backend/Backend.csproj (confirm package reference).
2. Register localization services and middleware in Backend/Program.cs:
   - `builder.Services.AddLocalization(o => o.ResourcesPath = "Resources")`
   - `app.UseRequestLocalization(...)` with SupportedCultures `["en", "bg"]`, default `"en"`, AcceptLanguageHeaderRequestCultureProvider
3. Create Backend/Resources/SharedResource.cs (marker class).
4. Create Backend/Resources/SharedResource.resx (English) and SharedResource.bg.resx (Bulgarian) with all message keys from validators, services, and endpoints.
