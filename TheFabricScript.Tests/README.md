# TheFabricScript — Unit Tests

## Overview

All tests use **xUnit** + **FluentAssertions** + **EF Core InMemory** provider.
No mocking framework is needed for the repository layer because the real `Repository<T>`
and `UnitOfWork` are used against an in-memory database, exercising actual query logic.

`Moq` is available for any future service-layer tests that need to mock interfaces.

---

## Test Structure

```
TheFabricScript.Tests/
├── Helpers/
│   ├── TestDbContextFactory.cs   — Creates isolated InMemory AppDbContext per test
│   └── SeedData.cs               — Fixed GUIDs + pre-built entity factories + SeedBasicAsync()
│
├── Repositories/
│   ├── RepositoryTests.cs        — Tests for generic Repository<T> (CRUD + soft delete)
│   └── UnitOfWorkTests.cs        — Tests for atomic commits + timestamp auto-update
│
└── Controllers/
    ├── ProductsControllerTests.cs        — Public product listing, detail, search
    ├── AdminProductsControllerTests.cs   — Admin CRUD, slug uniqueness, stock, toggle
    ├── AdminCouponsControllerTests.cs    — Create, conflict, toggle, validate (discount calc)
    ├── AdminOrdersControllerTests.cs     — Status transitions, refund, shipment
    └── AdminCategoriesControllerTests.cs — Category CRUD + slug enforcement
```

---

## Running Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run a specific test class
dotnet test --filter "FullyQualifiedName~RepositoryTests"

# Run a specific test method
dotnet test --filter "FullyQualifiedName~GetProduct_ShouldReturn404_WhenSlugNotFound"

# Run with code coverage (requires coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

---

## Test Isolation

Each test that uses `TestDbContextFactory.Create()` (with no argument) gets its own
in-memory database keyed by a fresh GUID. Tests are safe to run in parallel.

Tests that need to share seeded data (e.g. a multi-step flow) can pass a shared
database name: `TestDbContextFactory.Create("shared-db-name")`.

---

## Adding New Tests

1. Create a new file in the appropriate folder (`Repositories/` or `Controllers/`).
2. Use `TestDbContextFactory.Create()` for isolation.
3. Call `await SeedData.SeedBasicAsync(context)` for a standard baseline dataset.
4. Use `SeedData.*Id` constants for deterministic assertions.
5. Follow the **Arrange / Act / Assert** pattern with a `<summary>` XML doc on each test method.

---

## Test Naming Convention

```
MethodName_ShouldExpectedBehaviour_WhenCondition
```

Examples:
- `GetProduct_ShouldReturn404_WhenSlugNotFound`
- `CreateCoupon_ShouldReturn409_WhenCodeAlreadyExists`
- `UpdateStatus_ShouldSetDeliveredAt_WhenStatusIsDelivered`

---

## What Is NOT Covered Here (Needs Integration Tests)

- JWT authentication middleware (token validation, role enforcement)
- SQL Server-specific constraints (unique indexes, FK cascade)
- Razorpay signature verification
- Email/SMS OTP delivery
- File upload (bulk CSV) end-to-end

These are tested in the CI pipeline's integration tests against a real SQL Server container.
