# Local Development Setup

How to run the full stack locally with a single command.

---

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| Node.js | 18+ | Run npm scripts and frontend dev server |
| .NET SDK | 10.0 | Run the ASP.NET backend |
| PostgreSQL | 15+ | Database (local or Docker) |

### Quick PostgreSQL via Docker

If you don't have PostgreSQL installed locally:

```bash
docker run --name finance-db \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=finance_tracker_dev \
  -p 5432:5432 \
  -d postgres:16
```

---

## Running Everything at Once

From the **project root**:

```bash
npm run dev
```

This starts both servers in a single terminal using [`concurrently`](https://github.com/open-cli-tools/concurrently). Output is prefixed and colour-coded so you can tell them apart:

```
[api] info: Microsoft.Hosting.Lifetime — Now listening on: http://localhost:5194
[web] VITE v7.x  ready in 300ms
[web] ➜  Local:   http://localhost:3000/
```

Stop both at once with `Ctrl+C`.

---

## Running Separately

If you need to run only one side at a time:

```bash
npm run dev:api   # ASP.NET backend only  → http://localhost:5194
npm run dev:web   # Vite frontend only    → http://localhost:3000
```

Or directly without the root scripts:

```bash
# Backend
dotnet run --project backend/src/Personal.FinanceTracker.Api

# Frontend
cd frontend && npm run dev
```

---

## Default Ports

| Service | URL |
|---------|-----|
| Frontend (Vite) | http://localhost:3000 |
| Backend (HTTP) | http://localhost:5194 |
| Backend (HTTPS) | https://localhost:7199 |
| Swagger UI | http://localhost:5194/swagger |
| Health check | http://localhost:5194/health/live |

The Vite dev server proxies all `/api/*` requests to `http://localhost:5194` automatically — no CORS configuration needed during local development.

---

## First-Time Setup

```bash
# 1. Install root dev tooling (concurrently)
npm install

# 2. Install frontend packages
cd frontend && npm install && cd ..

# 3. Restore backend packages
dotnet restore backend/

# 4. Apply database migrations (once the first migration exists — Sprint 1)
dotnet ef database update --project backend/src/Personal.FinanceTracker.Api

# 5. Start everything
npm run dev
```

---

## How It Works

`npm run dev` at the root calls `concurrently`, which spawns `dev:api` and `dev:web` as two independent child processes and merges their output into the same terminal. The two processes are identical to running them manually in separate terminals — `concurrently` only handles the output multiplexing and joint `Ctrl+C` shutdown.

```
npm run dev
    │
    └── concurrently
            ├── [api]  dotnet run --project backend/src/Personal.FinanceTracker.Api
            └── [web]  npm run dev --prefix frontend
```
