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

- [Features](#features)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Development](#development)
- [Documentation](#documentation)
- [License](#license)

---

## Features

### Core Functionality
- **Transaction Management** — Track income and expenses with categories
- **Budget Planning** — Set budgets per category and monitor spending
- **Financial Reports** — Visualize spending patterns and trends
- **Category Management** — Organize transactions with custom categories
- **User Authentication** — Secure JWT-based authentication with token refresh
- **Search & Filters** — Filter transactions by date, category, type, and amount

### Technical Highlights
- **Modular Monolith** — Clean Architecture with isolated modules (Finance, Users, Reporting)
- **Minimal APIs** — High-performance ASP.NET 10 endpoints, no MVC controllers
- **Responsive Design** — Mobile-first UI with Tailwind CSS v4
- **Server State** — TanStack Query with query key factory pattern and optimistic updates
- **Observability** — OpenTelemetry with OTLP export (traces, metrics, logs)
- **Strict TypeScript** — `strict`, `verbatimModuleSyntax`, `noUnusedLocals` enforced

---

## Architecture

This project follows a **Modular Monolith** architecture — module isolation and clean boundaries without the operational overhead of microservices.

```
┌──────────────────────────────────────────────────────────────┐
│                 Personal Finance Tracker API                  │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│   │   Finance    │  │    Users     │  │    Reporting     │   │
│   │   Module     │  │   Module     │  │     Module       │   │
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
│                    Neon PostgreSQL                            │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│   │  finances.*  │  │   users.*    │  │   reporting.*    │   │
│   └──────────────┘  └──────────────┘  └──────────────────┘   │
└──────────────────────────────────────────────────────────────┘
```

### Clean Architecture Layers

Each module follows a strict dependency rule — dependencies point inward only:

| Layer | Responsibility | Depends On |
|-------|---------------|------------|
| **Domain** | Entities, value objects, domain rules | Nothing |
| **Application** | Use cases, service interfaces, validators | Domain |
| **Infrastructure** | EF Core repos, external services | Domain, Application |
| **Api** | Minimal API endpoints, DI registration | Application |

---

## Tech Stack

### Backend
| Package | Version | Purpose |
|---------|---------|---------|
| ASP.NET 10 Minimal APIs | 10.0 | HTTP host, no MVC controllers |
| Entity Framework Core | 10.0 | ORM |
| Npgsql EF Core Provider | 10.0 | PostgreSQL driver |
| FluentValidation | 12.1 | Request validation via `ValidationFilter<T>` |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0 | JWT authentication |
| Swashbuckle.AspNetCore | 10.1 | OpenAPI / Swagger UI |
| OpenTelemetry | 1.15 | Traces, metrics, logs (OTLP export) |
| AspNetCore.HealthChecks.NpgSql | 9.0 | PostgreSQL health check endpoint |
| xUnit + TestContainers | — | Unit and integration testing |
| TickerQ | — | Cron-based background jobs |

### Frontend
| Package | Version | Purpose |
|---------|---------|---------|
| React | 19 | UI framework |
| TypeScript | 5.9 | Type safety (`strict` mode) |
| Vite | 7 | Build tool and dev server |
| Tailwind CSS | v4 (Vite plugin) | Utility-first styling |
| TanStack Query | latest | Server state management |
| React Hook Form + Zod | latest | Form handling and validation |
| React Router DOM | v7 | Client-side routing |
| Native `fetch` API | — | HTTP client with 401/refresh interceptor |
| Recharts / Chart.js | latest | Data visualization |
| Lucide React | latest | Icons |
| date-fns | latest | Date utilities |
| clsx + tailwind-merge | latest | Conditional class composition |

### Infrastructure
| Component | Technology |
|-----------|-----------|
| Database | Neon PostgreSQL (serverless) |
| CI/CD | GitHub Actions |
| Observability | OpenTelemetry (OTLP) |
| Secrets (local) | .NET User Secrets |
| Secrets (production) | Environment variables |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker](https://www.docker.com/) (for local PostgreSQL)

### Local Development Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/abrmeval/personal-finance-tracker.git
   cd personal-finance-tracker
   ```

2. **Start a local PostgreSQL instance**
   ```bash
   docker run -d --name finance-db \
     -e POSTGRES_PASSWORD=postgres \
     -p 5432:5432 \
     postgres:16
   ```

3. **Configure backend secrets** (never committed to git)
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
     "Host=localhost;Port=5432;Database=finance_tracker_dev;Username=postgres;Password=postgres" \
     --project backend/src/Personal.FinanceTracker.Api

   dotnet user-secrets set "Jwt:SecretKey" \
     "your-local-secret-key-minimum-32-characters" \
     --project backend/src/Personal.FinanceTracker.Api
   ```

4. **Build and run the API**
   ```bash
   dotnet build backend/
   dotnet run --project backend/src/Personal.FinanceTracker.Api
   # API: http://localhost:5194
   # Swagger UI: http://localhost:5194/swagger
   # Health: http://localhost:5194/health/live
   ```

5. **Install frontend dependencies**
   ```bash
   cd frontend
   npm install
   ```

6. **Start the frontend**
   ```bash
   npm run dev
   # Frontend: http://localhost:3000
   # Proxies /api/* → http://localhost:5194
   ```

---

## Project Structure

```
personal-finance-tracker/
│
├── backend/
│   ├── Personal.FinanceTracker.slnx
│   ├── Directory.Build.props              # net10.0, TreatWarningsAsErrors, analyzers
│   │
│   └── src/
│       ├── Personal.FinanceTracker.Api/   # ASP.NET 10 host — Program.cs, middleware pipeline
│       ├── Personal.FinanceTracker.Shared/ # Shared kernel
│       │   ├── Abstractions/              # Entity base class
│       │   ├── Exceptions/               # NotFoundException
│       │   ├── Extensions/               # ClaimsPrincipalExtensions
│       │   ├── Filters/                  # ValidationFilter<T>
│       │   ├── Middleware/               # ExceptionHandlingMiddleware
│       │   └── Models/                   # ApiResponse<T>, ApiError, Result<T>
│       └── Modules/
│           ├── Users/                    # Auth — implemented (Sprint 1)
│           │   ├── Domain/               # User, RefreshToken entities + interfaces
│           │   ├── Application/          # DTOs, service interfaces, validators
│           │   ├── Infrastructure/       # EF Core, repositories, services, JWT config
│           │   └── Api/Endpoints/        # AuthEndpoints (register, login, refresh, revoke)
│           ├── Finance/                  # Transactions, Categories, Budgets (Sprint 2–3)
│           └── Reporting/               # Dashboard, analytics (Sprint 4)
│
├── frontend/
│   └── src/
│       ├── api/                          # Fetch-based client + per-resource API modules
│       ├── components/                   # Shared UI (auth/, layout/)
│       ├── features/                     # Feature pages (auth/ implemented; others Sprint 2+)
│       ├── hooks/                        # Custom React hooks
│       ├── routes/                       # createBrowserRouter route definitions
│       ├── types/                        # TypeScript types mirroring backend DTOs
│       └── utils/                        # clientLogger, documentTitle
│
├── docs/
│   ├── 01-Project-Structure.md
│   ├── 02-Backend-Documentation.md
│   ├── 03-Frontend-Documentation.md
│   ├── 04-DevOps-Deployment.md
│   ├── 05-Infrastructure.md
│   ├── 06-Local-Development.md
│   ├── DESIGN_PATTERNS.md               # Backend design patterns catalogue
│   └── ai/
│       ├── ui-design-rules.md
│       └── sprints/                      # Sprint plans and execution docs
│
└── README.md
```

---

## Development

### Backend Commands

```bash
# Build solution
dotnet build backend/

# Run API
dotnet run --project backend/src/Personal.FinanceTracker.Api

# Run all tests
dotnet test backend/

# Run a specific test project
dotnet test backend/tests/Finance.UnitTests

# Run tests with coverage
dotnet test backend/ --collect:"XPlat Code Coverage"

# Format code
dotnet format backend/

# Add a migration (once Finance module exists)
dotnet ef migrations add <MigrationName> \
  --context <DbContext> \
  --project backend/src/Modules/<ProjectName> \
  --startup-project backend/src/Personal.FinanceTracker.Api

# Apply migrations
dotnet ef database update \
  --context <DbContext> \
  --startup-project backend/src/Personal.FinanceTracker.Api
```

### Frontend Commands

```bash
cd frontend

# Install dependencies
npm install

# Start dev server (http://localhost:3000)
npm run dev

# Type-check and build for production
npm run build

# Preview production build
npm run preview

# Lint
npm run lint

# Run tests (Vitest)
npm test

# Run tests with coverage
npm run test:coverage
```

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
| [DESIGN_PATTERNS.md](docs/DESIGN_PATTERNS.md) | Backend design patterns catalogue (Result, Repository, Options, etc.) |
| [ai/ui-design-rules.md](docs/ai/ui-design-rules.md) | UI component conventions and Tailwind patterns |
| [AGENTS.md](AGENTS.md) | Coding standards, naming conventions, architecture rules |

---

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

<div align="center">
  <p>Built with .NET 10 and React 19</p>
</div>
