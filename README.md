# StockFlow

Production-oriented inventory, purchasing, sales, and reporting portfolio application. The API and background worker are separate processes while sharing Core, Application, and Infrastructure projects.

## Run the demo

Requirements: Docker Desktop with Compose and a local PostgreSQL 18.1 instance running on
`localhost:5432` with the `stockflow` database and `postgres` user/password. The API and worker
containers connect to that local PostgreSQL instance through `host.docker.internal`.

```bash
docker compose up --build
```

Open `http://localhost:5173`. API documentation is at `http://localhost:8080/swagger`; health is at `http://localhost:8080/health`.

Demo login: `admin@stockflow.local` / `StockFlow123!`

## Current implementation

- Clean Architecture boundaries and complete V1 domain model
- PostgreSQL EF Core model with foreign keys, safe delete behaviors, indexes, and seed data
- JWT authentication, role authorization, correlation IDs, structured logging, exception handling, CORS, health checks
- Dashboard and product/category/supplier/customer APIs, including Admin-only master-data CRUD (soft delete)
- Responsive Vue 3 + TypeScript + Tailwind shell, login, actionable dashboard, product inventory table, and separate Admin-only menus for Categories, Products, Suppliers, and Customers
- Separate idle-friendly worker with PostgreSQL `FOR UPDATE SKIP LOCKED` queue claim and streaming CSV export
- Separate Docker Compose containers for web, API, worker, and PostgreSQL

## Next slices

PO approval and atomic goods receipt, sales completion and stock deduction, report job request/download endpoints, notification scheduler, audit history views, and comprehensive tests.

See [architecture](docs/architecture.md) for design rules and status.
