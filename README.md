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
- Responsive Vue 3 + TypeScript + Tailwind shell, login, actionable dashboard, product inventory table, operational purchasing screens for purchase orders, goods receiving, and suppliers, plus separate Admin-only master-data menus
- Purchase order lifecycle APIs (Draft, Submitted, Approved, Received, Cancelled) with paginated search/filtering and atomic goods receipt updates for inventory and movement audit
- Separate idle-friendly worker with PostgreSQL `FOR UPDATE SKIP LOCKED` queue claim and queued CSV export
- Database constraints and row locking for concurrent inventory adjustments and goods receipts
- Automated unit/integration tests and GitHub Actions CI for backend, PostgreSQL concurrency, and frontend builds
- Separate Docker Compose containers for web, API, worker, and PostgreSQL

## Next slices

Sales completion and stock deduction, report job request/download endpoints, notification scheduler, audit history views, and broader end-to-end browser coverage.

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
`Jwt__Key` of at least 32 bytes, and `WebOrigin` through deployment secrets/environment variables.
Run EF migrations as a controlled release step. `Database__ApplyMigrations`, `SeedData__Demo`, and
`PasswordReset__ExposeResetToken` should remain `false` in production.

See [architecture](docs/architecture.md) for design rules and status.
