# StockFlow architecture

Dependency direction is `Core <- Application <- Infrastructure <- WebAPI/Worker`. Core owns entities and business vocabulary. Application owns contracts and use-case boundaries. Infrastructure owns PostgreSQL, security, files, email/report adapters. HTTP controllers remain thin; the Worker is independently deployable.

Transactional history uses restrictive foreign-key deletion. Only owned draft line items use database cascade, and application rules must reject deletion after processing. Master data is deactivated after use. Inventory-changing workflows must execute receipt/order state, movement audit, and product balance changes within one database transaction and lock affected product rows.

Large reports are queued in PostgreSQL. Workers claim one row with `FOR UPDATE SKIP LOCKED`, commit the claim, stream records to a file, and update progress/status. No complete dataset is materialized in memory.
