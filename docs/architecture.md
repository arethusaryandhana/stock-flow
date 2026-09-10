# StockFlow architecture

Dependency direction is `Core <- Application <- Infrastructure <- WebAPI/Worker`. Core owns entities and business vocabulary. Application owns contracts and use-case boundaries. Infrastructure owns PostgreSQL, security, files, email/report adapters. HTTP controllers remain thin; the Worker is independently deployable.

PostgreSQL tables are partitioned by bounded context instead of living in `public`: `identity` contains roles, users, password-reset tokens, and notifications; `master` contains categories, products, suppliers, and customers; `purchasing` contains purchase orders and goods receipts; `sales` contains sales orders; `inventory` contains stock movements and adjustments; and `reporting` contains report export jobs. EF migration history is also stored in `identity`.

Transactional history uses restrictive foreign-key deletion. Only owned draft line items use database cascade, and application rules must reject deletion after processing. Master data is deactivated after use. Inventory-changing workflows must execute receipt/order state, movement audit, and product balance changes within one database transaction and lock affected product rows.

Stock adjustments follow the same rule as goods receipts: the repository starts a transaction,
locks the target product with `FOR UPDATE`, recalculates the balance, and writes the adjustment,
movement, and product balance atomically. Database check constraints provide a final guard against
negative balances and invalid quantities.

Browser sessions use an `HttpOnly`, `SameSite=Strict` JWT cookie. Bearer tokens remain supported for
non-browser API clients. Each JWT contains the user's token version; changing or resetting a
password increments that version so previously issued sessions are rejected. Authentication
endpoints are rate-limited by client address.

Large reports are queued in PostgreSQL. Workers claim one row with `FOR UPDATE SKIP LOCKED`, commit the claim, write records to a file, and update progress/status. Batch/streamed database reads remain a future optimization for very large exports.

List endpoints use server-side pagination. Products, categories, suppliers, customers, purchase orders, goods receipts, stock movements, and stock adjustments accept `page` and `pageSize` (capped at 100), plus their supported search/filter parameters (`search`; products also support `status` and `categoryId`; purchase orders also support `status`; movements support `type` and `periodDays`). Responses include `items`, `page`, `pageSize`, `totalCount`, and `totalPages`; filtering is applied before `Skip/Take` so the API never loads the complete table for a normal screen request. The purchase-order response also includes `statusCounts`, calculated by the API in one aggregate query after search filtering and before status filtering, so the UI can render every status tab count without separate requests.

Operations exposes purchase-order and goods-receipt workflows. Purchase orders move from Draft to Submitted to Approved, while approved orders can be received partially or completely. Goods receipt processing runs in one database transaction, locks affected product rows before recalculating balances, writes receipt items and inventory movements together, and marks the order Received only when all ordered quantities have arrived.
