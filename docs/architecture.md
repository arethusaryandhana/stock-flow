# StockFlow architecture

Dependency direction is `Core <- Application <- Infrastructure <- WebAPI/Worker`. Core owns entities and business vocabulary. Application owns contracts and use-case boundaries. Infrastructure owns PostgreSQL, security, files, email/report adapters. HTTP controllers remain thin; the Worker is independently deployable.

PostgreSQL tables are partitioned by bounded context instead of living in `public`: `identity` contains roles, users, password-reset tokens, notifications, and immutable business audit logs; `master` contains the company profile, categories, products, suppliers, and customers; `purchasing` contains purchase orders and goods receipts; `sales` contains sales orders; `inventory` contains stock movements and adjustments; and `reporting` contains report export jobs. EF migration history is also stored in `identity`.

Transactional history uses restrictive foreign-key deletion. Only owned draft line items use database cascade, and application rules must reject deletion after processing. Master data is deactivated after use. Inventory-changing workflows must execute receipt/order state, movement audit, and product balance changes within one database transaction and lock affected product rows.

Stock adjustments follow the same rule as goods receipts: the repository starts a transaction,
locks the target product with `FOR UPDATE`, recalculates the balance, and writes the adjustment,
movement, and product balance atomically. Database check constraints provide a final guard against
negative balances and invalid quantities.

All protected API requests require a JWT in the `Authorization: Bearer` header. The web client
stores the token in session storage, or local storage when "Remember me" is selected. Cookies
cannot authenticate API requests. Each JWT contains the user's token version; changing or resetting a
password increments that version so previously issued sessions are rejected. Authentication
endpoints are rate-limited by client address.

Large reports are queued in PostgreSQL. Workers claim one row with `FOR UPDATE SKIP LOCKED`, commit the claim, write records to a temporary UTF-8 CSV, atomically rename it, and update progress/status. A partial unique index allows only one queued or processing job per user and report type, including under concurrent requests. Processing jobs older than 15 minutes are returned to the queue after a worker restart. The API lists only the current user's jobs and accepts a download only when the stored file resolves to the exact expected path under the configured report directory. API and worker share that directory; the Docker API mount is read-only. Product-stock CSV exports include the current company name, primary currency, and logo URL as metadata comments. Batch/streamed database reads remain a future optimization for very large exports.

Notifications are always scoped to the authenticated owner at repository level. Report completion writes a report-ready notification in the same save as the completed job. A second worker cycle scans active products at or below their effective warning threshold and creates alerts for active Admin and Manager users. Daily per-user/per-product deduplication keys are protected by a filtered unique database index, so retries and multiple worker instances cannot duplicate an alert. Read timestamps support both individual and mark-all flows.

Inventory settings are stored as one server-side configuration row and can only be read or changed through the Admin-only settings endpoint. Product creation falls back to the configured unit and reorder level when an API client omits them. The effective low-stock threshold is the greater of the global warning threshold and the product-specific reorder level, so global policy changes do not overwrite product data. Negative stock remains blocked by default; when an Admin explicitly enables it, both sales completion and stock adjustments can cross zero while movement and audit records continue to capture the resulting balance.

Business audit logs are appended from EF Core's tracked changes in the same transaction as the business write. The allowlist covers the company profile, inventory settings, products, categories, suppliers, customers, purchase orders, goods receipts, sales orders, and stock adjustments. Identity, password hashes, token versions, notification preferences, password-reset, report-job, and generated stock-movement entities are deliberately excluded, preventing secrets from entering audit JSON. Only Admin users can query audit history; stored audit rows have no update or delete API.

The company profile is a singleton configuration row. Authenticated users can read it for report/document rendering, while only Admin users can change it from Settings. It stores the business name, address, contact channels, primary currency (`IDR`, `USD`, `SGD`, `MYR`, or `EUR`), and an optional HTTP/HTTPS logo URL. The Settings screen validates and saves these values; reports and transaction documents can consume the same profile without duplicating identity data.

User management is available only to Admin users from Settings. The account directory supports server-side search, role and active-status filters, account creation, profile and role changes, admin-set password rotation, and activation/deactivation. The use case prevents an Admin from disabling or changing their own account and rejects changes that would leave the system without an active Admin. User and authentication fields remain outside the business audit trail by design.

List endpoints use server-side pagination. Products, categories, suppliers, customers, purchase orders, sales orders, goods receipts, stock movements, stock adjustments, report jobs, notifications, and audit logs accept `page` and `pageSize` (capped at 100), plus their supported search/filter parameters (`search`; products also support `status` and `categoryId`; purchase orders, sales orders, and reports also support `status`; movements support `type` and `periodDays`; audit logs support `entityType` and `action`). Responses include `items`, `page`, `pageSize`, `totalCount`, and `totalPages`; filtering is applied before `Skip/Take` so the API never loads the complete table for a normal screen request. Purchase-order, sales-order, and report responses also include `statusCounts`, calculated by the API before status filtering, so the UI can render every status tab count without separate requests. Notification responses additionally include the owner's total unread count.

Operations exposes purchase-order and goods-receipt workflows. Purchase orders move from Draft to Submitted to Approved, while approved orders can be received partially or completely. Goods receipt processing runs in one database transaction, locks affected product rows before recalculating balances, writes receipt items and inventory movements together, and marks the order Received only when all ordered quantities have arrived.

Sales orders move from Draft to Confirmed to Processing to Completed. Completion locks the sales
order and all affected products in deterministic order, checks availability, deducts every item,
writes Sale movements, and records `completed_at` in one transaction. Concurrent completions
therefore cannot oversell the same stock; an order with insufficient stock remains Processing.
