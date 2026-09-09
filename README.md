# Personal Finance Tracker

> A full-stack personal finance management application built with **ASP.NET 10** and **React 19**
> Track expenses, manage budgets, and gain insights into your financial health.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.9-3178C6?logo=typescript)](https://www.typescriptlang.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Neon-4169E1?logo=postgresql)](https://neon.tech/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## Table of Contents

- [Project Status](#project-status)
- [Features](#features)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Development](#development)
- [Documentation](#documentation)
- [License](#license)

---

## Project Status

The application is built sprint by sprint (plans in [`docs/ai/sprints/`](docs/ai/sprints/)):

| Area | Status |
|------|--------|
| Foundation — shared kernel, middleware pipeline, CI, tooling | Done |
| Users module — JWT auth (register, login, refresh, revoke) | Done |
| Finance module — transactions & categories (CRUD, filters, pagination) | Done |
| Finance module — budgets (period budgets, spending progress) | Implemented |
| Reporting module — dashboard, charts, background jobs (TickerQ) | Planned (Sprint 4) |
| Test suite — integration tests, frontend tests | Planned (Sprint 5) |
| DevOps — CD pipelines, Azure deployment, observability wiring | Planned (Sprint 6) |

---

## Features

### Implemented

- **User Authentication** — JWT-based login and registration with access/refresh token flow
- **Transaction Management** — Track income and expenses with categories, filtering, and pagination
- **Category Management** — Organize transactions with custom categories
- **Budget Planning** — Set per-category budgets with period-based (daily/weekly/monthly/yearly) spending tracking
- **Protected Routes** — Client-side route guarding with automatic token refresh on 401 responses
- **Responsive Design** — Mobile-first UI with Tailwind CSS v4

### Roadmap

- **Dashboard & Reports** — Overview cards, spending charts, monthly summaries (Sprint 4)
- **Background Jobs** — Budget alerts and monthly report generation via TickerQ (Sprint 4)
- **Production Deployment** — Azure hosting, Neon PostgreSQL, OpenTelemetry OTLP export (Sprint 6)

---

## Architecture

This project follows a **Modular Monolith** architecture — module isolation and clean boundaries without the operational overhead of microservices.

```
┌──────────────────────────────────────────────────────────────┐
│                 Personal Finance Tracker API                  │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│   │   Finance    │  │    Users     │  │    Reporting     │   │
│   │   Module ✓   │  │   Module ✓   │  │    (planned)     │   │
│   └──────┬───────┘  └──────┬───────┘  └───────┬──────────┘   │
│          │                 │                   │              │
│   ┌──────▼─────────────────▼───────────────────▼──────────┐   │
│   │              Personal.FinanceTracker.Shared            │   │
│   │   ExceptionMiddleware · ValidationFilter · Entity      │   │
│   └───────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                       PostgreSQL                              │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│   │  finances.*  │  │   users.*    │  │  reporting.*     │   │
│   │      ✓       │  │      ✓       │  │    (planned)     │   │
│   └──────────────┘  └──────────────┘  └──────────────────┘   │
└──────────────────────────────────────────────────────────────┘
```

Each module owns one PostgreSQL schema and one `DbContext` (local development uses Docker; Neon PostgreSQL is the production target).

### Clean Architecture Layers

Each module follows a strict dependency rule — dependencies point inward only:

| Layer | Responsibility | Depends On |
|-------|---------------|------------|
| **Domain** | Entities, value objects, domain rules | Nothing |
| **Application** | Use cases, service interfaces, validators | Domain |
| **Infrastructure** | EF Core repos, external services | Domain, Application |
| **Api** | Minimal API endpoints, DI registration | Application |

Modules register themselves via `AddXxxModule(...)` / `MapXxxEndpoints(...)` in their `DependencyInjection.cs` and are wired in `Program.cs`.

---

## Tech Stack

### Backend

| Package | Version | Purpose |
|---------|---------|---------|
| ASP.NET 10 Minimal APIs | 10.0 | HTTP host, no MVC controllers |
| Entity Framework Core + Npgsql | 10.0 | ORM with module-isolated PostgreSQL schemas |
| FluentValidation | 12.1 | Request validation via `ValidationFilter<T>` |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0 | JWT authentication |
| Microsoft.AspNetCore.OpenApi + Scalar.AspNetCore | 10.0 / 2.14 | OpenAPI document + API reference UI (dev only) |
| AspNetCore.HealthChecks.NpgSql | 9.0 | Referenced; DB readiness check not yet wired |
| xUnit | — | Unit tests (`Finance.UnitTests`, `Users.UnitTests`) |

**Planned:** OpenTelemetry (packages installed, OTLP wiring in Sprint 6), TickerQ background jobs (Sprint 4), TestContainers integration tests (Sprint 5).

### Frontend

| Package | Version | Purpose |
|---------|---------|---------|
| React | 19 | UI framework |
| TypeScript | 5.9 | Type safety (`strict` mode) |
| Vite | 7 | Build tool and dev server |
| Tailwind CSS | 4 (Vite plugin) | Utility-first styling |
| TanStack Query | 5 | Server state management |
| React Hook Form + Zod | 7 / 4 | Form handling and validation |
| React Router DOM | 7 | Client-side routing |
| Native `fetch` API | — | HTTP client (`src/api/client.ts`) with 401/refresh/retry interceptor |
| Recharts | 3 | Data visualization |
| Lucide React | — | Icons |
| date-fns | 4 | Date utilities |
| clsx + tailwind-merge | — | Conditional class composition |

**Installed but not yet wired:** Vitest, Testing Library, MSW (test scripts arrive with Sprint 5).

### Infrastructure

| Component | Technology |
|-----------|------------|
| Database (local) | PostgreSQL 18 via Docker Compose |
| Database (production, planned) | Neon PostgreSQL (serverless) |
| CI | GitHub Actions (build, lint, format check, PR standards) |
| Observability (planned) | OpenTelemetry (OTLP export) |
| Secrets (local) | `appsettings.Local.json` (gitignored, auto-loaded) |
| Secrets (production, planned) | Environment variables |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/) (local PostgreSQL)
- EF Core CLI: `dotnet tool install --global dotnet-ef`
- Optional (Windows): [Task](https://taskfile.dev) + Windows Terminal for the `task` shortcuts below

### 1. Start a local PostgreSQL instance

The database runs via Docker Compose with credentials from an env file:

```bash
cd infrastructure
copy .env.example .env        # macOS/Linux: cp .env.example .env
# Edit .env — set POSTGRES_USER, POSTGRES_PASSWORD, POSTGRES_DB
docker compose up -d          # postgres:18 on localhost:5432 (container: finance-tracker-psql)
```

### 2. Configure backend secrets

Local secrets live in `appsettings.Local.json` (gitignored, loaded explicitly by `Program.cs`). Create it next to `appsettings.json` in `backend/src/Personal.FinanceTracker.Api/`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=<POSTGRES_DB>;Username=<POSTGRES_USER>;Password=<POSTGRES_PASSWORD>"
  },
  "Jwt": {
    "SecretKey": "<your-local-secret-key-minimum-32-characters>",
    "Issuer": "<jwt_issuer>",
    "Audience": "<jwt_audience>",
    "ExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  }
}
```

Match the connection string values to `infrastructure/.env`. Never commit this file.

### 3. Apply database migrations

There are two `DbContext`s (one per module) — both must be updated. Run from `backend/`:

```bash
cd backend

dotnet ef database update --project src/Modules/Users/Personal.FinanceTracker.Users.csproj --startup-project src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj --context UsersDbContext

dotnet ef database update --project src/Modules/Finance/Personal.FinanceTracker.Finance.csproj --startup-project src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj --context FinanceDbContext
```

### 4. Build and run the API

```bash
dotnet run --project backend/src/Personal.FinanceTracker.Api
```

| Endpoint | URL |
|----------|-----|
| API | http://localhost:5194 |
| Scalar API reference (dev only) | http://localhost:5194/scalar |
| Health checks | http://localhost:5194/health/live, http://localhost:5194/health/ready |

### 5. Start the frontend

```bash
cd frontend
npm install
copy .env.example .env        # Required — dev/build scripts load it via dotenv-cli
npm run dev
```

| Endpoint | URL |
|----------|-----|
| Frontend | http://localhost:3000 |

The Vite dev server proxies all `/api/*` requests to `http://localhost:5194` — no CORS setup needed locally.

---

## Project Structure

```
personal-finance-tracker/
│
├── backend/
│   ├── Personal.FinanceTracker.slnx           # Solution (XML format)
│   ├── Directory.Build.props                  # Nullable, ImplicitUsings, Roslyn analyzers
│   │
│   ├── src/
│   │   ├── Personal.FinanceTracker.Api/       # Host — Program.cs, middleware, appsettings.Local.json (gitignored)
│   │   ├── Personal.FinanceTracker.Shared/    # Shared kernel
│   │   │   ├── Abstractions/                  # Entity base class
│   │   │   ├── Exceptions/                    # NotFoundException
│   │   │   ├── Extensions/                    # ClaimsPrincipalExtensions
│   │   │   ├── Filters/                       # ValidationFilter<T>
│   │   │   ├── Middleware/                    # ExceptionHandlingMiddleware (RFC 7807)
│   │   │   └── Models/                        # ApiResponse<T>, ApiError, Result<T>
│   │   └── Modules/
│   │       ├── Users/                         # Auth — register, login, refresh, revoke
│   │       │   ├── Domain/                    # User, RefreshToken entities + interfaces
│   │       │   ├── Application/               # DTOs, service interfaces, validators
│   │       │   ├── Infrastructure/            # EF Core (users.* schema), repos, JWT services
│   │       │   ├── Api/Endpoints/             # AuthEndpoints
│   │       │   └── DependencyInjection.cs     # AddUsersModule / MapUsersEndpoints
│   │       └── Finance/                       # Transactions, Categories, Budgets (finances.* schema)
│   │           └── (same layer layout as Users)
│   │
│   └── tests/
│       ├── Finance.UnitTests/                 # xUnit — domain/application logic
│       └── Users.UnitTests/                   # xUnit — domain/application logic
│
├── frontend/
│   ├── .env.example                           # VITE_API_URL, VITE_ENVIRONMENT
│   └── src/
│       ├── api/                               # Fetch-based client + auth/budgets/categories/transactions modules
│       ├── components/                        # Shared UI (layout/, ui/)
│       ├── features/                          # auth/, transactions/, categories/, budgets/
│       ├── hooks/                             # Custom React hooks
│       ├── pages/                             # NotFoundPage, PlaceholderPage
│       ├── routes/                            # createBrowserRouter route definitions
│       ├── types/                             # TypeScript types mirroring backend DTOs
│       └── utils/                             # clientLogger, documentTitle
│
├── infrastructure/
│   ├── docker-compose.yml                     # Local PostgreSQL 18 (finance-tracker-psql)
│   └── .env.example                           # POSTGRES_USER / PASSWORD / DB
│
├── docs/                                      # Architecture and sprint documentation
├── Taskfile.yml                               # Windows Terminal task shortcuts (see Development)
├── run.py                                     # Opens frontend + backend in terminal tabs
└── README.md
```

---

## Development

### Backend Commands

```bash
# Build the solution
dotnet build backend/Personal.FinanceTracker.slnx

# Run the API
dotnet run --project backend/src/Personal.FinanceTracker.Api

# Run all tests
dotnet test backend/Personal.FinanceTracker.slnx

# Run a single test project
dotnet test backend/tests/Finance.UnitTests

# Run a single test by name
dotnet test backend/Personal.FinanceTracker.slnx --filter "FullyQualifiedName~MyMethodName"

# Run tests with coverage
dotnet test backend/Personal.FinanceTracker.slnx --collect:"XPlat Code Coverage"

# Format all C# code
dotnet format backend/Personal.FinanceTracker.slnx

# Verify formatting without changing files (what CI checks, warn-only)
dotnet format backend/Personal.FinanceTracker.slnx --verify-no-changes --severity warn
```

### EF Core Migrations

Run from `backend/`. Add a migration to a module, then apply it:

```bash
# Add a Finance migration
dotnet ef migrations add <MigrationName> --project src/Modules/Finance/Personal.FinanceTracker.Finance.csproj --startup-project src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj --context FinanceDbContext --output-dir Infrastructure/Data/Migrations

# Apply it
dotnet ef database update --project src/Modules/Finance/Personal.FinanceTracker.Finance.csproj --startup-project src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj --context FinanceDbContext
```

For the Users module, use `src/Modules/Users/Personal.FinanceTracker.Users.csproj` and `--context UsersDbContext`.

### Frontend Commands

```bash
cd frontend

npm run dev           # Vite dev server (http://localhost:3000) — loads .env via dotenv-cli
npm run build         # Type-check (tsc -b) then production build
npm run build-lint    # ESLint + type-check + production build
npm run lint          # ESLint only
npm run preview       # Serve the production build
npm run serve         # Build, then serve the production build
```

Vitest, Testing Library, and MSW are installed, but test scripts are not wired up yet (planned — Testing sprint).

### Task Runner (Windows)

`Taskfile.yml` opens the apps in Windows Terminal tabs (requires [Task](https://taskfile.dev) and `wt`):

```bash
task local          # Frontend (production preview) + Backend, in terminal tabs
task local-debug    # Frontend (Vite dev server) + Backend, in terminal tabs
task backend        # Backend only
task frontend       # Frontend production preview only
task frontend-debug # Vite dev server only
task backend-format # dotnet format the solution
```

Alternative: `python run.py` opens both apps in terminal tabs.

### CI (GitHub Actions)

- **Dev CI** (`dev.yml`, on push/PR to `main`): backend restore + Release build + format check (warn-only); frontend `npm ci` + lint + build. Runs on .NET 10 / Node 20.
- **PR Standard Check** (`pr-standard.yml`, required for merging into `main`):
  - Title: `PR: [Area] Title` — optional area in brackets (e.g. `[Finance]`, `[Users]`, `[CI]`, `[Docs]`), imperative mood, at most 150 characters
  - Description must contain, in order:
    ```markdown
    # Summary:
    - 2–3 sentences: what changed and why it is useful

    # Key Changes:
    - Bullet point 1
    - Bullet point 2
    ```
- **opencode** (`opencode.yml`): comment `/oc` or `/opencode` on an issue or PR to trigger an automated code-reviewer agent run.

---

## Documentation

All documentation lives in `docs/`:

| Document | Description |
|----------|-------------|
| [01-Project-Structure.md](docs/01-Project-Structure.md) | Monorepo layout and module organization |
| [02-Backend-Documentation.md](docs/02-Backend-Documentation.md) | Clean Architecture, EF Core, Minimal APIs, FluentValidation |
| [03-Frontend-Documentation.md](docs/03-Frontend-Documentation.md) | React patterns, TanStack Query, forms, chart components |
| [04-DevOps-Deployment.md](docs/04-DevOps-Deployment.md) | GitHub Actions CI/CD, environment configuration |
| [05-Infrastructure.md](docs/05-Infrastructure.md) | Neon PostgreSQL, environment variables, secrets |
| [06-Local-Development.md](docs/06-Local-Development.md) | Local setup, running the stack, migrations |
| [DEPENDENCIES.md](docs/DEPENDENCIES.md) | NuGet/npm dependency catalogue with purposes |
| [DESIGN_PATTERNS.md](docs/DESIGN_PATTERNS.md) | Backend design patterns catalogue (Result, Repository, Options, etc.) |
| [ai/ui-design-rules.md](docs/ai/ui-design-rules.md) | UI component conventions and Tailwind patterns |
| [ai/sprints/SPRINTS-OVERVIEW.md](docs/ai/sprints/SPRINTS-OVERVIEW.md) | Sprint plans, sequencing, and status |
| [AGENTS.md](AGENTS.md) | Coding standards, naming conventions, architecture rules |

---

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

<div align="center">
  <p>Built with .NET 10 and React 19</p>
</div>
