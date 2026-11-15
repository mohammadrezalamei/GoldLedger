# GoldLedger

GoldLedger is a gold wallet and order sample using .NET 10, ASP.NET Core, EF Core and SQLite.

## Architecture

- GoldLedger.Domain contains aggregates, value objects, validation, domain events and repository contracts.
- GoldLedger.Application coordinates wallet and order operations. DTOs carry data; validation belongs to the domain.
- GoldLedger.Infrastructure implements SQLite persistence, internal settlement, event dispatch and the Tehran clock.
- GoldLedger.API exposes HTTP endpoints and Swagger.
- GoldLedger.Tests covers domain behavior, persistence, services and HTTP endpoints.

## Transactions and concurrency

All application mutations run through a shared operation guard. Failed results, exceptions and cancellation discard tracked changes. UnitOfWork also discards changes when preparing or saving a transaction fails.

Wallets and orders use application-managed concurrency tokens. Updating owned order tags also updates the order token. Stale updates and duplicate keys return Conflict; callers must reload state before retrying. SQLite lock contention also returns Conflict when it occurs during UnitOfWork execution.

Domain events are dispatched before the final save. Creation events are recorded in DomainEventRecords. A completion event stages an internal PaymentSettlement. The order, wallet, settlement and event records are persisted by the same SaveChanges transaction. Events are cleared only after a successful save or when failed changes are discarded.

PaymentSettlements has one entry per order. Matching settlement requests are idempotent; inconsistent repeats fail. Buy entries record the debit including fees; sell entries record the credit after fees. This is an internal ledger, not a bank/payment gateway integration. Event records are an audit log, not an external-message outbox. External integrations would need a separate reliable delivery mechanism.

Use UnitOfWork for application writes so that event handling and settlement run. Direct DbContext.SaveChanges is reserved for infrastructure work.

## Time

Production services use an injected clock backed by TimeProvider. Dates are stored in UTC; order response timestamps include the Asia/Tehran offset. Domain methods accept an explicit occurrence time for deterministic callers.

## Run locally

Run these commands from the solution directory in PowerShell. The absolute database path ensures the API and migration tool use the same file.

```powershell
dotnet restore GoldLedger.slnx
$env:ConnectionStrings__GoldLedger = "Data Source=$PWD/GoldLedger.API/goldledger.db"
dotnet ef database update --project GoldLedger.Infrastructure --startup-project GoldLedger.API
dotnet run --project GoldLedger.API
```

Swagger UI is available at the application root. Database migrations are explicit; the API does not create or migrate tables at startup.

The EnforceLedgerIntegrityAndSettlement migration adds settlement/event tables and database constraints. Existing invalid records must be corrected before this migration can succeed. Existing settled orders are not backfilled with invented payment records.

## Verification

```powershell
dotnet test GoldLedger.slnx
```

Source files use UTF-8 and CRLF line endings.
