# Custom features — one customer, one folder

Logic written for a **single tenant** lives here, never in the shared services.

```
WMS.Application/Features/Custom/<feature-code>/     business logic
WMS.API/Controllers/Custom/                          controllers
```

Rules (enforced in `FeatureService.CreateAsync`, not just by convention):

1. The feature code starts with `custom.` — obvious in every list and grep.
2. `IsCustom = true` and `DefaultEnabled = false` — nobody gets it unless explicitly granted.
3. `OwnerTenantId` names the customer it was written for.
4. A custom feature can never be part of a plan (`PlanService` refuses it), only a
   per-tenant override.
5. Every custom controller carries `[RequireFeature("custom.<code>")]`.
6. Every custom feature is listed in `docs/CUSTOM_FEATURES.md` with the reason and the
   files it touches.

Why the ceremony: the first special request always looks like a harmless exception.
By the fifth one, nobody remembers who asked for what, which tenant depends on it, or
whether it is safe to delete — and the shared code is full of `if (tenantId == 7)`.
