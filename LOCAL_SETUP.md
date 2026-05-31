# Local Development Setup — The Fabric Script Backend

## Prerequisites Checklist

Before running, make sure all of these are installed:

| Tool | Check | Install |
|---|---|---|
| .NET SDK | `dotnet --version` → should show 10.x | https://dotnet.microsoft.com/download |
| SQL Server | `sqlcmd -?` or SQL Server Management Studio | https://aka.ms/sqledge (free) |
| Git | `git --version` | https://git-scm.com |

---

## Step 1 — Create the Database

**Option A: Run the SQL schema script** (quickest)

Open **SQL Server Management Studio** or **Azure Data Studio**, connect to `localhost`, then run:
```
docs/database/schema.sql
```
This creates `TheFabricScriptDb` and all tables with indexes.

**Option B: Use EF Core migrations** (generates tables automatically on `dotnet run`)

The app auto-migrates in Development mode — just make sure the DB server is running.
To generate the first migration manually:
```bash
cd TheFabricScript.API
dotnet ef migrations add InitialCreate --project ../TheFabricScript.Infrastructure
dotnet ef database update --project ../TheFabricScript.Infrastructure
```

---

## Step 2 — Configure Connection String

Edit `TheFabricScript.API/appsettings.Development.json`:

**If using SQL Server Express (local install):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=TheFabricScriptDb_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**If using SQL Server Developer / full local:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TheFabricScriptDb_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**If using SQL Server with username/password:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TheFabricScriptDb_Dev;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

---

## Step 3 — Configure JWT Secret

In `appsettings.Development.json`, add a strong JWT secret (min 32 characters):
```json
{
  "Jwt": {
    "SecretKey": "TheFabricScript_Dev_Secret_Key_2026_AtLeast32Chars!"
  }
}
```

---

## Step 4 — Restore & Run

```bash
# From the solution root (thefabricscript-backend/)
dotnet restore

# Run the API
dotnet run --project TheFabricScript.API
```

You should see output like:
```
[INF] Now listening on: https://localhost:7001
[INF] Now listening on: http://localhost:5001
[INF] Application started. Press Ctrl+C to shut down.
```

---

## Step 5 — Open Swagger UI

Open in your browser:
```
https://localhost:7001/swagger
```

or (HTTP):
```
http://localhost:5001/swagger
```

You'll see the full API documentation with all endpoints grouped by controller.

---

## Step 6 — Verify the API is Working

Run these quick checks:

### ✅ Check 1 — Health (no auth needed)
```
GET http://localhost:5001/api/categories
```
Expected: `200 OK` with `[]` (empty array — no data yet)

### ✅ Check 2 — Products list
```
GET http://localhost:5001/api/products
```
Expected: `200 OK` with `{ "total": 0, "page": 1, "pageSize": 24, "data": [] }`

### ✅ Check 3 — Auth required endpoint returns 401
```
GET http://localhost:5001/api/cart
```
Expected: `401 Unauthorized`

### ✅ Check 4 — Admin endpoint returns 401 without token
```
GET http://localhost:5001/api/admin/dashboard
```
Expected: `401 Unauthorized`

---

## Step 7 — Run Unit Tests

```bash
dotnet test --verbosity normal
```

All tests should pass. Expected output:
```
Passed! - Failed: 0, Passed: 35, Skipped: 0
```

---

## Using Swagger to Test Authenticated Endpoints

1. Open `https://localhost:7001/swagger`
2. Find `POST /api/auth/register` → click **Try it out**
3. Paste this body and click **Execute**:
   ```json
   {
     "firstName": "Test",
     "lastName": "User",
     "email": "test@example.com",
     "password": "Test@1234"
   }
   ```
4. Copy the `accessToken` from the response
5. Click the **Authorize 🔓** button at the top right of Swagger
6. Enter: `Bearer <paste your token here>`
7. Click **Authorize** → now all protected endpoints will include your token

---

## Common Issues & Fixes

### ❌ "A connection was successfully established with the server, but then an error occurred"
→ SQL Server is not running. Start it from **Services** (Windows) or:
```bash
# macOS (with SQL Server installed via Docker)
docker start sql-server-container
```

### ❌ "SSL connection error" or certificate warning
→ Add `TrustServerCertificate=True` to your connection string (already included in the templates above).

### ❌ "dotnet: command not found"
→ .NET SDK not installed or not in PATH. Install from https://dotnet.microsoft.com/download

### ❌ Port 7001 already in use
→ Change the port in `Properties/launchSettings.json` under `applicationUrl`.

### ❌ Swagger shows no XML comments (plain endpoint names only)
→ Make sure you built the project first: `dotnet build`
→ XML files are generated to `bin/Debug/net10.0/*.xml` and loaded at startup.

### ❌ "IpRateLimiting section not found" warning
→ Already added to `appsettings.json`. Make sure you saved the file and restarted the app.

---

## Environment Files Summary

| File | Purpose | Committed to Git? |
|---|---|---|
| `appsettings.json` | Base config + rate limit rules | ✅ Yes (no secrets) |
| `appsettings.Development.json` | Dev DB connection + overrides | ✅ Yes (no prod secrets) |
| `appsettings.Production.json` | Prod secrets | ❌ Never — use Azure Key Vault |

---

## Useful dotnet Commands

```bash
# Build only
dotnet build

# Run with hot reload
dotnet watch run --project TheFabricScript.API

# Add a new EF Core migration
dotnet ef migrations add MigrationName --project TheFabricScript.Infrastructure --startup-project TheFabricScript.API

# Apply pending migrations
dotnet ef database update --project TheFabricScript.Infrastructure --startup-project TheFabricScript.API

# List all migrations
dotnet ef migrations list --project TheFabricScript.Infrastructure --startup-project TheFabricScript.API

# Run tests with coverage report
dotnet test --collect:"XPlat Code Coverage"
```
