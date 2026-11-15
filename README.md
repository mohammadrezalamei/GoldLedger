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

## Request retries and errors

Every POST and PATCH requires an Idempotency-Key header. Generate a unique key per logical operation, and reuse it when retrying that operation. Keys contain 1–128 ASCII letters, digits, dots, hyphens or underscores. For example:

```http
POST /api/wallets/user/11111111-1111-1111-1111-111111111111/deposit
Content-Type: application/json
Idempotency-Key: deposit-20261115-001

{"amount": 1.5}
```

The key is global across mutation endpoints. The fingerprint includes HTTP method, route and bound request arguments. Reusing a key with different input returns 409. A matching retry returns the original successful body, status and Location, with Idempotency-Replayed: true. First execution returns false. GET requests do not require a key.

Business changes and the successful response are committed in one SQLite transaction. Concurrent writes wait for SQLite's write lock; lock timeouts return 409 and can be retried with the same key. Domain concurrency tokens also protect callers that execute application services directly. Failed responses, exceptions and cancellation roll back the outer transaction and do not reserve the key. Successful keys are retained without expiry; deleting a record would permit that operation to execute again.

Missing or invalid keys return 422 through domain validation. Malformed request bodies return 400. Unexpected errors, including read failures, return a sanitized Result envelope and X-Trace-Id; diagnostic details remain in server logs.

Buy orders whose total rounds to zero are rejected. Monetary rounding follows Money's midpoint-to-even policy; a sale with a 100% fee can have zero net proceeds.

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

The AddRequestIdempotency migration adds durable request-response records. Relative SQLite paths are resolved against the API content root.

The EnforceLedgerIntegrityAndSettlement migration adds settlement/event tables and database constraints. Existing invalid records must be corrected before this migration can succeed. Existing settled orders are not backfilled with invented payment records.

## Verification

```powershell
dotnet test GoldLedger.slnx
```

Tests cover real file-backed SQLite migrations, stale writes, concurrent completion/cancellation, tag concurrency, internal settlement, transaction rollback and HTTP retries through WebApplicationFactory. The CI workflow builds and tests Release on Windows and Linux.

Source files use UTF-8 and CRLF line endings.
