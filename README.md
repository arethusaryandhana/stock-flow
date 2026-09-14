# StockFlow

Production-oriented inventory, purchasing, sales, and reporting portfolio application. The API and background worker are separate processes while sharing Core, Application, and Infrastructure projects.

## Run the demo

Requirement: Docker Desktop with Compose. The Compose stack starts PostgreSQL and persists its
data in the `stockflow_db` Docker volume.

```bash
docker compose up --build
```

Open `http://localhost:5173`. API documentation is at `http://localhost:8080/swagger`; health is at `http://localhost:8080/health`.

Demo login: `admin@stockflow.local` / `StockFlow123!`

## Current implementation

- Clean Architecture boundaries and complete V1 domain model
- PostgreSQL EF Core model with foreign keys, safe delete behaviors, indexes, and seed data
- HttpOnly-cookie/Bearer JWT authentication, server-side session revocation, role authorization, auth rate limiting, correlation IDs, structured logging, exception handling, CORS, health checks
- Dashboard and product/category/supplier/customer APIs, including Admin-only master-data CRUD (soft delete)
- Responsive Vue 3 + TypeScript + Tailwind shell, login, actionable dashboard, product inventory table, operational purchasing and sales screens, plus separate Admin-only master-data menus
- Purchase order lifecycle APIs (Draft, Submitted, Approved, Received, Cancelled) with paginated search/filtering and atomic goods receipt updates for inventory and movement audit
- Sales order lifecycle APIs (Draft, Confirmed, Processing, Completed, Cancelled) with paginated search/filtering and atomic stock deduction on completion
- Report page and authenticated request/status/download APIs backed by an idle-friendly worker and PostgreSQL `FOR UPDATE SKIP LOCKED` queue claims
- Per-user notification center with unread state, ownership checks, report-ready events, deduplicated low-stock alerts, and account-synced delivery/category/sound/refresh preferences
- Account and security settings for profile updates, password changes, role visibility, and server-side revocation of every active session
- Display and regional settings for language, theme, time zone, date and number formats, and a shared default table page size
- Admin-only inventory settings for product defaults, effective global low-stock warnings, and optional negative-stock transactions
- Admin-only company profile for business identity, contacts, primary currency, and report/document branding
- Admin-only user management for account creation, role assignment, active status, password reset, and last-admin protection
- Immutable Admin audit history for business entities, including actor and safe before/after field values without authentication secrets
- Database constraints and row locking for concurrent inventory adjustments, goods receipts, and sales completion
- Automated unit/integration tests and GitHub Actions CI for backend, PostgreSQL concurrency, and frontend builds
- Separate Docker Compose containers for web, API, worker, and PostgreSQL

## Next slices

Additional report types, email notification delivery, and broader end-to-end browser coverage.

## Run tests

Backend unit tests run without external services:

```bash
dotnet test backend/StockFlow.sln
```

PostgreSQL integrity tests activate when `STOCKFLOW_TEST_CONNECTION` points to a disposable
database whose name contains `test`. The test suite recreates that database. CI supplies
`stockflow_tests` automatically.

```powershell
$env:STOCKFLOW_TEST_CONNECTION = "Host=localhost;Port=5432;Database=stockflow_tests;Username=postgres;Password=postgres;Pooling=false"
dotnet test backend/StockFlow.sln
```

## Production configuration

Production does not auto-migrate or seed demo data. Supply `ConnectionStrings__Database`, a unique
`Jwt__Key` of at least 32 bytes, `WebOrigin`, a shared `ReportStorage` path, and an optional
`Notifications__LowStockIntervalMinutes` (default `5`) through deployment
secrets/environment variables. The worker needs write access to report storage while the API only
needs read access for downloads.
JWT browser sessions and bearer tokens are valid for 8 hours by default; override this with
`Jwt__LifetimeMinutes` when a different lifetime is required (maximum `480` minutes).
Run EF migrations as a controlled release step. `Database__ApplyMigrations`, `SeedData__Demo`, and
`PasswordReset__ExposeResetToken` should remain `false` in production.

See [architecture](docs/architecture.md) for design rules and status.
