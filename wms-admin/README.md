# WMS Admin — Platform control plane

Separate Angular app for the **platform owner (SuperAdmin)** to manage the WMS SaaS.
This is NOT the tenant app (`wms-ui`) — it manages tenants, subscription plans and platform stats.

## What it does
- **Dashboard** — platform stats (tenants by status, users, monthly growth, recent signups).
- **Tenants** — list, create (full provisioning + admin account), edit, suspend/activate,
  assign a plan, per-tenant module toggle, delete.
- **Plans** — subscription plans: module set + limits (max users / warehouses / transfers).

## Access
Only users with `IsSuperAdmin = true` can log in (the login rejects normal tenant admins).
The seeded platform admin is on the system tenant (`admin` slug).

## Backend API
Consumes `/api/admin/*` (SuperAdmin-gated) + `/api/auth/login`. Same backend as `wms-ui`.

## Run (dev)
```bash
npm install
npm start          # http://localhost:7060  (backend expected at http://localhost:7040)
```
The backend CORS allows `http://localhost:7060` in development.

## Build (prod)
```bash
ng build --configuration production   # apiUrl = /api (served behind the same nginx as the API)
```
Deploy the `dist/` on its own subdomain (e.g. `admin.yourdomain.uz`) behind nginx,
proxying `/api` to the backend — separate from the tenant app host.

## Stack
Angular 21 (standalone, signals, zoneless), PrimeNG 21 (Aura), ApexCharts.
Shares `wms-ui`'s design system (`styles.scss`). English-only (internal console).
