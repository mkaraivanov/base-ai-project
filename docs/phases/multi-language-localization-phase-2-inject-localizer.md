# Phase 2 — Backend: Inject IStringLocalizer

5. Update all validators to accept `IStringLocalizer<SharedResource>` and use `.WithMessage(localizer["Key"])`.
6. Update all services to accept `IStringLocalizer<SharedResource>` and use `Result<T>.Failure(localizer["Key"])`.
7. Update endpoints to accept `IStringLocalizer<SharedResource>` and replace inline error strings.
