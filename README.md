# GoldLedger 🪙

**Clean Architecture, Domain-Driven Design, and reliable concurrency control in .NET.**

GoldLedger is a gold trading backend built with **.NET 10**, **ASP.NET Core**, and **Entity Framework Core**. It demonstrates how a rich domain model, explicit business rules, and transactional persistence work together to maintain consistency across trading operations.

The system supports gold wallets, buy and sell orders, balance reservations, and internal settlement.

## Architecture

GoldLedger follows Clean Architecture with clear boundaries between business logic, application workflows, and infrastructure.

| Layer | Responsibility |
|-------|----------------|
| **Domain** | Aggregates, value objects, business rules, and domain events |
| **Application** | Use cases, service orchestration, and application contracts |
| **Infrastructure** | Persistence, repositories, event processing, and settlement |
| **API** | HTTP endpoints, request handling, and OpenAPI documentation |

The domain remains independent of persistence and transport concerns. Business validation is enforced by domain objects, while application services coordinate operations.

## Domain-Driven Design

### Aggregates

**GoldOrder** manages order creation, pricing, fees, metadata, and valid transitions between pending, settled, and cancelled states.

**GoldWallet** manages total, reserved, and available gold balances. Its operations enforce balance constraints during deposits, withdrawals, reservations, and settlement.

### Value Objects

Dedicated value objects make business concepts explicit:

- **GoldWeight** — Gold quantities and weight calculations
- **Money** — Monetary amounts and rounding rules
- **Percentage** — Fee percentages and related calculations
- **Tag** — Validated and normalized order tags

### Domain Events

Aggregates raise events to represent business changes. Event processing coordinates internal settlement and persists event records within the same transaction as the associated order and wallet changes.

## Concurrency and Consistency

Trading operations must remain consistent when multiple requests access the same state.

GoldLedger uses **optimistic concurrency control** with application-managed concurrency tokens on orders and wallets. Stale updates are rejected instead of silently overwriting committed changes. Changes to order tags also participate in concurrency checks.

Order transitions, wallet updates, settlement records, and event records are committed atomically. Failed operations roll back their changes, preventing partial settlement or inconsistent balances.

## Idempotent Requests

Mutating endpoints use an `Idempotency-Key` to prevent repeated execution when clients retry requests.

- Identical retries return the original successful response.
- Reusing a key with different input returns a conflict.
- Business changes and the successful response are stored in one transaction.
- Failed requests remain eligible for retry.

This protects operations such as deposits, withdrawals, and order creation from accidental duplication.

## Error Handling

Expected business failures are represented through the **Result pattern**, keeping success and failure paths explicit.

Centralized exception handling provides consistent API error responses without exposing internal implementation details.

## Persistence

GoldLedger uses **EF Core and SQLite**, with separate entity configurations and versioned database migrations.

Internal settlement records provide a transactional record of completed trades. Settlement represents internal accounting; external payment processing is outside its scope.

## Testing

The test suite includes **243 test cases** across domain, application, persistence, and API layers.

Coverage includes:

- Business rules and financial calculation boundaries
- Order lifecycle and wallet balance constraints
- Concurrent updates and competing order transitions
- Concurrent withdrawals and balance protection
- Transaction rollback and settlement consistency
- Database migrations and persistence behavior
- Request replay and idempotency conflicts
- HTTP integration tests using `WebApplicationFactory`

## Technology Stack

.NET 10 · ASP.NET Core Web API · Entity Framework Core · SQLite · xUnit · Swagger / OpenAPI

## Source Code

[Explore GoldLedger on GitHub](https://github.com/mohammadrezalamei/GoldLedger/)
