# The Fabric Script — Backend API

**Stack:** ASP.NET Core (.NET 10) · SQL Server · Entity Framework Core · JWT Auth

---

## Prerequisites

- .NET 10 SDK
- SQL Server (local or SQL Server Express)
- Git

---

## Quick Start

### 1. Clone the repo
```bash
git clone https://github.com/thefabricscript-ui/thefabricscript-backend.git
cd thefabricscript-backend
```

### 2. Configure connection string

Edit `TheFabricScript.API/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TheFabricScriptDb_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```
If using SQL Server Express: `Server=localhost\\SQLEXPRESS;...`

### 3. Restore packages
```bash
dotnet restore
```

### 4. Run EF Core migrations
```bash
cd TheFabricScript.API
dotnet ef migrations add InitialCreate --project ../TheFabricScript.Infrastructure
dotnet ef database update --project ../TheFabricScript.Infrastructure
```

### 5. Run the API
```bash
dotnet run --project TheFabricScript.API
```

API will be available at:
- `https://localhost:7001` (HTTPS)
- `http://localhost:5001` (HTTP)
- Swagger UI: `https://localhost:7001/swagger`

---

## Project Structure

```
TheFabricScript.sln
├── TheFabricScript.API/           # Web API entry point
│   ├── Controllers/               # Auth, Products, Cart, Orders, Admin...
│   ├── Middleware/                # Exception handling, logging
│   ├── Program.cs                 # DI wiring, middleware pipeline
│   └── appsettings.json           # Config (DO NOT commit secrets)
│
├── TheFabricScript.Core/          # Business logic (no dependencies)
│   ├── Entities/                  # Domain models
│   ├── Interfaces/                # IRepository, IUnitOfWork, service contracts
│   └── DTOs/                      # Data transfer objects
│
├── TheFabricScript.Infrastructure/ # Data access
│   ├── Data/                      # AppDbContext + entity configurations
│   ├── Repositories/              # Repository<T> + UnitOfWork
│   └── Migrations/                # EF Core migrations (auto-generated)
│
└── TheFabricScript.Tests/         # xUnit tests
```

---

## Key Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/auth/register` | Public | Email registration |
| POST | `/api/auth/login` | Public | Email login |
| POST | `/api/auth/google` | Public | Google OAuth |
| POST | `/api/auth/otp/send` | Public | Send OTP |
| POST | `/api/auth/otp/verify` | Public | Verify OTP |
| POST | `/api/auth/refresh` | Public | Refresh token |
| GET | `/api/products` | Public | Product list (filtered) |
| GET | `/api/products/{slug}` | Public | Product detail |
| GET | `/api/products/search?q=` | Public | Search products |
| GET | `/api/categories` | Public | Category tree |
| GET | `/api/cart` | JWT | User cart |
| POST | `/api/cart` | JWT | Add to cart |
| PUT | `/api/cart/{id}` | JWT | Update qty |
| DELETE | `/api/cart/{id}` | JWT | Remove item |

---

## Environment Variables to Configure

Fill in `appsettings.json` (or use environment variables / Azure Key Vault in production):

| Key | Description |
|-----|-------------|
| `Jwt:SecretKey` | 256-bit secret for JWT signing |
| `Google:ClientId` | Google OAuth client ID |
| `Razorpay:KeyId` | Razorpay API key |
| `Razorpay:KeySecret` | Razorpay secret |
| `Sms:ApiKey` | MSG91 API key |
| `Email:Password` | SMTP password |

---

## Next Steps

1. Implement `IAuthService` in Infrastructure layer
2. Add `OrdersController` with Razorpay integration
3. Add `AdminController` (product CRUD, order management)
4. Add `WishlistController`
5. Set up CI/CD with GitHub Actions
