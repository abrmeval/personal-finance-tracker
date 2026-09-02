# Sprint Planning Overview — Personal Finance Tracker

This document is the master sprint plan for building the Personal Finance Tracker from scaffolding to production. It is the single source of truth for sprint scope, sequencing, and goals.

> Sprint detail files live alongside this document at `docs/ai/sprints/sprint-N.md`.

---

## Project State at Planning Date

| Area | Status |
|------|--------|
| Backend | Hello World ASP.NET stub — no modules, no packages, empty `.sln` |
| Frontend | Default Vite counter app — no routing, no packages beyond React |
| Database | Not provisioned |
| CI/CD | No workflows exist |
| Tests | None |
| Docs | Complete (~4,600 lines of architecture docs with code samples) |

Everything must be built. The documentation in `docs/` provides copy-ready code samples for every component.

---

## Sprint Summary

| # | Sprint | Duration | Key Outcome |
|---|--------|----------|-------------|
| [0](#sprint-0--foundation--tooling) | Foundation & Tooling | 1 week | Both stacks runnable; all packages installed; shared kernel built |
| [1](#sprint-1--users-module--authentication) | Users Module / Authentication | 1.5 weeks | Register, login, JWT-protected routes working end-to-end |
| [2](#sprint-2--finance-module-transactions--categories) | Finance Module: Transactions & Categories | 2 weeks | Core CRUD fully connected front-to-back with pagination and filtering |
| [3](#sprint-3--finance-module-budgets) | Finance Module: Budgets | 1 week | Budget tracking with period-based spending calculations |
| [4](#sprint-4--reporting-module--dashboard) | Reporting Module & Dashboard | 1.5 weeks | Dashboard with charts; background jobs running |
| [5](#sprint-5--testing) | Testing | 1 week | Unit + integration coverage on all critical paths |
| [6](#sprint-6--devops--infrastructure) | DevOps & Infrastructure | 1 week | CI/CD pipeline live; production deployed on Azure |
| | **Total** | **~9 weeks** | Full working application |

---

## Sprint 0 — Foundation & Tooling

**Duration:** 1 week
**Doc:** [sprint-0.md](./sprint-0.md)
**Status:** Done

### Goals
Get both stacks from scaffolding to a working, runnable baseline. No feature work begins until this sprint is complete — it is a blocker for all subsequent sprints.

### What's Included
- Fix `Directory.Build.props` `net8.0` / `net10.0` mismatch
- Register both backend projects in the empty `.sln`
- Install all planned NuGet packages (EF Core, Npgsql, FluentValidation, JWT Bearer, OpenTelemetry, TickerQ)
- Build the Shared Kernel: `ExceptionHandlingMiddleware`, `ValidationFilter<T>`, base `Entity`, `NotFoundException`, `ProblemDetails` mapping
- Replace `Program.cs` Hello World with a full middleware pipeline (CORS, auth, health checks, Swagger)
- Install all planned npm packages (React Router, TanStack Query, Axios, React Hook Form, Zod, Tailwind CSS, chart libraries)
- Configure `vite.config.ts` with `@/` path alias and dev API proxy
- Set up Tailwind CSS and main layout skeleton (`Sidebar`, `Header`, `MainLayout`)
- Wire `QueryClientProvider` and `BrowserRouter` into `main.tsx`
- Delete `Class1.cs` stub

### Out of Scope
- Any feature modules (Finance, Users, Reporting)
- Database migrations
- Authentication logic
- Deployment / CI/CD

### Success Criteria
- `dotnet build` passes with zero warnings
- `npm run build` passes with zero TypeScript errors
- API starts and responds at `/health/live`
- Frontend starts with routing and layout visible in browser

---

## Sprint 1 — Users Module / Authentication

**Duration:** 1.5 weeks
**Doc:** [sprint-1.md](./sprint-1.md)
**Status:** Done

### Goals
Users can register and log in. JWT-protected routes work end-to-end. Frontend has a working auth flow with refresh token handling.

### What's Included
- **Backend:** `User` domain entity, `RefreshToken`, `IUserRepository`, `UserRepository`
- **Backend:** `UsersDbContext` with `users.*` schema, EF Core configuration, first migration
- **Backend:** JWT configuration, register/login/refresh-token Minimal API endpoints
- **Backend:** `AddUsersModule` + `MapUsersEndpoints` registration
- **Backend:** FluentValidation validators for register/login requests
- **Frontend:** `authService.ts`, Axios interceptor with 401 → refresh token → retry logic
- **Frontend:** `AuthContext`, `useAuth` hook, `LoginPage`, `RegisterPage`, protected route wrapper
- **Frontend:** Type definitions: `User`, `LoginRequest`, `AuthResponse`

### Out of Scope
- Finance module entities
- Any feature pages beyond auth

### Dependencies
- Sprint 0 complete

---

## Sprint 2 — Finance Module: Transactions & Categories

**Duration:** 2 weeks
**Doc:** [sprint-2.md](./sprint-2.md)
**Status:** In Progress (Tasks 1–28 complete — pending commit, end-to-end runtime verification, and designer-enforcer review)

### Goals
Full CRUD for transactions and categories, connected front-to-back with pagination, filtering, and cache-invalidated queries.

### What's Included
- **Backend:** `Transaction`, `Category` domain entities with factory methods; `TransactionType` enum
- **Backend:** `FinanceDbContext` (`finances.*` schema), EF Core Fluent API configurations, migrations
- **Backend:** `ITransactionRepository`, `ICategoryRepository`, EF Core implementations
- **Backend:** All transaction endpoints (list w/ filter+pagination, get, create, update, delete)
- **Backend:** All category endpoints (list, create, update, delete)
- **Backend:** `TransactionService`, `CategoryService`, FluentValidation validators
- **Frontend:** Type definitions: `Transaction`, `Category`, `PagedResult<T>`, `CreateTransactionRequest`
- **Frontend:** `transactionsApi`, `categoriesApi` service modules
- **Frontend:** `useTransactions`, `useCreateTransaction`, `useUpdateTransaction`, `useDeleteTransaction`, `useCategories` hooks with query key factories
- **Frontend:** `TransactionForm` (Zod + React Hook Form), `TransactionList`, `TransactionsPage`, `CategoriesPage`

### Out of Scope
- Budget tracking
- Dashboard charts

### Dependencies
- Sprint 1 complete (JWT auth needed for protected endpoints)

---

## Sprint 3 — Finance Module: Budgets

**Duration:** 1 week
**Doc:** [sprint-3.md](./sprint-3.md)
**Status:** New

### Goals
Users can create budgets per category with period-based (daily/weekly/monthly/yearly) spending calculations and visual progress tracking.

### What's Included
- **Backend:** `Budget` domain entity, `BudgetPeriod` enum, `IBudgetRepository`, `BudgetService`
- **Backend:** Budget endpoints with spending-vs-budget calculation logic; EF Core configuration + migration
- **Backend:** FluentValidation validators for budget requests
- **Frontend:** `budgetsApi` service, `useBudgets`, `useCreateBudget`, `useUpdateBudget`, `useDeleteBudget` hooks
- **Frontend:** `BudgetForm`, `BudgetList`, `BudgetProgressChart`, `BudgetsPage`
- **Frontend:** Type definitions: `Budget`, `BudgetWithSpending`, `CreateBudgetRequest`

### Out of Scope
- Budget alert background jobs (Sprint 4)

### Dependencies
- Sprint 2 complete (categories needed for budget-category association)

---

## Sprint 4 — Reporting Module & Dashboard

**Duration:** 1.5 weeks
**Doc:** [sprint-4.md](./sprint-4.md)
**Status:** New

### Goals
A complete dashboard with financial overview cards and charts. Monthly summaries and category breakdowns available. Background jobs running on schedule.

### What's Included
- **Backend:** Reporting module — `ReportingDbContext` (read-only cross-schema views), `IDashboardRepository`, summary query logic
- **Backend:** Dashboard summary, income-vs-expenses by month, and category breakdown endpoints
- **Backend:** `AddReportingModule` + `MapReportingEndpoints` registration
- **Backend:** TickerQ background jobs — `MonthlyReportJob` (1st of month), `BudgetAlertJob` (every 6 hours)
- **Frontend:** `reportsApi` service, `useDashboardSummary`, `useIncomeVsExpenses`, `useCategoryBreakdown` hooks
- **Frontend:** `OverviewCards`, `SpendingPieChart`, `IncomeExpenseChart`, `Dashboard` page fully assembled
- **Frontend:** `formatCurrency`, `formatDate`, `getCurrentMonthRange` utility functions

### Out of Scope
- Email/push notifications for budget alerts

### Dependencies
- Sprints 2 and 3 complete (needs transaction and budget data)

---

## Sprint 5 — Testing

**Duration:** 1 week
**Doc:** [sprint-5.md](./sprint-5.md)
**Status:** New

### Goals
Core domain and application logic covered by unit tests. Integration tests validating key API endpoints against a real database.

### What's Included
- **Backend:** `Finance.UnitTests` and `Users.UnitTests` xUnit projects; register in `.sln`
- **Backend:** Unit tests for all domain entities and services with mocked repositories
- **Backend:** `Finance.IntegrationTests` with TestContainers — transaction and auth endpoints
- **Frontend:** Install Vitest, `@testing-library/react`, MSW; add `npm test` script
- **Frontend:** Unit tests for utility functions and key components (`TransactionForm`, `LoginPage`)

### Out of Scope
- E2E tests (Playwright/Cypress)
- Performance tests

### Dependencies
- Sprints 0–4 complete

---

## Sprint 6 — DevOps & Infrastructure

**Duration:** 1 week
**Doc:** [sprint-6.md](./sprint-6.md)
**Status:** New

### Goals
Fully automated CI/CD pipeline. Production environment live on Azure with observability configured.

### What's Included
- `.github/workflows/` — `ci-checks.yml`, `api-deploy.yml`, `frontend-deploy.yml`, `db-migrate.yml`
- Terraform `infrastructure/` folder — Neon PostgreSQL, Azure App Service, Static Web Apps, Key Vault, App Insights
- Production `appsettings.Production.json`, Azure Key Vault secrets, `staticwebapp.config.json`
- OpenTelemetry OTLP export wired to Application Insights
- GitHub Secrets configured for all workflows
- Health check endpoints validated (`/health/live`, `/health/ready`)
- Fix `.github/skills/finance-tracker-expert/SKILL.md` placeholder

### Out of Scope
- Custom domain / SSL (post-launch)
- Staging deployment slot

### Dependencies
- All previous sprints complete

---

## Known Gaps & Risks

| Risk | Mitigation |
|------|-----------|
| `net8.0` vs `net10.0` mismatch in `Directory.Build.props` | Resolved in Sprint 0 Task 1 |
| Empty `.sln` — projects not registered | Resolved in Sprint 0 Task 2 |
| No `@/` path alias in `vite.config.ts` | Resolved in Sprint 0 Task 8 |
| TickerQ requires PostgreSQL backing store — needs migration | Handled in Sprint 4 |
| Neon PostgreSQL provisioning depends on Sprint 6 | Sprints 1–5 use local PostgreSQL via Docker/TestContainers |

---

*Last updated: 01/09/2026*
