# GoldLedger

GoldLedger is a portfolio sample of a gold trading ledger built with ASP.NET Core and C#. It demonstrates how a small transactional domain can be organized with Clean Architecture and Domain-Driven Design (DDD), while keeping business outcomes explicit through the Result Pattern and protecting shared in-memory state with thread-safe repository operations.

## Architecture

The solution is separated into focused projects:

- **GoldLedger.Domain** â€” domain entities, aggregates, value objects, domain events, repository contracts, and the reusable `Result` / `Result<T>` types.
- **GoldLedger.Application** â€” wallet and order use cases, application services, DTOs, and service contracts.
- **GoldLedger.Infrastructure** â€” in-memory persistence, repositories, and unit-of-work implementation.
- **GoldLedger.API** â€” ASP.NET Core controllers and composition root, with Swagger for exploring the API.
- **GoldLedger.Tests** â€” API integration tests and infrastructure repository tests.

## Concepts demonstrated

- **Clean Architecture:** dependencies are arranged so application and infrastructure concerns are separated from the domain model.
- **Domain-Driven Design:** gold orders and wallets are modeled around domain concepts, with value objects, aggregate roots, and domain events.
- **Result Pattern:** operations express success or failure as values, including error information, rather than relying on exceptions for expected business outcomes.
- **Thread-safe in-memory concurrency:** singleton in-memory repositories coordinate access to shared state for concurrent requests. This keeps the sample self-contained; the in-memory store is not intended as durable production persistence.

## Run locally

Requires the .NET SDK compatible with the target framework declared in the project files.

```bash
dotnet restore GoldLedger.slnx
dotnet build GoldLedger.slnx
dotnet test GoldLedger.slnx
dotnet run --project GoldLedger.API/GoldLedger.API.csproj
```

The API exposes Swagger UI at the application root when running locally.

## Scope

This repository is intended as an architecture and implementation portfolio sample for gold wallet and buy/sell order flows. It preserves the existing project behavior and does not add unrelated features. The in-memory persistence is volatile and suitable for demonstration/testing rather than production use.

