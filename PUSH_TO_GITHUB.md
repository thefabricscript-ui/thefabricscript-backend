# Push to GitHub — Step by Step

## Prerequisites
- Git installed on your machine
- Access to https://github.com/thefabricscript-ui org (owner or member)

---

## Step 1: Create the repo on GitHub

1. Go to https://github.com/organizations/thefabricscript-ui/repositories/new
2. Repository name: `thefabricscript-backend`
3. Visibility: **Private**
4. Do NOT initialize with README (we already have one)
5. Click **Create repository**

---

## Step 2: Push from your local machine

Open a terminal, navigate to this folder, then run:

```bash
cd /path/to/TheFabricScript/thefabricscript-backend

git init
git add .
git commit -m "feat: initial backend scaffold — entities, infrastructure, API, admin controllers"

git remote add origin https://github.com/thefabricscript-ui/thefabricscript-backend.git
git branch -M main
git push -u origin main
```

---

## Step 3: Create the develop branch

```bash
git checkout -b develop
git push -u origin develop
```

---

## Step 4: Set up branch protection rules (on GitHub)

Go to: **Settings → Branches → Add rule**

For `main`:
- ✅ Require pull request before merging
- ✅ Require status checks to pass (select: `build-and-test`)
- ✅ Require branches to be up to date before merging
- ✅ Do not allow bypassing

For `develop`:
- ✅ Require pull request before merging
- ✅ Require status checks to pass

---

## Step 5: Add GitHub Secrets (for deploy pipeline)

Go to: **Settings → Secrets and variables → Actions → New repository secret**

| Secret Name | Value |
|---|---|
| `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` | Download from Azure App Service → Get publish profile |

---

## Branch Strategy

```
main          ← production only, protected
develop       ← staging, CI runs here
feature/*     ← dev work (e.g. feature/auth-service)
fix/*         ← bug fixes
```

**Workflow:**
`feature/xyz` → PR to `develop` → CI passes → merge → auto-deploy to staging  
`develop` → PR to `main` → manual approval → deploy to production

---

## GitHub Actions — What runs automatically

| Trigger | Workflow | What it does |
|---|---|---|
| PR to `main` or `develop` | `ci.yml` | Build + run all tests |
| Push to `develop` | `deploy-staging.yml` | Build Docker image, push to GHCR |
| Push to `main` | *(add deploy-prod.yml later)* | Deploy to production |
