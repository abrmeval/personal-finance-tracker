# Sprint 4 — Reporting Module & Dashboard

**Duration:** 1.5 weeks
**Status:** New
**Overview:** [SPRINTS-OVERVIEW.md](./SPRINTS-OVERVIEW.md)

---

## Overview

Sprint 4 delivers the Reporting module and the Dashboard — the first read-centric slice of the application. Users land on a dashboard with overview cards (total balance, monthly income, monthly expenses), a spending-by-category pie chart for the current month, and an income-vs-expenses line chart over the last six months. A Reports page offers the same breakdowns over a selectable month and a 12-month trend. Behind the scenes, TickerQ background jobs run on schedule: `MonthlyReportJob` (1st of the month) persists per-user monthly summaries into the `reports` schema, and `BudgetAlertJob` (every 6 hours) scans for budgets at or above 80% usage.

**This sprint depends on Sprints 2 and 3 being complete.** The Finance module's transactions, categories, and budgets — plus their repositories — are the data source for every dashboard aggregate. No new finance write paths are added; Finance only gains read-only aggregate contracts.

> **Convention alignment (11/09/2026):** This plan was verified against the **as-built** code and the **current** external library APIs before a single task was written. Three planned approaches from the architecture docs do not match reality and have been corrected here — see the next section. Every code sample below mirrors an existing file or an officially documented API; follow them exactly.

---

## Convention Alignment — Plan vs Reality (11/09/2026)

Earlier revisions of the architecture docs describe Sprint 4 features with APIs that do not match what is installed or available today. Where they disagree, **the code and the official library docs win**:

1. **TickerQ samples in `docs/02-Backend-Documentation.md` §10 are outdated.** They show `ITickerJob`, `options.UsePostgreSql(...)`, and `.AddJob<T>()` — none of which exist in TickerQ 10.4.0 (the current release, targeting .NET 10). The real API, verified against tickerq.net docs: jobs are plain classes with `[TickerFunction("function-name", cronExpression: "0 0 1 * *")]` methods, discovered by a source generator at compile time and **auto-seeded on startup** when a cron expression is present. Registration is `builder.Services.AddTickerQ(...)` plus `app.UseTickerQ()`. Persistence uses an EF Core "operational store" (`TickerQ.EntityFrameworkCore`) that creates `TimeTickers`, `CronTickers`, and `CronTickerOccurrences` tables in a configurable schema (default `ticker`). Official docs: https://tickerq.net/docs and https://tickerq.net/docs/entity-framework.
2. **Chart samples in `docs/03-Frontend-Documentation.md` §7–8 use `react-chartjs-2`, MUI `Card`, and MUI `Loading`** — none of which are installed. The installed charting library is **Recharts 3.8.1** (unused until this sprint), and the as-built styling convention is Tailwind-only with plain `div` containers. All chart and card samples in this plan are rewritten for Recharts + Tailwind. The design intent (colors, layout, loading/error/empty states) from the original samples is preserved.
3. **"Read-only cross-schema views" refined to repository read contracts.** The overview sketch had `ReportingDbContext` mapping the `finances.*` tables a second time for cross-schema reads. Mapping the same tables into two DbContexts creates migration-drift risk and duplicates entity configuration. Instead, Reporting consumes **Finance's repository aggregate contracts** — `docs/01-Project-Structure.md` §Allowed References explicitly permits `Reporting → Shared, Finance (read-only contracts)`, and Sprint 2 already set the precedent with `ITransactionRepository.GetTotalExpensesByCategoryAsync` (added for Sprint 3's budget spending). `ReportingDbContext` owns only the `reports` schema (monthly summaries) and hosts the TickerQ operational store tables (Task 12).
4. **The dashboard is read-only — no mutation error plumbing needed.** The `ApiError` / `modelErrors` / `ClientLogger` pattern from the finance pages applies to mutations. The Dashboard and Reports pages only render query results, so they follow the simpler `BudgetList` pattern: inline error banner, skeleton loading, empty state.
5. **TickerQ operational store attaches to `ReportingDbContext`** (`UseApplicationDbContext<ReportingDbContext>(ConfigurationType.UseModelCustomizer)`) so TickerQ's tables arrive through the same migration set the team already manages. The dedicated `TickerQDbContext` fallback is documented in Task 12.

---

## Scope

### What's Included

**Backend — Finance module (read contracts only)**
- Aggregation result records in `Domain/Models`: `TransactionTypeTotals`, `MonthlyTotals`, `CategoryExpenseTotal`, `UserMonthlyTotals`
- `ITransactionRepository` additions: `GetTotalsByTypeAsync`, `GetMonthlyTotalsAsync`, `GetExpenseTotalsByCategoryAsync`, `GetUserMonthlyTotalsAsync` (cross-user, job-only) with EF Core `GroupBy` implementations
- `IBudgetRepository.GetAllActiveAsync` (cross-user, job-only) and `IBudgetService.GetBudgetsNearLimitAsync(threshold, ct)` with `BudgetService` implementation
- Latent bug fix (verify first): UTC-kind normalization of `startDate`/`endDate` in `TransactionRepository.BuildFilteredQuery` — Npgsql `timestamptz` rejects `DateTimeKind.Unspecified` parameters
- `BudgetAlertJob` in `Modules/Finance/Jobs` (logs alerts; notifications out of scope)

**Backend — Reporting module (new)**
- New project `Personal.FinanceTracker.Reporting` (Clean Architecture folders, references Shared + Finance), registered in the solution and wired into `Program.cs` via the existing `TODO Sprint 4` hooks
- `MonthlySummary` domain entity (private ctor + `Create`/`Update` factories) and `IMonthlySummaryRepository` in `Domain/Interfaces`
- `ReportingDbContext` (`reports` schema), `MonthlySummaryConfiguration` (snake_case, `HasPrecision(18, 2)`, unique `(user_id, year, month)` index), migration `AddMonthlySummariesTable`
- DTOs: `DashboardSummaryResponse`, `MonthlyTotalsResponse`, `CategoryBreakdownResponse`, query param records `IncomeVsExpensesQueryParams`, `CategoryBreakdownQueryParams`
- `IDashboardService` / `IReportingService` in `Application/Interfaces`; `DashboardService` / `ReportingService` in `Infrastructure/Services` — `Result<T>` pattern, month-gap filling, end-of-day date normalization
- `ReportingEndpoints`: `GET /api/reports/dashboard/summary`, `GET /api/reports/dashboard/income-vs-expenses`, `GET /api/reports/dashboard/category-breakdown` — every response wrapped in `ApiResponse<T>`
- New error code `ApiErrorCode.InvalidReportParameters`
- TickerQ 10.4.0 packages installed; `AddTickerQ` + operational store (schema `ticker`) + `app.UseTickerQ()` in `Program.cs`; migration `AddTickerQTables`
- `MonthlyReportJob` in `Modules/Reporting/Jobs` (cron `0 0 1 * *`) — persists previous-month summaries idempotently (upsert)
- New `Reporting.UnitTests` project + `BudgetServiceTests` additions in `Finance.UnitTests`

**Frontend**
- Type definitions in `src/types/reporting.ts`: `DashboardSummary`, `MonthlyTotals`, `CategoryBreakdown`
- `getCurrentMonthRange()` utility in `src/utils/dates.ts` (date-fns, `yyyy-MM-dd` range)
- `reportsApi` service module (`src/api/reports.ts`) using the fetch-based `apiClient`, returning `ApiResponse<T>` envelopes
- `src/features/dashboard/hooks/useDashboard.ts`: `dashboardKeys` factory, `useDashboardSummary` (2-min `staleTime`), `useIncomeVsExpenses`, `useCategoryBreakdown` (`enabled` guard)
- Cross-feature cache invalidation: transaction **and** category mutations invalidate `dashboardKeys.all`
- Components in `src/features/dashboard/components/`: `OverviewCards`, `IncomeExpenseChart`, `SpendingPieChart` — Recharts, Tailwind, loading/error/empty states
- `DashboardPage` wired to the index route `/` (replaces the placeholder); `ReportsPage` wired to `/reports` (month picker + 12-month trend); `PlaceholderPage` deleted

### Out of Scope
- Email/push notifications for budget alerts (job logs only)
- Budget progress chart and recent-transactions widget on the dashboard (docs/03 sketches exist; deferred as polish — see Side Notes)
- Persisted-summary read endpoints (`reports.monthly_summaries` is job output + Sprint 5 test target; dashboard reads are live)
- TickerQ Dashboard UI package (`TickerQ.Dashboard`)
- Frontend tests (Sprint 5 — Vitest is installed but has no scripts/config yet)

---

## Pre-Sprint State (Verified 11/09/2026)

1. **Sprints 0–3 are Done.** Auth, transactions, categories, and budgets work end-to-end. `dotnet test` = 189/189 passing (130 Finance + 59 Users). `npm run build` green.
2. **The hooks are already in place.** `Program.cs` lines 54 and 87 contain the exact `// TODO Sprint 4` placeholders for `AddReportingModule` / `MapReportingEndpoints`. `frontend/src/routes/index.tsx` has placeholder routes at `/` ("Dashboard — coming in Sprint 4") and `/reports`. The Sidebar already lists both destinations.
3. **Frontend chart dependencies are installed but unused:** `recharts` 3.8.1 and `date-fns` 4.1.0 (see `docs/DEPENDENCIES.md` — both listed as "Referenced — planned"). No chart code exists yet.
4. **TickerQ is NOT installed.** No NuGet package exists in any `.csproj`. The version to install is **10.4.0** (`TickerQ` + `TickerQ.EntityFrameworkCore` — all TickerQ packages are versioned together and target .NET 10, matching this solution).
5. **Reporting module does not exist.** `backend/src/Modules/` contains only `Finance` and `Users`.
6. **Baseline health:** `dotnet build` has 0 errors and 6 pre-existing test-project warnings (`TreatWarningsAsErrors` is commented out — tracked in `SPRINTS-OVERVIEW.md` Known Gaps). Do not add new warnings; fix pre-existing ones when touched.
7. **Known latent bug (fix in Task 4):** `TransactionQueryParams.StartDate`/`EndDate` bind from query strings as `DateTimeKind.Unspecified`, which Npgsql's `timestamptz` mapping rejects when EF Core sends them as query parameters. The transactions list date filter may fail at runtime when dates are supplied. Reproduce end-to-end first (Task 4, step 0), then fix.

---

## As-Built Conventions — MUST Follow

| Convention | Evidence (reference file) |
|------------|---------------------------|
| Repository interfaces → `Domain/Interfaces` | `ITransactionRepository.cs`, `IBudgetRepository.cs` |
| Service interfaces → `Application/Interfaces` | `ITransactionService.cs`, `IBudgetService.cs` |
| Service implementations → `Infrastructure/Services` | `BudgetService.cs`, `CategoryService.cs` |
| Services return `Result<T>` with `ErrorResult(Code, Description)` | `Shared/Models/Result.cs`, `BudgetService.cs` |
| Error codes are SCREAMING_SNAKE constants in `ApiErrorCode` | `Shared/Constants/ApiErrorCode.cs` |
| Endpoints wrap every response in `ApiResponse<T>`; `TypedResults` only | `BudgetEndpoints.cs`, `TransactionEndpoints.cs` |
| Query params are sealed records bound with `[AsParameters]` | `Application/DTOs/Requests/TransactionQueryParams.cs` |
| One `DbContext` per module, `HasDefaultSchema`, snake_case columns, `idx_` indexes, `HasPrecision(18, 2)`, `timestamptz`, `EnableRetryOnFailure` | `FinanceDbContext.cs`, `BudgetConfiguration.cs`, `DependencyInjection.cs` (Finance) |
| Migrations in `Infrastructure/Data/Migrations` with explicit `--context/--project/--startup-project/--output-dir` | Sprint 3 Task 4 commands |
| Frontend HTTP: fetch-based `apiClient` (`BASE_URL` includes `/api`), functions return the full envelope | `src/api/transactions.ts`, `src/api/budgets.ts` |
| Query key factories; mutations invalidate via the factory, never hardcoded strings | `useTransactions.ts`, `useBudgets.ts` |
| Feature-based folders co-locate pages/hooks/components | `src/features/budgets/`, `src/features/transactions/` |
| Zod schemas co-located when forms exist (no forms this sprint) | `src/features/budgets/schemas.ts` |
| Currency/date formatting: `Intl` with `es-MX` / MXN from `src/utils/formatters.ts` | `formatCurrency`, `formatDate` |
| Mobile-first, 44px tap targets, `aria-label` on icon-only controls, `role`/`aria` on charts | `docs/ai/ui-design-rules.md`, `BudgetCard.tsx` progressbar |
| Test class mirrors class under test; `MethodName_Scenario_ExpectedResult` | `Finance.UnitTests/Domain/Entities/BudgetTests.cs` |

---

## Tasks

---

### Task 1 — Reporting Module Project Scaffold

**Status:** New

**Description:**
Create the Reporting module project with the same Clean Architecture folder layout as Finance, register it in the solution, and wire the project references. Reporting references Shared and **Finance** (the only module-to-module reference, explicitly allowed by `docs/01-Project-Structure.md` §Allowed References: "Reporting | Shared, Finance (read-only contracts)"). The Api project gains a reference to Reporting. No TickerQ packages yet (Task 2), no code yet — this task only produces an empty, building skeleton.

**Steps:**

1. Create the project and folders (mirror Finance's layout):

```
backend/src/Modules/Reporting/
├── Personal.FinanceTracker.Reporting.csproj
├── DependencyInjection.cs        (added in Task 11 — placeholder now)
├── Api/
│   └── Endpoints/                (Task 10)
├── Application/
│   ├── DTOs/
│   │   ├── Requests/             (Task 8)
│   │   ├── Responses/            (Task 8)
│   └── Interfaces/               (Task 8)
├── Domain/
│   ├── Entities/                 (Task 6)
│   ├── Interfaces/               (Task 6)
│   └── Models/                   (only if needed — Finance keeps result records there)
└── Infrastructure/
    ├── Data/
    │   ├── Configurations/       (Task 7)
    │   ├── Migrations/           (generated by Task 7 / Task 12)
    │   └── ReportingDbContext.cs (Task 7)
    ├── Repositories/             (Task 9)
    ├── Services/                 (Task 9)
    └── Jobs/                     (Task 13 — note: Jobs sit under module root, see Task 13)
```

2. Create `backend/src/Modules/Reporting/Personal.FinanceTracker.Reporting.csproj` — mirror the Finance csproj exactly, minus the FluentValidation packages (this sprint's reporting endpoints validate in the service layer via `Result<T>`, no request-body validators):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
 <ItemGroup>
   <FrameworkReference Include="Microsoft.AspNetCore.App" />
 </ItemGroup>
     <ItemGroup>
    <ProjectReference Include="..\..\..\Personal.FinanceTracker.Shared\Personal.FinanceTracker.Shared.csproj" />
    <ProjectReference Include="..\Finance\Personal.FinanceTracker.Finance.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.8" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.8">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.1" />
  </ItemGroup>
</Project>
```

3. Register the project in the solution (from `backend/`):

```bash
dotnet sln Personal.FinanceTracker.slnx add src/Modules/Reporting/Personal.FinanceTracker.Reporting.csproj
```

4. Add the project reference to the Api — in `backend/src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj`, alongside the existing module references:

```xml
<ProjectReference Include="..\Modules\Reporting\Personal.FinanceTracker.Reporting.csproj" />
```

5. Add a minimal placeholder `DependencyInjection.cs` so the module compiles (replaced fully in Task 11):

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Personal.FinanceTracker.Reporting;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services;
    }
}
```

6. Run `dotnet build backend/Personal.FinanceTracker.slnx` — confirm 0 errors, 0 new warnings.

**Success Criteria:**
- Project compiles and appears in `Personal.FinanceTracker.slnx`
- Folder layout mirrors Finance exactly
- Only references are Shared + Finance (+ framework/EF packages matching Finance)
- Api builds with the new project reference

---

### Task 2 — Install TickerQ Packages

**Status:** New

**Description:**
Install TickerQ 10.4.0. All TickerQ packages are versioned together; both target .NET 10. The **core** package (`TickerQ`) provides the `[TickerFunction]` attribute and source generator — it goes in every project that defines jobs (Finance, Reporting). The **EF Core persistence** package (`TickerQ.EntityFrameworkCore`) provides `AddOperationalStore` — it goes in the Api host only, next to the `AddTickerQ` registration (Task 12).

**Steps:**

1. From `backend/`, add the core package to Finance and Reporting:

```bash
dotnet add src/Modules/Finance/Personal.FinanceTracker.Finance.csproj package TickerQ --version 10.4.0
dotnet add src/Modules/Reporting/Personal.FinanceTracker.Reporting.csproj package TickerQ --version 10.4.0
```

2. Add both packages to the Api host:

```bash
dotnet add src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj package TickerQ --version 10.4.0
dotnet add src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj package TickerQ.EntityFrameworkCore --version 10.4.0
```

3. Run `dotnet build backend/Personal.FinanceTracker.slnx` — confirm 0 errors, 0 new warnings. (Nothing uses the packages yet.)

**Success Criteria:**
- `TickerQ` 10.4.0 referenced in Finance, Reporting, and Api
- `TickerQ.EntityFrameworkCore` 10.4.0 referenced in Api only
- Build stays green with zero new warnings
- `docs/DEPENDENCIES.md` is updated in Task 24 (not now — keep docs in sync at sprint closure)

---

### Task 3 — Finance: Aggregation Result Records and Repository Contracts

**Status:** New

**Description:**
Add the read-only aggregate contracts that Reporting consumes, following the precedent set by `GetTotalExpensesByCategoryAsync` (Sprint 2). Result records live in `Domain/Models` (pure, no dependencies); methods are added to `ITransactionRepository`. Cross-user methods are annotated as job-only contracts.

**Steps:**

1. Create `backend/src/Modules/Finance/Domain/Models/TransactionTypeTotals.cs`:

```csharp
namespace Personal.FinanceTracker.Finance.Domain.Models;

public sealed record TransactionTypeTotals(decimal TotalIncome, decimal TotalExpenses);
```

2. Create `backend/src/Modules/Finance/Domain/Models/MonthlyTotals.cs`:

```csharp
namespace Personal.FinanceTracker.Finance.Domain.Models;

public sealed record MonthlyTotals(int Year, int Month, decimal TotalIncome, decimal TotalExpenses);
```

3. Create `backend/src/Modules/Finance/Domain/Models/CategoryExpenseTotal.cs`:

```csharp
namespace Personal.FinanceTracker.Finance.Domain.Models;

public sealed record CategoryExpenseTotal(Guid CategoryId, decimal Total);
```

4. Create `backend/src/Modules/Finance/Domain/Models/UserMonthlyTotals.cs`:

```csharp
namespace Personal.FinanceTracker.Finance.Domain.Models;

public sealed record UserMonthlyTotals(Guid UserId, decimal TotalIncome, decimal TotalExpenses);
```

5. Extend `backend/src/Modules/Finance/Domain/Interfaces/ITransactionRepository.cs` with the new methods (keep the existing members untouched):

```csharp
using Personal.FinanceTracker.Finance.Domain.Entities;
using Personal.FinanceTracker.Finance.Domain.Enums;
using Personal.FinanceTracker.Finance.Domain.Models;

namespace Personal.FinanceTracker.Finance.Domain.Interfaces;

public interface ITransactionRepository
{
    // ... existing members unchanged ...

    /// <summary>
    /// Returns income and expense totals for a user within an optional date range.
    /// Read contract consumed by the Reporting module's dashboard (Sprint 4).
    /// </summary>
    Task<TransactionTypeTotals> GetTotalsByTypeAsync(
        Guid userId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default);

    /// <summary>
    /// Returns per-month income and expense totals for a user within a date range.
    /// Read contract consumed by the Reporting module's dashboard (Sprint 4).
    /// </summary>
    Task<IReadOnlyList<MonthlyTotals>> GetMonthlyTotalsAsync(
        Guid userId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default);

    /// <summary>
    /// Returns expense totals grouped by category for a user within a date range.
    /// Read contract consumed by the Reporting module's dashboard (Sprint 4).
    /// </summary>
    Task<IReadOnlyList<CategoryExpenseTotal>> GetExpenseTotalsByCategoryAsync(
        Guid userId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default);

    /// <summary>
    /// Returns income and expense totals grouped by user for a calendar month.
    /// Cross-user read contract consumed exclusively by the Reporting module's
    /// MonthlyReportJob background job (Sprint 4) — never exposed via an endpoint.
    /// </summary>
    Task<IReadOnlyList<UserMonthlyTotals>> GetUserMonthlyTotalsAsync(
        int year,
        int month,
        CancellationToken ct = default);
}
```

6. Run `dotnet build` — it fails here only if signatures are mistyped (the implementation arrives in Task 4, so add the methods and implementation together if you prefer one green build).

**Success Criteria:**
- All four result records are sealed records in `Domain/Models` with no dependencies
- Interface methods all accept `CancellationToken` and return records, not entities — no EF Core types in Domain
- Cross-user method carries the job-only doc comment
- Per-user methods exist alongside — never replacing — the existing Sprint 2/3 members

---

### Task 4 — Finance: TransactionRepository Aggregation Implementations

**Status:** New

**Description:**
Implement the four aggregation methods in `TransactionRepository` with EF Core `GroupBy` queries, and fix the latent `DateTimeKind.Unspecified` bug in the existing date filters (reproduce first — see step 0). All reads filter `IsActive`.

**Steps:**

0. **Reproduce the latent date-filter bug first** (end-to-end, per repo guidelines): start the stack (`task local-debug`), open the Transactions page, and filter by start/end date. If the request 500s with an Npgsql error about `DateTimeKind` (or the filter silently misbehaves), you have reproduced it. Apply step 4's normalization. If it works correctly, document that in the sprint completion record and skip step 4.

1. Add the implementations to `backend/src/Modules/Finance/Infrastructure/Repositories/TransactionRepository.cs`:

```csharp
using Personal.FinanceTracker.Finance.Domain.Models;
// (existing usings unchanged)

public async Task<TransactionTypeTotals> GetTotalsByTypeAsync(
    Guid userId,
    DateTime? from,
    DateTime? to,
    CancellationToken ct = default)
{
    var query = context.Transactions.Where(t => t.UserId == userId && t.IsActive);

    if (from.HasValue)
        query = query.Where(t => t.Date >= from.Value);

    if (to.HasValue)
        query = query.Where(t => t.Date <= to.Value);

    var totals = await query
        .GroupBy(t => 1)
        .Select(g => new TransactionTypeTotals(
            g.Where(t => t.Type == TransactionType.Income).Sum(t => (decimal?)t.Amount) ?? 0m,
            g.Where(t => t.Type == TransactionType.Expense).Sum(t => (decimal?)t.Amount) ?? 0m))
        .FirstOrDefaultAsync(ct);

    return totals ?? new TransactionTypeTotals(0m, 0m);
}

public async Task<IReadOnlyList<MonthlyTotals>> GetMonthlyTotalsAsync(
    Guid userId,
    DateTime from,
    DateTime to,
    CancellationToken ct = default)
{
    return await context.Transactions
        .Where(t => t.UserId == userId && t.IsActive && t.Date >= from && t.Date <= to)
        .GroupBy(t => new { t.Date.Year, t.Date.Month })
        .Select(g => new MonthlyTotals(
            g.Key.Year,
            g.Key.Month,
            g.Where(t => t.Type == TransactionType.Income).Sum(t => (decimal?)t.Amount) ?? 0m,
            g.Where(t => t.Type == TransactionType.Expense).Sum(t => (decimal?)t.Amount) ?? 0m))
        .OrderBy(m => m.Year)
        .ThenBy(m => m.Month)
        .ToListAsync(ct);
}

public async Task<IReadOnlyList<CategoryExpenseTotal>> GetExpenseTotalsByCategoryAsync(
    Guid userId,
    DateTime from,
    DateTime to,
    CancellationToken ct = default)
{
    return await context.Transactions
        .Where(t => t.UserId == userId
            && t.IsActive
            && t.Type == TransactionType.Expense
            && t.CategoryId != null
            && t.Date >= from
            && t.Date <= to)
        .GroupBy(t => t.CategoryId!.Value)
        .Select(g => new CategoryExpenseTotal(g.Key, g.Sum(t => (decimal?)t.Amount) ?? 0m))
        .OrderByDescending(x => x.Total)
        .ToListAsync(ct);
}

public async Task<IReadOnlyList<UserMonthlyTotals>> GetUserMonthlyTotalsAsync(
    int year,
    int month,
    CancellationToken ct = default)
{
    var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
    var to = from.AddMonths(1).AddTicks(-1);

    return await context.Transactions
        .Where(t => t.IsActive && t.Date >= from && t.Date <= to)
        .GroupBy(t => t.UserId)
        .Select(g => new UserMonthlyTotals(
            g.Key,
            g.Where(t => t.Type == TransactionType.Income).Sum(t => (decimal?)t.Amount) ?? 0m,
            g.Where(t => t.Type == TransactionType.Expense).Sum(t => (decimal?)t.Amount) ?? 0m))
        .ToListAsync(ct);
}
```

2. Nullable sums (`(decimal?)t.Amount ?? 0m`) translate to SQL `COALESCE(SUM(...), 0)` — groups with no rows of one type yield `0`, never `null`.

3. `GroupBy(t => 1) + FirstOrDefaultAsync` folds both totals into a single SQL round trip; the `?? new TransactionTypeTotals(0m, 0m)` fallback covers a user with zero transactions.

4. **Latent bug fix (only if reproduced in step 0):** in `BuildFilteredQuery`, normalize the existing date filters to UTC — Npgsql `timestamptz` rejects `DateTimeKind.Unspecified` parameters:

```csharp
if (startDate.HasValue)
    query = query.Where(t => t.Date >= DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc));

if (endDate.HasValue)
    query = query.Where(t => t.Date <= DateTime.SpecifyKind(endDate.Value.Date, DateTimeKind.Utc).AddDays(1).AddTicks(-1));
```

   Note this also gives the existing transactions filter inclusive end-of-day semantics (a date-only `endDate` previously excluded same-day transactions after midnight). The Dashboard service performs the same normalization (Task 9), so reporting does not depend on this fix.

5. Run `dotnet build` — confirm 0 errors, 0 new warnings.

**Success Criteria:**
- All four methods pass `ct` through to every EF Core async call and filter `IsActive`
- Aggregates are computed in SQL (`GroupBy`/`Sum` translated) — no in-memory materialization of transactions
- Category breakdown excludes income, uncategorized expenses, and soft-deleted rows; ordered by total descending
- Step 0 outcome (reproduced or not) is recorded — if reproduced, the fix is applied and the transactions date filter verified end-to-end

---

### Task 5 — Finance: Budgets-Near-Limit Read Contract

**Status:** New

**Description:**
Add the cross-user budget read used by `BudgetAlertJob`: `IBudgetRepository.GetAllActiveAsync` plus `IBudgetService.GetBudgetsNearLimitAsync(thresholdPercentage, ct)`. The service reuses the existing private `GetSpendingForPeriodAsync` / `MapToWithSpending` helpers. Category names are user-scoped (`ICategoryRepository.GetAllByUserAsync` takes a userId), so they are fetched once per distinct budget owner.

**Steps:**

1. Add to `backend/src/Modules/Finance/Domain/Interfaces/IBudgetRepository.cs`:

```csharp
/// <summary>
/// Returns all active budgets across all users.
/// Cross-user read contract consumed exclusively by the BudgetAlertJob
/// background job (Sprint 4) — never exposed via an endpoint.
/// </summary>
Task<IReadOnlyList<Budget>> GetAllActiveAsync(CancellationToken ct = default);
```

2. Implement it in `backend/src/Modules/Finance/Infrastructure/Repositories/BudgetRepository.cs`:

```csharp
public async Task<IReadOnlyList<Budget>> GetAllActiveAsync(CancellationToken ct = default)
    => await context.Budgets
        .Where(b => b.IsActive)
        .OrderBy(b => b.Name)
        .ToListAsync(ct);
```

3. Add to `backend/src/Modules/Finance/Application/Interfaces/IBudgetService.cs`:

```csharp
Task<Result<IReadOnlyList<BudgetWithSpendingResponse>>> GetBudgetsNearLimitAsync(
    decimal thresholdPercentage,
    CancellationToken ct = default);
```

4. Implement it in `backend/src/Modules/Finance/Infrastructure/Services/BudgetService.cs`:

```csharp
public async Task<Result<IReadOnlyList<BudgetWithSpendingResponse>>> GetBudgetsNearLimitAsync(
    decimal thresholdPercentage,
    CancellationToken ct = default)
{
    if (thresholdPercentage is < 0 or > 100)
    {
        return Result<IReadOnlyList<BudgetWithSpendingResponse>>.Failure(new(
            ApiErrorCode.InvalidReportParameters, "Threshold percentage must be between 0 and 100."));
    }

    var budgets = await budgetRepository.GetAllActiveAsync(ct);
    if (budgets.Count == 0)
        return Result<IReadOnlyList<BudgetWithSpendingResponse>>.Success([]);

    // Category names are user-scoped — one fetch per distinct budget owner
    var categoryNamesByUser = new Dictionary<Guid, Dictionary<Guid, string>>();
    foreach (var ownerUserId in budgets.Select(b => b.UserId).Distinct())
    {
        categoryNamesByUser[ownerUserId] = (await categoryRepository.GetAllByUserAsync(ownerUserId, ct))
            .ToDictionary(c => c.Id, c => c.Name);
    }

    var results = new List<BudgetWithSpendingResponse>();
    foreach (var budget in budgets)
    {
        var spent = await GetSpendingForPeriodAsync(budget.UserId, budget.CategoryId, budget.Period, ct);
        var response = MapToWithSpending(
            budget,
            categoryNamesByUser[budget.UserId].GetValueOrDefault(budget.CategoryId, "Unknown"),
            spent);

        if (response.PercentageUsed >= thresholdPercentage)
            results.Add(response);
    }

    logger.LogInformation(
        "Budget alert scan: {AlertCount}/{BudgetCount} budgets at or above {Threshold}% usage",
        results.Count, budgets.Count, thresholdPercentage);

    return Result<IReadOnlyList<BudgetWithSpendingResponse>>.Success(results);
}
```

   This method uses `ApiErrorCode.InvalidReportParameters`, which is added to `Shared` in Task 8 — do Task 8 step 1 first if you want a green build after every task.

5. Run `dotnet build` — confirm 0 errors, 0 new warnings.

**Success Criteria:**
- Threshold validation returns a `Result` failure (not a throw) for out-of-range values
- Spending calculation reuses the exact period logic from the budget endpoints — no duplicated period math
- The method never appears in an endpoint — endpoint handlers stay user-scoped
- Empty-budget short circuit avoids per-user category queries when there is nothing to scan

---

### Task 6 — Reporting: MonthlySummary Domain Entity and Repository Interface

**Status:** New

**Description:**
Create the Reporting module's only domain entity: `MonthlySummary`, the persisted snapshot a background job writes on the 1st of each month. It follows the entity conventions exactly (private ctor, static `Create`, `private set`, extends `Entity`) but has no soft-delete — rows are regenerated monthly via `Update` (upsert), so `IsActive` would be dead weight. `IMonthlySummaryRepository` lives in `Domain/Interfaces` per convention.

**Steps:**

1. Create `backend/src/Modules/Reporting/Domain/Entities/MonthlySummary.cs`:

```csharp
using Personal.FinanceTracker.Shared.Abstractions;

namespace Personal.FinanceTracker.Reporting.Domain.Entities;

public sealed class MonthlySummary : Entity
{
    public Guid UserId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public decimal TotalIncome { get; private set; }
    public decimal TotalExpenses { get; private set; }
    public decimal NetAmount { get; private set; }

    private MonthlySummary() { }

    public static MonthlySummary Create(
        Guid userId,
        int year,
        int month,
        decimal totalIncome,
        decimal totalExpenses)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        if (year is < 2000 or > 2100)
            throw new ArgumentException("Year is out of supported range.", nameof(year));

        if (month is < 1 or > 12)
            throw new ArgumentException("Month must be between 1 and 12.", nameof(month));

        if (totalIncome < 0)
            throw new ArgumentException("Total income cannot be negative.", nameof(totalIncome));

        if (totalExpenses < 0)
            throw new ArgumentException("Total expenses cannot be negative.", nameof(totalExpenses));

        return new MonthlySummary
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Year = year,
            Month = month,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            NetAmount = totalIncome - totalExpenses,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(decimal totalIncome, decimal totalExpenses)
    {
        if (totalIncome < 0)
            throw new ArgumentException("Total income cannot be negative.", nameof(totalIncome));

        if (totalExpenses < 0)
            throw new ArgumentException("Total expenses cannot be negative.", nameof(totalExpenses));

        TotalIncome = totalIncome;
        TotalExpenses = totalExpenses;
        NetAmount = totalIncome - totalExpenses;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

2. Create `backend/src/Modules/Reporting/Domain/Interfaces/IMonthlySummaryRepository.cs`:

```csharp
using Personal.FinanceTracker.Reporting.Domain.Entities;

namespace Personal.FinanceTracker.Reporting.Domain.Interfaces;

public interface IMonthlySummaryRepository
{
    Task<MonthlySummary?> GetByUserAndPeriodAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken ct = default);

    Task AddAsync(MonthlySummary summary, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
```

3. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- Entity extends `Personal.FinanceTracker.Shared.Abstractions.Entity`; all properties `private set`
- `NetAmount` is computed inside the entity (factory and `Update`) — never passed in
- Repository interface exposes only the upsert surface: find-by-period, add, save — no delete, no list (this sprint)
- Single-entity lookup returns `MonthlySummary?` — never throws for not-found

---

### Task 7 — Reporting: ReportingDbContext, Configuration, and Migration

**Status:** New

**Description:**
Create the Reporting module's `DbContext` isolated to the `reports` schema, its Fluent API configuration for `MonthlySummary`, and the first migration. Mirrors the Finance module's context, configuration style, and DI wiring exactly.

**Steps:**

1. Create `backend/src/Modules/Reporting/Infrastructure/Data/ReportingDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Personal.FinanceTracker.Reporting.Domain.Entities;

namespace Personal.FinanceTracker.Reporting.Infrastructure.Data;

public sealed class ReportingDbContext(DbContextOptions<ReportingDbContext> options) : DbContext(options)
{
    public DbSet<MonthlySummary> MonthlySummaries => Set<MonthlySummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("reports");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReportingDbContext).Assembly);
    }
}
```

2. Create `backend/src/Modules/Reporting/Infrastructure/Data/Configurations/MonthlySummaryConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Personal.FinanceTracker.Reporting.Domain.Entities;

namespace Personal.FinanceTracker.Reporting.Infrastructure.Data.Configurations;

public sealed class MonthlySummaryConfiguration : IEntityTypeConfiguration<MonthlySummary>
{
    public void Configure(EntityTypeBuilder<MonthlySummary> builder)
    {
        builder.ToTable("monthly_summaries");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(s => s.Year)
            .HasColumnName("year")
            .IsRequired();

        builder.Property(s => s.Month)
            .HasColumnName("month")
            .IsRequired();

        builder.Property(s => s.TotalIncome)
            .HasColumnName("total_income")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(s => s.TotalExpenses)
            .HasColumnName("total_expenses")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(s => s.NetAmount)
            .HasColumnName("net_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("idx_monthly_summaries_user_id");

        builder.HasIndex(s => new { s.UserId, s.Year, s.Month })
            .IsUnique()
            .HasDatabaseName("idx_monthly_summaries_user_period");
    }
}
```

3. Register the context in the module DI — replace the placeholder `AddReportingModule` body in `backend/src/Modules/Reporting/DependencyInjection.cs` (services/endpoints registration grows in later tasks):

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Personal.FinanceTracker.Reporting.Infrastructure.Data;

namespace Personal.FinanceTracker.Reporting;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ReportingDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "reports");
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                    npgsqlOptions.CommandTimeout(30);
                }));

        return services;
    }
}
```

4. Generate the migration (from `backend/`):

```bash
dotnet ef migrations add AddMonthlySummariesTable \
  --project src/Modules/Reporting/Personal.FinanceTracker.Reporting.csproj \
  --startup-project src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj \
  --context ReportingDbContext \
  --output-dir Infrastructure/Data/Migrations
```

5. Review the generated migration: `reports.monthly_summaries` with `numeric(18,2)` amount columns, `timestamptz` dates, both indexes, and the unique `idx_monthly_summaries_user_period`.

6. Apply it:

```bash
dotnet ef database update \
  --project src/Modules/Reporting/Personal.FinanceTracker.Reporting.csproj \
  --startup-project src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj \
  --context ReportingDbContext
```

**Success Criteria:**
- `reports.monthly_summaries` exists in PostgreSQL with all expected columns and both indexes
- Migration lives in `Infrastructure/Data/Migrations` alongside Finance's migrations
- `EnableRetryOnFailure` + `MigrationsHistoryTable("__EFMigrationsHistory", "reports")` match the Finance module's wiring
- No TickerQ tables yet — they arrive in Task 12 as a separate migration

---

### Task 8 — Reporting: Error Code, DTOs, and Service Interfaces

**Status:** New

**Description:**
Add the shared error code, the reporting DTOs (responses + `[AsParameters]` query records — nullable so missing/invalid query values produce an enveloped `Result` failure instead of a bare binding 400), and the two service interfaces.

**Steps:**

1. In `backend/src/Personal.FinanceTracker.Shared/Constants/ApiErrorCode.cs`, add alongside the existing constants:

```csharp
public const string InvalidReportParameters = "INVALID_REPORT_PARAMETERS";
```

2. Create `backend/src/Modules/Reporting/Application/DTOs/Responses/DashboardSummaryResponse.cs`:

```csharp
namespace Personal.FinanceTracker.Reporting.Application.DTOs.Responses;

public sealed record DashboardSummaryResponse(
    decimal TotalBalance,
    decimal MonthlyIncome,
    decimal MonthlyExpenses);
```

3. Create `backend/src/Modules/Reporting/Application/DTOs/Responses/MonthlyTotalsResponse.cs`:

```csharp
namespace Personal.FinanceTracker.Reporting.Application.DTOs.Responses;

public sealed record MonthlyTotalsResponse(
    int Year,
    int Month,
    decimal Income,
    decimal Expenses);
```

4. Create `backend/src/Modules/Reporting/Application/DTOs/Responses/CategoryBreakdownResponse.cs`:

```csharp
namespace Personal.FinanceTracker.Reporting.Application.DTOs.Responses;

public sealed record CategoryBreakdownResponse(
    Guid CategoryId,
    string CategoryName,
    decimal Total);
```

5. Create the query param records — `backend/src/Modules/Reporting/Application/DTOs/Requests/IncomeVsExpensesQueryParams.cs`:

```csharp
namespace Personal.FinanceTracker.Reporting.Application.DTOs.Requests;

public sealed record IncomeVsExpensesQueryParams(int? Months);
```

   and `backend/src/Modules/Reporting/Application/DTOs/Requests/CategoryBreakdownQueryParams.cs`:

```csharp
namespace Personal.FinanceTracker.Reporting.Application.DTOs.Requests;

public sealed record CategoryBreakdownQueryParams(
    DateTime? StartDate,
    DateTime? EndDate);
```

6. Create `backend/src/Modules/Reporting/Application/Interfaces/IDashboardService.cs`:

```csharp
using Personal.FinanceTracker.Reporting.Application.DTOs.Responses;
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Reporting.Application.Interfaces;

public interface IDashboardService
{
    Task<Result<DashboardSummaryResponse>> GetSummaryAsync(
        Guid userId,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<MonthlyTotalsResponse>>> GetIncomeVsExpensesAsync(
        Guid userId,
        int? months,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<CategoryBreakdownResponse>>> GetCategoryBreakdownAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct = default);
}
```

7. Create `backend/src/Modules/Reporting/Application/Interfaces/IReportingService.cs`:

```csharp
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Reporting.Application.Interfaces;

public interface IReportingService
{
    Task<Result<int>> GenerateMonthlyReportsAsync(
        int year,
        int month,
        CancellationToken ct = default);
}
```

8. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- All DTOs are sealed records; no mutable setters
- Query params are nullable so validation failures flow through `Result<T>` → enveloped 400 (the `ApiResponse<T>` contract the frontend parses), not a bare ProblemDetails binding error
- Interfaces live in `Application/Interfaces` with no Infrastructure references

---

### Task 9 — Reporting: DashboardService, ReportingService, and MonthlySummaryRepository

**Status:** New

**Description:**
Implement the reporting data layer and services. `DashboardService` computes live aggregates via Finance's repository contracts (normalizing date kinds and filling month gaps). `ReportingService` performs the idempotent monthly upsert the job triggers.

**Steps:**

1. Create `backend/src/Modules/Reporting/Infrastructure/Repositories/MonthlySummaryRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Personal.FinanceTracker.Reporting.Domain.Entities;
using Personal.FinanceTracker.Reporting.Domain.Interfaces;
using Personal.FinanceTracker.Reporting.Infrastructure.Data;

namespace Personal.FinanceTracker.Reporting.Infrastructure.Repositories;

public sealed class MonthlySummaryRepository(ReportingDbContext context) : IMonthlySummaryRepository
{
    public async Task<MonthlySummary?> GetByUserAndPeriodAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken ct = default)
        => await context.MonthlySummaries
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Year == year && s.Month == month, ct);

    public async Task AddAsync(MonthlySummary summary, CancellationToken ct = default)
        => await context.MonthlySummaries.AddAsync(summary, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}
```

2. Create `backend/src/Modules/Reporting/Infrastructure/Services/DashboardService.cs`:

```csharp
using Microsoft.Extensions.Logging;
using Personal.FinanceTracker.Finance.Domain.Interfaces;
using Personal.FinanceTracker.Finance.Domain.Models;
using Personal.FinanceTracker.Reporting.Application.DTOs.Responses;
using Personal.FinanceTracker.Reporting.Application.Interfaces;
using Personal.FinanceTracker.Shared.Constants;
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Reporting.Infrastructure.Services;

public sealed class DashboardService(
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository,
    ILogger<DashboardService> logger) : IDashboardService
{
    private const int DefaultMonths = 6;
    private const int MaxMonths = 24;

    public async Task<Result<DashboardSummaryResponse>> GetSummaryAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

        var monthTotals = await transactionRepository.GetTotalsByTypeAsync(userId, monthStart, monthEnd, ct);
        var allTimeTotals = await transactionRepository.GetTotalsByTypeAsync(userId, null, null, ct);

        logger.LogDebug("Dashboard summary computed for user {UserId}", userId);

        return Result<DashboardSummaryResponse>.Success(new DashboardSummaryResponse(
            TotalBalance: allTimeTotals.TotalIncome - allTimeTotals.TotalExpenses,
            MonthlyIncome: monthTotals.TotalIncome,
            MonthlyExpenses: monthTotals.TotalExpenses));
    }

    public async Task<Result<IReadOnlyList<MonthlyTotalsResponse>>> GetIncomeVsExpensesAsync(
        Guid userId,
        int? months,
        CancellationToken ct = default)
    {
        var monthCount = months ?? DefaultMonths;
        if (monthCount is < 1 or > MaxMonths)
        {
            return Result<IReadOnlyList<MonthlyTotalsResponse>>.Failure(new(
                ApiErrorCode.InvalidReportParameters,
                $"Months must be between 1 and {MaxMonths}."));
        }

        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var from = currentMonthStart.AddMonths(-(monthCount - 1));
        var to = currentMonthStart.AddMonths(1).AddTicks(-1);

        var totals = await transactionRepository.GetMonthlyTotalsAsync(userId, from, to, ct);
        var byPeriod = totals.ToDictionary(t => (t.Year, t.Month));

        // Fill month gaps with zeros so chart lines are continuous
        var results = new List<MonthlyTotalsResponse>(monthCount);
        for (var i = monthCount - 1; i >= 0; i--)
        {
            var monthStart = currentMonthStart.AddMonths(-i);
            byPeriod.TryGetValue((monthStart.Year, monthStart.Month), out var monthTotals);
            results.Add(new MonthlyTotalsResponse(
                monthStart.Year,
                monthStart.Month,
                monthTotals?.TotalIncome ?? 0m,
                monthTotals?.TotalExpenses ?? 0m));
        }

        return Result<IReadOnlyList<MonthlyTotalsResponse>>.Success(results);
    }

    public async Task<Result<IReadOnlyList<CategoryBreakdownResponse>>> GetCategoryBreakdownAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct = default)
    {
        if (startDate is null || endDate is null || startDate.Value.Date > endDate.Value.Date)
        {
            return Result<IReadOnlyList<CategoryBreakdownResponse>>.Failure(new(
                ApiErrorCode.InvalidReportParameters,
                "A valid start and end date range is required."));
        }

        // Normalize to UTC full-day ranges — Npgsql timestamptz rejects Unspecified kind
        var from = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(endDate.Value.Date, DateTimeKind.Utc).AddDays(1).AddTicks(-1);

        var totals = await transactionRepository.GetExpenseTotalsByCategoryAsync(userId, from, to, ct);

        var categoryNames = (await categoryRepository.GetAllByUserAsync(userId, ct))
            .ToDictionary(c => c.Id, c => c.Name);

        var results = totals.Select(t => new CategoryBreakdownResponse(
            t.CategoryId,
            categoryNames.GetValueOrDefault(t.CategoryId, "Unknown"),
            t.Total)).ToList();

        return Result<IReadOnlyList<CategoryBreakdownResponse>>.Success(results);
    }
}
```

3. Create `backend/src/Modules/Reporting/Infrastructure/Services/ReportingService.cs`:

```csharp
using Microsoft.Extensions.Logging;
using Personal.FinanceTracker.Finance.Domain.Interfaces;
using Personal.FinanceTracker.Reporting.Application.Interfaces;
using Personal.FinanceTracker.Reporting.Domain.Entities;
using Personal.FinanceTracker.Reporting.Domain.Interfaces;
using Personal.FinanceTracker.Shared.Constants;
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Reporting.Infrastructure.Services;

public sealed class ReportingService(
    ITransactionRepository transactionRepository,
    IMonthlySummaryRepository monthlySummaryRepository,
    ILogger<ReportingService> logger) : IReportingService
{
    public async Task<Result<int>> GenerateMonthlyReportsAsync(
        int year,
        int month,
        CancellationToken ct = default)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
        {
            return Result<int>.Failure(new(
                ApiErrorCode.InvalidReportParameters, "Invalid year or month."));
        }

        var totals = await transactionRepository.GetUserMonthlyTotalsAsync(year, month, ct);

        var generated = 0;
        foreach (var userTotals in totals)
        {
            var existing = await monthlySummaryRepository
                .GetByUserAndPeriodAsync(userTotals.UserId, year, month, ct);

            if (existing is null)
            {
                await monthlySummaryRepository.AddAsync(
                    MonthlySummary.Create(userTotals.UserId, year, month, userTotals.TotalIncome, userTotals.TotalExpenses),
                    ct);
            }
            else
            {
                existing.Update(userTotals.TotalIncome, userTotals.TotalExpenses);
            }

            generated++;
        }

        await monthlySummaryRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Generated {Count} monthly summaries for {Year}-{Month:00}",
            generated, year, month);

        return Result<int>.Success(generated);
    }
}
```

4. Register everything in `backend/src/Modules/Reporting/DependencyInjection.cs` — add alongside the DbContext registration:

```csharp
// Repositories
services.AddScoped<IMonthlySummaryRepository, MonthlySummaryRepository>();

// Services
services.AddScoped<IDashboardService, DashboardService>();
services.AddScoped<IReportingService, ReportingService>();
```

   With usings: `Personal.FinanceTracker.Reporting.Application.Interfaces`, `Personal.FinanceTracker.Reporting.Domain.Interfaces`, `Personal.FinanceTracker.Reporting.Infrastructure.Repositories`, `Personal.FinanceTracker.Reporting.Infrastructure.Services`.

5. Run `dotnet build` — confirm 0 errors, 0 new warnings.

**Success Criteria:**
- Services consume only Finance's repository interfaces and Reporting's own — no `FinanceDbContext`, no entity types from Finance
- All date math uses `DateTimeKind.Utc`; end dates are inclusive (end-of-day)
- Income-vs-expenses fills gaps so the response always has exactly `months` contiguous rows
- `GenerateMonthlyReportsAsync` is idempotent — running twice for the same period updates rows, never duplicates (unique index backs this)
- `Result<T>` failures use `InvalidReportParameters` with user-safe descriptions

---

### Task 10 — Reporting: ReportingEndpoints

**Status:** New

**Description:**
Create the reporting endpoints — three authenticated GETs under `/api/reports`, wrapped in `ApiResponse<T>`, mapping `InvalidReportParameters` failures to enveloped 400s. No business logic in handlers. Naming note: the endpoints class itself owns the `MapReportingEndpoints` extension (there is exactly one endpoints class in this module, so the Finance-style DI wrapper would create two identical extension methods).

**Steps:**

1. Create `backend/src/Modules/Reporting/Api/Endpoints/ReportingEndpoints.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Personal.FinanceTracker.Reporting.Application.DTOs.Requests;
using Personal.FinanceTracker.Reporting.Application.DTOs.Responses;
using Personal.FinanceTracker.Reporting.Application.Interfaces;
using Personal.FinanceTracker.Shared.Constants;
using Personal.FinanceTracker.Shared.Extensions;
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Reporting.Api.Endpoints;

public static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports")
            .WithTags("Reports")
            .RequireAuthorization();

        group.MapGet("/dashboard/summary", GetSummaryAsync)
            .WithName("GetDashboardSummary")
            .WithDescription("Get total balance and current-month income/expense totals for the authenticated user.");

        group.MapGet("/dashboard/income-vs-expenses", GetIncomeVsExpensesAsync)
            .WithName("GetIncomeVsExpenses")
            .WithDescription("Get monthly income and expense totals for the last N months (default 6, max 24).");

        group.MapGet("/dashboard/category-breakdown", GetCategoryBreakdownAsync)
            .WithName("GetCategoryBreakdown")
            .WithDescription("Get expense totals grouped by category for a date range.");

        return app;
    }

    private static async Task<Ok<ApiResponse<DashboardSummaryResponse>>> GetSummaryAsync(
        ClaimsPrincipal user,
        IDashboardService dashboardService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await dashboardService.GetSummaryAsync(userId, ct);

        return TypedResults.Ok(new ApiResponse<DashboardSummaryResponse>
        {
            IsOk = true,
            Data = result.Value,
            StatusCode = StatusCodes.Status200OK,
            CodeText = "OK"
        });
    }

    private static async Task<Results<Ok<ApiResponse<IReadOnlyList<MonthlyTotalsResponse>>>, BadRequest<ApiResponse<IReadOnlyList<MonthlyTotalsResponse>>>>> GetIncomeVsExpensesAsync(
        ClaimsPrincipal user,
        [AsParameters] IncomeVsExpensesQueryParams queryParams,
        IDashboardService dashboardService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await dashboardService.GetIncomeVsExpensesAsync(userId, queryParams.Months, ct);

        if (result.IsFailure)
        {
            return TypedResults.BadRequest(new ApiResponse<IReadOnlyList<MonthlyTotalsResponse>>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Invalid Report Parameters",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = result.Error?.Description,
                },
                StatusCode = StatusCodes.Status400BadRequest,
                CodeText = "BAD_REQUEST"
            });
        }

        return TypedResults.Ok(new ApiResponse<IReadOnlyList<MonthlyTotalsResponse>>
        {
            IsOk = true,
            Data = result.Value,
            StatusCode = StatusCodes.Status200OK,
            CodeText = "OK"
        });
    }

    private static async Task<Results<Ok<ApiResponse<IReadOnlyList<CategoryBreakdownResponse>>>, BadRequest<ApiResponse<IReadOnlyList<CategoryBreakdownResponse>>>>> GetCategoryBreakdownAsync(
        ClaimsPrincipal user,
        [AsParameters] CategoryBreakdownQueryParams queryParams,
        IDashboardService dashboardService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await dashboardService.GetCategoryBreakdownAsync(userId, queryParams.StartDate, queryParams.EndDate, ct);

        if (result.IsFailure)
        {
            return TypedResults.BadRequest(new ApiResponse<IReadOnlyList<CategoryBreakdownResponse>>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Invalid Report Parameters",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = result.Error?.Description,
                },
                StatusCode = StatusCodes.Status400BadRequest,
                CodeText = "BAD_REQUEST"
            });
        }

        return TypedResults.Ok(new ApiResponse<IReadOnlyList<CategoryBreakdownResponse>>
        {
            IsOk = true,
            Data = result.Value,
            StatusCode = StatusCodes.Status200OK,
            CodeText = "OK"
        });
    }
}
```

2. Call it from the module DI — add to `MapReportingEndpoints` wiring in `DependencyInjection.cs` (this is the one place the Finance pattern differs — see task Description):

```csharp
using Microsoft.AspNetCore.Routing;
using Personal.FinanceTracker.Reporting.Api.Endpoints;

public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder app)
{
    app.MapReportingEndpoints(); // hmm — see note below
    return app;
}
```

   **Do not add this wrapper.** `ReportingEndpoints.MapReportingEndpoints` and a `DependencyInjection.MapReportingEndpoints` would be two extension methods with the same name and signature on `IEndpointRouteBuilder` — an ambiguity. The endpoints class already provides the extension `Program.cs` needs. Leave the wrapper out entirely; `Program.cs` calls `app.MapReportingEndpoints()` directly (Task 11).

3. Run `dotnet build` — confirm 0 errors, 0 new warnings.

**Success Criteria:**
- All three endpoints use `TypedResults`, `RequireAuthorization` (group level), and the `ApiResponse<T>` envelope
- `GetUserId()` on every handler — nothing is user-agnostic
- Validation failures return enveloped 400 with `INVALID_REPORT_PARAMETERS` surfaced by the middleware/error model
- No `ValidationFilter<T>` on GETs — query params validate through the service `Result` path
- Scalar (`/scalar` in Development) shows the three endpoints under the "Reports" tag

---

### Task 11 — Reporting: Program.cs Wire-Up

**Status:** New

**Description:**
Replace the two `TODO Sprint 4` placeholders in `Program.cs` with the real module registration. This is the exact hook the file has been waiting for since Sprint 0.

**Steps:**

1. In `backend/src/Personal.FinanceTracker.Api/Program.cs`:

   Add the using:

```csharp
using Personal.FinanceTracker.Reporting;
```

   Replace line 54:

```csharp
// TODO Sprint 4: builder.Services.AddReportingModule(builder.Configuration);
```

   with:

```csharp
builder.Services.AddReportingModule(builder.Configuration);
```

   Replace line 87:

```csharp
// TODO Sprint 4: app.MapReportingEndpoints();
```

   with:

```csharp
app.MapReportingEndpoints();
```

2. Run `dotnet build`, then start the API (`dotnet run --project backend/src/Personal.FinanceTracker.Api`) and verify:
   - `http://localhost:5194/health/live` responds
   - Scalar at `http://localhost:5194/scalar` lists the Reports tag with the three endpoints
   - Unauthenticated `GET /api/reports/dashboard/summary` returns 401; an authenticated call (copy a bearer token from the frontend dev tools or use Scalar's auth) returns an enveloped summary

**Success Criteria:**
- Both `TODO Sprint 4` comments are gone; no other `TODO Sprint 4` remains in the codebase
- Reporting endpoints respond before TickerQ exists (jobs are Tasks 12–13 — nothing here depends on them)
- `GET /api/reports/dashboard/summary` returns `{ isOk: true, data: { totalBalance, monthlyIncome, monthlyExpenses } }` for a user with transactions

---

### Task 12 — TickerQ: Registration, Operational Store, and Migration

**Status:** New

**Description:**
Wire TickerQ into the host: `AddTickerQ` with the EF Core operational store attached to `ReportingDbContext` (tables in the `ticker` schema), `app.UseTickerQ()`, and the `AddTickerQTables` migration. This resolves the `SPRINTS-OVERVIEW.md` Known Gap "TickerQ requires PostgreSQL backing store — needs migration".

**Steps:**

1. In `backend/src/Personal.FinanceTracker.Api/Program.cs`, after `AddReportingModule` (order matters — the operational store resolves `ReportingDbContext` from DI) and before `var app = builder.Build();`:

```csharp
using Personal.FinanceTracker.Reporting.Infrastructure.Data;
using TickerQ.DependencyInjection;
// The AddOperationalStore/ConfigurationType extensions come from TickerQ.EntityFrameworkCore —
// add the using the compiler asks for (check the package docs: https://tickerq.net/docs/entity-framework/installation)

// TickerQ background jobs — EF Core persistence sharing ReportingDbContext (tables in the "ticker" schema)
builder.Services.AddTickerQ(options =>
{
    options.AddOperationalStore(ef =>
    {
        ef.UseApplicationDbContext<ReportingDbContext>(ConfigurationType.UseModelCustomizer);
        ef.SetSchema("ticker");
    });
});
```

   The `UseModelCustomizer` configuration injects TickerQ's entity configurations into `ReportingDbContext`'s model during migrations — **no changes to `ReportingDbContext` itself** (verified: https://tickerq.net/docs/entity-framework/dbcontext, "Option 2: Application DbContext — UseModelCustomizer (recommended): Your DbContext stays untouched").

2. After the endpoint mapping (next to `app.MapReportingEndpoints();`), activate the scheduler:

```csharp
app.UseTickerQ();
```

   `UseTickerQ` activates the job processor — it is not HTTP middleware, so its position in the pipeline is not critical. The official quick start shows it directly after `Build()`; either placement is valid.

3. Generate the migration (from `backend/`):

```bash
dotnet ef migrations add AddTickerQTables \
  --project src/Modules/Reporting/Personal.FinanceTracker.Reporting.csproj \
  --startup-project src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj \
  --context ReportingDbContext \
  --output-dir Infrastructure/Data/Migrations
```

4. Review the generated migration — it must create `ticker.time_tickers`, `ticker.cron_tickers`, and `ticker.cron_ticker_occurrences` (exact names per TickerQ's configurations) and **nothing else** — `reports.monthly_summaries` already exists from Task 7.

5. Apply it:

```bash
dotnet ef database update \
  --project src/Modules/Reporting/Personal.FinanceTracker.Reporting.csproj \
  --startup-project src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj \
  --context ReportingDbContext
```

6. **Fallback path (only if the model customizer misbehaves):** switch to a dedicated context with migrations landing in the Api project:

```csharp
options.AddOperationalStore(ef =>
{
    ef.UseTickerQDbContext<TickerQDbContext>(db =>
        db.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            npgsql => npgsql.MigrationsAssembly("Personal.FinanceTracker.Api")),
        schema: "ticker");
});
```

   Then `dotnet ef migrations add InitTickerQ --context TickerQDbContext --startup-project src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj --output-dir Migrations/TickerQ` (per https://tickerq.net/docs/entity-framework/migrations). Document any switch in the sprint completion record.

**Success Criteria:**
- `ticker` schema exists in PostgreSQL with TickerQ's three tables
- Startup logs show TickerQ initializing (scheduler started; no cron jobs seeded yet — jobs arrive in Task 13)
- `reports.monthly_summaries` untouched by this migration
- No new build warnings

---

### Task 13 — TickerQ: MonthlyReportJob and BudgetAlertJob

**Status:** New

**Description:**
Define the two background jobs. TickerQ jobs are plain classes — no interface, no manual registration: the `[TickerFunction]` attribute with a `cronExpression` is discovered at compile time by the source generator and **auto-seeded** as a `CronTicker` on startup (verified: https://tickerq.net/docs/guides/defining-jobs/attribute and https://tickerq.net/docs/guides/configuration#seeding). Constructor injection is first-class.

**Steps:**

1. Create `backend/src/Modules/Reporting/Jobs/MonthlyReportJob.cs`:

```csharp
using Microsoft.Extensions.Logging;
using Personal.FinanceTracker.Reporting.Application.Interfaces;
using TickerQ.Utilities.Base;

namespace Personal.FinanceTracker.Reporting.Jobs;

public sealed class MonthlyReportJob(
    IReportingService reportingService,
    ILogger<MonthlyReportJob> logger)
{
    [TickerFunction("generate-monthly-reports", cronExpression: "0 0 1 * *")]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var previousMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1);

        logger.LogInformation(
            "MonthlyReportJob starting for {Year}-{Month:00}",
            previousMonth.Year, previousMonth.Month);

        var result = await reportingService.GenerateMonthlyReportsAsync(
            previousMonth.Year, previousMonth.Month, ct);

        if (result.IsFailure)
        {
            logger.LogError(
                "MonthlyReportJob failed for {Year}-{Month:00}: {Error}",
                previousMonth.Year, previousMonth.Month, result.Error?.Description);
            return;
        }

        logger.LogInformation(
            "MonthlyReportJob completed: {Count} summaries persisted",
            result.Value);
    }
}
```

2. Create `backend/src/Modules/Finance/Jobs/BudgetAlertJob.cs`:

```csharp
using Microsoft.Extensions.Logging;
using Personal.FinanceTracker.Finance.Application.DTOs.Responses;
using Personal.FinanceTracker.Finance.Application.Interfaces;
using TickerQ.Utilities.Base;

namespace Personal.FinanceTracker.Finance.Jobs;

public sealed class BudgetAlertJob(
    IBudgetService budgetService,
    ILogger<BudgetAlertJob> logger)
{
    private const decimal AlertThresholdPercentage = 80m;

    [TickerFunction("check-budget-alerts", cronExpression: "0 */6 * * *")]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("BudgetAlertJob starting with threshold {Threshold}%", AlertThresholdPercentage);

        var result = await budgetService.GetBudgetsNearLimitAsync(AlertThresholdPercentage, ct);
        if (result.IsFailure)
        {
            logger.LogError("BudgetAlertJob failed: {Error}", result.Error?.Description);
            return;
        }

        foreach (var budget in result.Value)
        {
            logger.LogWarning(
                "Budget alert: user {UserId}, budget \"{BudgetName}\" ({CategoryName}) at {Percentage}% of {Period} limit — spent {Spent} of {Limit}",
                budget.CategoryId, // NOTE: replace with the budget's user id — see step 3
                budget.Name,
                budget.CategoryName,
                budget.PercentageUsed,
                budget.Period,
                budget.SpentAmount,
                budget.LimitAmount);
        }

        logger.LogInformation("BudgetAlertJob completed: {Count} budgets at or above threshold", result.Value.Count);
    }
}
```

3. **Alert payload check:** `BudgetWithSpendingResponse` (Sprint 3) does not carry `UserId`. The alert log needs it. Extend `BudgetWithSpendingResponse` with a `UserId` field and populate it in `MapToWithSpending` (BudgetService) — it is already available on the `Budget` entity. Then log `budget.UserId` in the job (fix the NOTE placeholder in the sample above). Keep the frontend type (`BudgetWithSpending` in `src/types/finance.ts`) unchanged — an extra serialized field is additive and ignored by the frontend; do **not** mirror it (Task 15 only mirrors the reporting DTOs).

   If you judge the extra field on a public response to be worse than a dedicated internal record, the alternative is a small `BudgetAlert` record returned by `GetBudgetsNearLimitAsync` instead of `BudgetWithSpendingResponse` — pick one and note the choice in the completion record. The sample above assumes the additive field.

4. Verify seeding and execution:
   - Start the API. Startup logs must show the two cron tickers seeded (`generate-monthly-reports`, `check-budget-alerts`).
   - Query `ticker.cron_tickers` in PostgreSQL — both rows present with the expected cron expressions and `is_enabled = true`.
   - To observe an actual run within a sprint session (the real schedules are hours/months apart), **temporarily** change one cron expression to `*/2 * * * *` (every 2 minutes) locally, confirm execution + `reports.monthly_summaries` rows (after inserting transactions in a past month), then **revert before committing**. Never commit the temporary cron.

5. Run `dotnet build` — confirm 0 errors, 0 new warnings.

**Success Criteria:**
- Cron expressions: `0 0 1 * *` (1st of month, midnight) and `0 */6 * * *` (every 6 hours) — 5-part format, auto-expanded by TickerQ
- Jobs contain no business logic beyond orchestration + logging — all work lives in the services
- Failures are logged and returned from gracefully — a failing job run never throws unhandled
- Both tickers seeded in `ticker.cron_tickers` on first startup; a manual run persists/updates `reports.monthly_summaries` correctly
- Committed cron expressions are the production schedules (temporary test crons reverted)

---

### Task 14 — Backend: Reporting.UnitTests and Finance Test Additions

**Status:** New

**Description:**
Create the `Reporting.UnitTests` xUnit project (mirroring `Finance.UnitTests` structure and packages) and add the Sprint 4 test coverage: `MonthlySummary` domain tests, `DashboardService` and `ReportingService` unit tests with NSubstitute mocks, and `BudgetServiceTests.GetBudgetsNearLimitAsync` coverage in `Finance.UnitTests`.

**Steps:**

1. Create `backend/tests/Reporting.UnitTests/` mirroring `Finance.UnitTests`:
   - `Personal.FinanceTracker.Reporting.UnitTests.csproj` — same test packages (`Microsoft.NET.Test.Sdk` 17.13.0, `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.0, `NSubstitute` 5.3.0, `FluentAssertions` 8.2.0), project reference to `Personal.FinanceTracker.Reporting.csproj`
   - `Usings.cs` with the same global usings as `Finance.UnitTests` (`global using FluentAssertions;` + xUnit usings)
   - Register in the solution: `dotnet sln Personal.FinanceTracker.slnx add tests/Reporting.UnitTests/Personal.FinanceTracker.Reporting.UnitTests.csproj`

2. Create `Domain/Entities/MonthlySummaryTests.cs` — cover:
   - `Create_WithValidInputs_SetsNetAmountAndDates`
   - `Create_WithInvalidYear_ThrowsArgumentException` (and month / negative totals / empty userId variants)
   - `Update_RecalculatesNetAmount`

3. Create `Application/Services/DashboardServiceTests.cs`. Mock `ITransactionRepository` + `ICategoryRepository`. Representative sample:

```csharp
public class DashboardServiceTests
{
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly ILogger<DashboardService> _logger = Substitute.For<ILogger<DashboardService>>();

    private DashboardService CreateSut() => new(_transactionRepository, _categoryRepository, _logger);

    [Fact]
    public async Task GetSummaryAsync_WithMonthAndAllTimeTotals_ComputesBalanceAndMonthlyTotals()
    {
        var userId = Guid.NewGuid();
        _transactionRepository.GetTotalsByTypeAsync(userId, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var from = (DateTime?)callInfo[1];
                return from is null
                    ? new TransactionTypeTotals(1000m, 400m)   // all-time
                    : new TransactionTypeTotals(500m, 300m);    // current month
            });

        var result = await CreateSut().GetSummaryAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalBalance.Should().Be(600m);
        result.Value.MonthlyIncome.Should().Be(500m);
        result.Value.MonthlyExpenses.Should().Be(300m);
        await _transactionRepository.Received(2)
            .GetTotalsByTypeAsync(userId, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    // Also cover:
    // GetIncomeVsExpensesAsync_WithNullMonths_DefaultsTo6ContiguousRows (gap-filled zeros)
    // GetIncomeVsExpensesAsync_WithMonthsOutOfRange_ReturnsInvalidReportParameters (0 and 25)
    // GetCategoryBreakdownAsync_WithMissingDates_ReturnsInvalidReportParameters
    // GetCategoryBreakdownAsync_WithStartAfterEnd_ReturnsInvalidReportParameters
    // GetCategoryBreakdownAsync_WithUnknownCategoryId_FallsBackToUnknownName
    // GetCategoryBreakdownAsync_NormalizesEndDateToEndOfDay (capture the from/to args with Arg.Do and assert)
}
```

4. Create `Application/Services/ReportingServiceTests.cs` — cover:
   - `GenerateMonthlyReportsAsync_WhenSummaryMissing_CreatesSummary`
   - `GenerateMonthlyReportsAsync_WhenSummaryExists_UpdatesSummary` (assert `AddAsync` not called)
   - `GenerateMonthlyReportsAsync_WithInvalidMonth_ReturnsInvalidReportParameters`
   - `GenerateMonthlyReportsAsync_WithNoTransactions_PersistsNothingAndReturnsZero`

5. Add `Application/Services/BudgetServiceTests.cs` to `Finance.UnitTests` (the class doesn't exist yet — the rest of `BudgetService` coverage remains Sprint 5 scope):
   - `GetBudgetsNearLimitAsync_WithThresholdOutOfRange_ReturnsInvalidReportParameters`
   - `GetBudgetsNearLimitAsync_WithBudgetsAboveThreshold_ReturnsOnlyThoseBudgets`
   - `GetBudgetsNearLimitAsync_WithNoActiveBudgets_ReturnsEmptyList`

6. Run `dotnet test backend/Personal.FinanceTracker.slnx` — all existing 189 tests plus the new ones pass, 0 new warnings.

**Success Criteria:**
- `Reporting.UnitTests` registered in the solution and passing
- Date/UTC normalization and month-gap filling are asserted via argument capture — not just response shape
- Upsert semantics (create vs update paths) both covered
- No test touches a database, HTTP, or TickerQ — pure unit tests with mocks

---

### Task 15 — Frontend: Reporting Type Definitions

**Status:** New

**Description:**
Create `src/types/reporting.ts` mirroring the backend DTOs exactly. Reporting is a separate domain — it gets its own type file, like `finance.ts` and `auth.ts`.

**Steps:**

1. Create `frontend/src/types/reporting.ts`:

```typescript
export interface DashboardSummary {
  totalBalance: number;
  monthlyIncome: number;
  monthlyExpenses: number;
}

export interface MonthlyTotals {
  year: number;
  month: number;
  income: number;
  expenses: number;
}

export interface CategoryBreakdown {
  categoryId: string;
  categoryName: string;
  total: number;
}
```

2. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- Types mirror the backend response DTOs exactly (camelCase JSON)
- No duplication of finance types — `BudgetWithSpending` is NOT extended to match Task 13's additive `userId` field (frontend ignores it)

---

### Task 16 — Frontend: getCurrentMonthRange Utility

**Status:** New

**Description:**
Create `src/utils/dates.ts` with `getCurrentMonthRange()` — the first date-fns usage in the app (formatting elsewhere uses native `Intl` via `formatters.ts`; range math is where date-fns earns its place).

**Steps:**

1. Create `frontend/src/utils/dates.ts`:

```typescript
import { endOfMonth, format, startOfMonth } from "date-fns";

export interface DateRange {
  startDate: string;
  endDate: string;
}

export function getCurrentMonthRange(): DateRange {
  const now = new Date();
  return {
    startDate: format(startOfMonth(now), "yyyy-MM-dd"),
    endDate: format(endOfMonth(now), "yyyy-MM-dd"),
  };
}
```

2. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- Returns `yyyy-MM-dd` strings — exactly what `reportsApi.getCategoryBreakdown` sends and the backend binds
- One exported type (`DateRange`) reusable by the Reports page month picker

---

### Task 17 — Frontend: reportsApi Service Module

**Status:** New

**Description:**
Create the `reportsApi` object in `src/api/reports.ts`, mirroring `transactionsApi`/`budgetsApi`: fetch-based `apiClient`, full `ApiResponse<T>` envelopes, no unwrapping, no `/api` prefix.

**Steps:**

1. Create `frontend/src/api/reports.ts`:

```typescript
import { apiClient } from "@/api/client";
import type { ApiResponse } from "@/types/http";
import type {
  CategoryBreakdown,
  DashboardSummary,
  MonthlyTotals,
} from "@/types/reporting";

export const reportsApi = {
  getDashboardSummary: (): Promise<ApiResponse<DashboardSummary>> =>
    apiClient.get<DashboardSummary>("/reports/dashboard/summary"),

  getIncomeVsExpenses: (months: number): Promise<ApiResponse<MonthlyTotals[]>> =>
    apiClient.get<MonthlyTotals[]>(
      `/reports/dashboard/income-vs-expenses?months=${encodeURIComponent(String(months))}`,
    ),

  getCategoryBreakdown: (
    startDate: string,
    endDate: string,
  ): Promise<ApiResponse<CategoryBreakdown[]>> =>
    apiClient.get<CategoryBreakdown[]>(
      `/reports/dashboard/category-breakdown?startDate=${encodeURIComponent(startDate)}&endDate=${encodeURIComponent(endDate)}`,
    ),
};
```

2. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- Same shape as the other API modules; returns the full envelope
- Query parameters URL-encoded; no `any`; no manual auth headers

---

### Task 18 — Frontend: useDashboard Hooks and Cross-Feature Invalidation

**Status:** New

**Description:**
Create the dashboard TanStack Query hooks with a `dashboardKeys` factory, and add dashboard invalidation to the existing transaction and category mutation hooks — dashboard aggregates are computed from transactions and labeled by categories, so those mutations make dashboard data stale.

**Steps:**

1. Create `frontend/src/features/dashboard/hooks/useDashboard.ts`:

```typescript
import { useQuery } from '@tanstack/react-query';
import { reportsApi } from '@/api/reports';

export const dashboardKeys = {
  all: ['dashboard'] as const,
  summary: () => [...dashboardKeys.all, 'summary'] as const,
  incomeVsExpenses: (months: number) => [...dashboardKeys.all, 'income-vs-expenses', months] as const,
  categoryBreakdown: (startDate: string, endDate: string) =>
    [...dashboardKeys.all, 'category-breakdown', startDate, endDate] as const,
};

export function useDashboardSummary() {
  return useQuery({
    queryKey: dashboardKeys.summary(),
    queryFn: () => reportsApi.getDashboardSummary(),
    staleTime: 1000 * 60 * 2, // 2 minutes — money overview changes with transactions
  });
}

export function useIncomeVsExpenses(months: number = 6) {
  return useQuery({
    queryKey: dashboardKeys.incomeVsExpenses(months),
    queryFn: () => reportsApi.getIncomeVsExpenses(months),
  });
}

export function useCategoryBreakdown(startDate: string, endDate: string) {
  return useQuery({
    queryKey: dashboardKeys.categoryBreakdown(startDate, endDate),
    queryFn: () => reportsApi.getCategoryBreakdown(startDate, endDate),
    enabled: startDate !== '' && endDate !== '',
  });
}
```

2. In `frontend/src/features/transactions/hooks/useTransactions.ts`, add dashboard invalidation to **all three** mutation hooks (create, update, delete), exactly like the existing `budgetKeys.all` invalidation:

```typescript
import { dashboardKeys } from '@/features/dashboard/hooks/useDashboard';

// inside each mutation's onSuccess:
onSuccess: () => {
  void queryClient.invalidateQueries({ queryKey: transactionKeys.lists() });
  void queryClient.invalidateQueries({ queryKey: budgetKeys.all });
  void queryClient.invalidateQueries({ queryKey: dashboardKeys.all });
},
```

3. In `frontend/src/features/categories/hooks/useCategories.ts`, add the same `dashboardKeys.all` invalidation to the **update and delete** mutations only — creating a category cannot change any aggregate (no transactions reference it yet), but renames change breakdown labels and deletes change label fallbacks.

4. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- `dashboardKeys` factory used everywhere; no hardcoded `['dashboard']` strings outside the factory
- Summary hook has 2-minute `staleTime`; breakdown hook is `enabled`-guarded on both dates
- Transaction mutations invalidate transactions + budgets + dashboard; category update/delete invalidate dashboard
- Cross-feature hook imports follow the established pattern (transactions already imports from budgets) — dashboard does not import from transactions, so no circular-import risk
- No TanStack Query calls outside hook files

---

### Task 19 — Frontend: OverviewCards Component

**Status:** New

**Description:**
Create `OverviewCards` — three summary stat cards (Total Balance, Monthly Income, Monthly Expenses) with skeleton loading. Tailwind-only, lucide icons, `formatCurrency` from the shared formatters. The docs/03 sample used an MUI `Card`; this version uses the as-built plain-div card pattern.

**Steps:**

1. Create `frontend/src/features/dashboard/components/OverviewCards.tsx`:

```typescript
import { TrendingDown, TrendingUp, Wallet } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import type { DashboardSummary } from "@/types/reporting";
import { formatCurrency } from "@/utils/formatters";

interface OverviewCardsProps {
  summary: DashboardSummary | null;
  isLoading: boolean;
}

interface SummaryCard {
  title: string;
  value: number;
  icon: LucideIcon;
  iconBg: string;
  valueText: string;
}

export function OverviewCards({ summary, isLoading }: OverviewCardsProps) {
  if (isLoading) {
    return (
      <div className="grid grid-cols-1 gap-4 md:grid-cols-3" aria-busy="true">
        {[1, 2, 3].map((i) => (
          <div key={i} className="h-28 animate-pulse rounded-lg bg-gray-100" />
        ))}
      </div>
    );
  }

  const cards: SummaryCard[] = [
    {
      title: "Total Balance",
      value: summary?.totalBalance ?? 0,
      icon: Wallet,
      iconBg: "bg-indigo-500",
      valueText: "text-indigo-600",
    },
    {
      title: "Monthly Income",
      value: summary?.monthlyIncome ?? 0,
      icon: TrendingUp,
      iconBg: "bg-green-500",
      valueText: "text-green-600",
    },
    {
      title: "Monthly Expenses",
      value: summary?.monthlyExpenses ?? 0,
      icon: TrendingDown,
      iconBg: "bg-red-500",
      valueText: "text-red-600",
    },
  ];

  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
      {cards.map((card) => (
        <div
          key={card.title}
          className="rounded-lg border border-gray-200 bg-white px-4 py-5"
        >
          <div className="flex items-center justify-between gap-3">
            <div className="min-w-0">
              <p className="text-sm font-medium text-gray-500">{card.title}</p>
              <p className={`truncate text-2xl font-bold ${card.valueText}`}>
                {formatCurrency(card.value)}
              </p>
            </div>
            <div className={`shrink-0 rounded-full p-3 ${card.iconBg}`}>
              <card.icon className="h-6 w-6 text-white" aria-hidden="true" />
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
```

2. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- Mobile-first: cards stack on `xs`, `md:grid-cols-3` from tablet up
- Tailwind class strings are complete literals (array entries are static strings so the JIT compiler sees them)
- Negative balances render correctly (formatCurrency handles negatives)
- Icons are decorative (`aria-hidden`) — the card title carries the meaning

---

### Task 20 — Frontend: IncomeExpenseChart Component

**Status:** New

**Description:**
Create the income-vs-expenses line chart with Recharts (the installed library — the docs/03 `react-chartjs-2` sample is rewritten 1:1 in intent: green income line, red expenses line, currency-formatted axis and tooltips). Fetches its own data via `useIncomeVsExpenses` and owns its loading/error/empty states.

> **SVG color exception:** chart series and grid colors are concrete hex values (`#10B981`, `#EF4444`, `#E5E7EB`) — Tailwind classes cannot style Recharts' generated SVG attributes. This is the accepted exception, like Sprint 3's runtime progress-bar width.

**Steps:**

1. Create `frontend/src/features/dashboard/components/IncomeExpenseChart.tsx`:

```typescript
import { format } from "date-fns";
import {
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { useIncomeVsExpenses } from "@/features/dashboard/hooks/useDashboard";
import { formatCurrency } from "@/utils/formatters";

interface IncomeExpenseChartProps {
  months?: number;
}

interface ChartRow {
  label: string;
  Income: number;
  Expenses: number;
}

export function IncomeExpenseChart({ months = 6 }: IncomeExpenseChartProps) {
  const { data: response, isLoading, error } = useIncomeVsExpenses(months);

  if (isLoading) {
    return (
      <div
        className="h-96 animate-pulse rounded-lg bg-gray-100"
        aria-label="Loading income vs expenses chart"
      />
    );
  }

  if (error) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
        Failed to load income vs expenses. Please try again.
      </div>
    );
  }

  const rows: ChartRow[] = (response?.data ?? []).map((m) => ({
    label: format(new Date(m.year, m.month - 1, 1), "MMM yy"),
    Income: m.income,
    Expenses: m.expenses,
  }));

  return (
    <div className="rounded-lg border border-gray-200 bg-white px-4 py-5">
      <h2 className="mb-4 text-lg font-semibold text-gray-900">
        Income vs Expenses
      </h2>
      {rows.length === 0 ? (
        <p className="py-8 text-center text-sm text-gray-500">
          No data available yet. Add some transactions to see the trend.
        </p>
      ) : (
        <div
          className="h-80"
          role="img"
          aria-label={`Line chart of monthly income versus expenses over the last ${months} months`}
        >
          <ResponsiveContainer width="100%" height="100%">
            <LineChart data={rows} margin={{ top: 8, right: 16, bottom: 0, left: 8 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />
              <XAxis dataKey="label" tick={{ fontSize: 12, fill: "#6B7280" }} />
              <YAxis
                width={96}
                tick={{ fontSize: 12, fill: "#6B7280" }}
                tickFormatter={(value: number) => formatCurrency(value)}
              />
              <Tooltip formatter={(value) => formatCurrency(Number(value))} />
              <Legend />
              <Line
                type="monotone"
                dataKey="Income"
                stroke="#10B981"
                strokeWidth={2}
                dot={false}
              />
              <Line
                type="monotone"
                dataKey="Expenses"
                stroke="#EF4444"
                strokeWidth={2}
                dot={false}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  );
}
```

2. Run `npm run build` — confirm 0 TypeScript errors. If Recharts' strict typings reject the `Tooltip` formatter's inferred parameter, annotate it as `(value: number | string) => formatCurrency(Number(value))`.

**Success Criteria:**
- Component owns loading / error / empty states — the parent never branches for them
- Month labels localize via date-fns `format` (`"MMM yy"`); amounts via the shared `formatCurrency`
- Chart is accessible (`role="img"` + descriptive `aria-label`)
- Props allow the Reports page to reuse it with a longer window

---

### Task 21 — Frontend: SpendingPieChart Component

**Status:** New

**Description:**
Create the spending-by-category donut chart with Recharts, parameterized by date range so the Dashboard (current month) and Reports (selected month) can share it.

**Steps:**

1. Create `frontend/src/features/dashboard/components/SpendingPieChart.tsx`:

```typescript
import { Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts";
import { useCategoryBreakdown } from "@/features/dashboard/hooks/useDashboard";
import { formatCurrency } from "@/utils/formatters";

const COLORS = [
  "#3B82F6", // blue
  "#EF4444", // red
  "#10B981", // green
  "#F59E0B", // yellow
  "#8B5CF6", // purple
  "#EC4899", // pink
  "#06B6D4", // cyan
  "#F97316", // orange
];

interface SpendingPieChartProps {
  startDate: string;
  endDate: string;
}

interface ChartSlice {
  name: string;
  value: number;
}

export function SpendingPieChart({ startDate, endDate }: SpendingPieChartProps) {
  const { data: response, isLoading, error } = useCategoryBreakdown(startDate, endDate);

  if (isLoading) {
    return (
      <div
        className="h-96 animate-pulse rounded-lg bg-gray-100"
        aria-label="Loading spending by category chart"
      />
    );
  }

  if (error) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
        Failed to load spending by category. Please try again.
      </div>
    );
  }

  const slices: ChartSlice[] = (response?.data ?? []).map((c) => ({
    name: c.categoryName,
    value: c.total,
  }));

  return (
    <div className="rounded-lg border border-gray-200 bg-white px-4 py-5">
      <h2 className="mb-4 text-lg font-semibold text-gray-900">
        Spending by Category
      </h2>
      {slices.length === 0 ? (
        <p className="py-8 text-center text-sm text-gray-500">
          No spending data for this period.
        </p>
      ) : (
        <div
          className="h-80"
          role="img"
          aria-label="Pie chart of expenses by category for the selected period"
        >
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={slices}
                dataKey="value"
                nameKey="name"
                innerRadius={60}
                outerRadius={95}
                paddingAngle={2}
              >
                {slices.map((slice, index) => (
                  <Cell key={slice.name} fill={COLORS[index % COLORS.length]} />
                ))}
              </Pie>
              <Tooltip formatter={(value) => formatCurrency(Number(value))} />
              <Legend
                verticalAlign="bottom"
                height={36}
                formatter={(value) => <span className="text-xs text-gray-600">{value}</span>}
              />
            </PieChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  );
}
```

2. Run `npm run build` — confirm 0 TypeScript errors (same `Tooltip` formatter typing note as Task 20).

**Success Criteria:**
- Date range arrives via props — the component never derives ranges itself
- Color palette cycles safely past 8 categories (`% COLORS.length`)
- Loading / error / empty states handled internally, matching `IncomeExpenseChart`
- Legend and tooltip render localized currency

---

### Task 22 — Frontend: DashboardPage and Index Route

**Status:** New

**Description:**
Assemble the Dashboard page and wire it to the router's index route, replacing the "coming in Sprint 4" placeholder. Read-only page: query error renders an inline banner (the `BudgetList` pattern), no mutation plumbing, `setDocumentTitle`.

**Steps:**

1. Create `frontend/src/features/dashboard/pages/DashboardPage.tsx`:

```typescript
import { useEffect } from "react";
import { IncomeExpenseChart } from "@/features/dashboard/components/IncomeExpenseChart";
import { OverviewCards } from "@/features/dashboard/components/OverviewCards";
import { SpendingPieChart } from "@/features/dashboard/components/SpendingPieChart";
import { useDashboardSummary } from "@/features/dashboard/hooks/useDashboard";
import { setDocumentTitle } from "@/utils/documentTitle";
import { getCurrentMonthRange } from "@/utils/dates";

export function DashboardPage() {
  useEffect(() => {
    setDocumentTitle("Dashboard");
  }, []);

  const { data: summaryResponse, isLoading, error } = useDashboardSummary();
  const { startDate, endDate } = getCurrentMonthRange();

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>

      {error && (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          Failed to load dashboard summary. Please try again.
        </div>
      )}

      <OverviewCards summary={summaryResponse?.data ?? null} isLoading={isLoading} />

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <SpendingPieChart startDate={startDate} endDate={endDate} />
        <IncomeExpenseChart months={6} />
      </div>
    </div>
  );
}
```

2. In `frontend/src/routes/index.tsx`:
   - Import `DashboardPage` from `@/features/dashboard/pages/DashboardPage`
   - Replace the index placeholder:

```typescript
{
  index: true,
  element: <DashboardPage />,
},
```

3. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- `/` renders the real dashboard for authenticated users; unauthenticated visits still redirect via `ProtectedRoute`
- Charts stack on mobile (`grid-cols-1`), sit side-by-side from `lg` up
- Summary query error surfaces as a user-facing message; charts own their own error states
- Page contains zero direct HTTP calls — only the custom hook

---

### Task 23 — Frontend: ReportsPage and Reports Route

**Status:** New

**Description:**
Create the Reports page — a month picker driving the category breakdown for the selected month, plus a 12-month income-vs-expenses trend. Reuses the dashboard components; replaces the `/reports` placeholder. After this task both "coming in Sprint 4" placeholders are gone and `PlaceholderPage` is deleted.

**Steps:**

1. Create `frontend/src/features/reports/pages/ReportsPage.tsx`:

```typescript
import { useEffect, useState } from "react";
import { endOfMonth, format, parseISO, startOfMonth } from "date-fns";
import { IncomeExpenseChart } from "@/features/dashboard/components/IncomeExpenseChart";
import { SpendingPieChart } from "@/features/dashboard/components/SpendingPieChart";
import { setDocumentTitle } from "@/utils/documentTitle";
import type { DateRange } from "@/utils/dates";

function getMonthRange(monthValue: string): DateRange {
  const monthStart = startOfMonth(parseISO(`${monthValue}-01`));
  return {
    startDate: format(monthStart, "yyyy-MM-dd"),
    endDate: format(endOfMonth(monthStart), "yyyy-MM-dd"),
  };
}

export function ReportsPage() {
  useEffect(() => {
    setDocumentTitle("Reports");
  }, []);

  const [month, setMonth] = useState(() => format(new Date(), "yyyy-MM"));
  const { startDate, endDate } = getMonthRange(month);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Reports</h1>
        <div>
          <label
            htmlFor="report-month"
            className="mb-1 block text-sm font-medium text-gray-700"
          >
            Month
          </label>
          <input
            id="report-month"
            type="month"
            value={month}
            min="2000-01"
            onChange={(e) => {
              if (e.target.value) setMonth(e.target.value);
            }}
            className="rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
          />
        </div>
      </div>

      <SpendingPieChart startDate={startDate} endDate={endDate} />

      <IncomeExpenseChart months={12} />
    </div>
  );
}
```

2. In `frontend/src/routes/index.tsx`:
   - Import `ReportsPage` from `@/features/reports/pages/ReportsPage`
   - Replace the `/reports` placeholder with `{ path: "reports", element: <ReportsPage /> }`
   - Remove the now-unused `PlaceholderPage` import and delete `frontend/src/pages/PlaceholderPage.tsx` (`noUnusedLocals` fails the build otherwise)

3. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- `/reports` renders the real page; no `PlaceholderPage` remains anywhere in the app
- Changing the month re-queries the breakdown (new query key via `dashboardKeys.categoryBreakdown(start, end)`)
- Empty month input is ignored (never parsed) — the guard in `onChange` prevents it
- Month input has a visible label and a sensible `min` bound

---

### Task 24 — Final Verification, E2E Smoke, and Sprint Closure

**Status:** New

**Description:**
Full-stack verification and closure. Run everything the sprint's success criteria depend on, then update the tracking docs. Be picky about the UI — pixel-verify mobile and desktop layouts per `docs/ai/ui-design-rules.md`.

**Steps:**

1. **Builds and tests:**

```bash
dotnet build backend/Personal.FinanceTracker.slnx          # 0 errors, 0 new warnings
dotnet test backend/Personal.FinanceTracker.slnx           # all green (189 + Sprint 4 tests)
dotnet format backend/Personal.FinanceTracker.slnx --verify-no-changes --severity warn
cd frontend && npm run build-lint                           # ESLint + tsc -b + Vite build
```

2. **Database verification** (psql / pgAdmin against the local Docker PostgreSQL):
   - `reports` schema exists with `monthly_summaries` (columns, `numeric(18,2)`, `timestamptz`, both indexes)
   - `ticker` schema exists with TickerQ's three tables
   - `ticker.cron_tickers` contains `generate-monthly-reports` (`0 0 1 * *`) and `check-budget-alerts` (`0 */6 * * *`), both enabled

3. **E2E smoke** (`task local-debug` for Vite dev + backend):
   - Register/login a test user; create categories, transactions across the current and past 2 months (including an expense-heavy month to stress the charts), and a budget near its limit
   - Dashboard `/`: cards show correct balance and current-month totals (cross-check one number by hand against the transactions you created); pie chart shows current-month category breakdown; line chart shows a continuous 6-month trend with zero-filled gaps
   - Reports `/reports`: switching the month swaps the pie breakdown; the 12-month trend renders
   - Create a new transaction from the Transactions page, navigate to the Dashboard — the summary and charts reflect it (invalidation works)
   - If Task 4's step 0 reproduced the date-filter bug: verify the Transactions date filter now works end-to-end
   - Check the UI at 320px width and at desktop — no horizontal scroll, charts stack cleanly, tap targets clear

4. **Docs closure:**
   - Update this file's header status and every task status to `Done`, and append a **Sprint Completion Record** (mirroring sprint-3.md) with verification results and any as-built deviations
   - Update Sprint 4 status in `SPRINTS-OVERVIEW.md` (header row + Sprint 4 section), refresh its "Last updated" date, and strike the TickerQ Known Gap row (resolved)
   - Update `docs/DEPENDENCIES.md`: move `recharts` and `date-fns` from "Referenced — planned" to Active; add `TickerQ` (Finance, Reporting, Api) and `TickerQ.EntityFrameworkCore` (Api) 10.4.0 as Active
   - Run the `designer-enforcer` agent before marking the sprint Done (AGENTS.md requirement)

**Success Criteria:**
- Every command in step 1 passes cleanly
- Every database object in step 2 verified present and shaped correctly
- Every E2E behavior in step 3 observed first-hand, not assumed
- All tracking docs updated; completion record written

---

## Success Criteria — Sprint Complete

- [ ] `dotnet build` — 0 errors, 0 new warnings
- [ ] `dotnet test` — all tests green (189 pre-existing + Sprint 4 additions)
- [ ] `npm run build-lint` — ESLint, `tsc -b`, Vite build all green
- [ ] `reports.monthly_summaries` exists with the unique `(user_id, year, month)` index
- [ ] `ticker` schema exists with TickerQ's tables; both cron tickers seeded on startup
- [ ] `GET /api/reports/dashboard/summary` returns enveloped balance + current-month totals for the authenticated user
- [ ] `GET /api/reports/dashboard/income-vs-expenses?months=N` returns N contiguous monthly rows (gap-filled), 400 envelope when months is out of 1–24
- [ ] `GET /api/reports/dashboard/category-breakdown?startDate&endDate` returns category expense totals for the range, 400 envelope when the range is invalid
- [ ] `MonthlyReportJob` upserts summaries for the previous month (verified via temporary cron locally, reverted before commit)
- [ ] `BudgetAlertJob` logs budgets at/above 80% usage every 6 hours
- [ ] Dashboard page at `/` — cards, pie chart (current month), line chart (6 months) — mobile-first, accessible
- [ ] Reports page at `/reports` — month picker + 12-month trend; no placeholder routes remain; `PlaceholderPage` deleted
- [ ] Transaction and category mutations invalidate dashboard queries
- [ ] Task 4 step 0 outcome recorded; if the date-filter bug was reproduced, it is fixed and verified E2E
- [ ] No `TODO Sprint 4` markers remain anywhere in the codebase
- [ ] Sprint status updated in this file and `SPRINTS-OVERVIEW.md`; `docs/DEPENDENCIES.md` reflects TickerQ + recharts + date-fns as Active; `designer-enforcer` run completed

---

## Side Notes — Future Work

- **Auth hardening (audit C-1, CRITICAL):** JWTs + user object in `localStorage` — deferred by owner decision to a dedicated task before production (tracked in `SPRINTS-OVERVIEW.md` Known Gaps). Do not spread the pattern.
- **Dashboard widgets deferred:** `BudgetProgressChart` and `RecentTransactions` (sketched in `docs/03` §7–8) are natural Sprint 5+ polish; the dashboard grid accommodates a third row.
- **Persisted-summary read endpoints:** `reports.monthly_summaries` is currently write-only (job output). If year-over-year reporting lands later, add `GET /api/reports/monthly-summaries` reading the snapshots instead of live aggregates.
- **`docs/ai/ui-design-rules.md` drift:** still references MUI Grid / `sx` props; the codebase is Tailwind-only. Cleanup tracked since Sprint 3.
- **`AuthContext` stale user on profile update:** tracked since Sprint 3 (Side Notes there) — still open.
- **TickerQ Dashboard UI** (`TickerQ.Dashboard`): optional real-time job monitoring UI; not installed this sprint. Candidate for Sprint 6 alongside OpenTelemetry wiring.

---

## References

- [SPRINTS-OVERVIEW.md](./SPRINTS-OVERVIEW.md) — sprint sequencing and status
- [sprint-3.md](./sprint-3.md) — as-built conventions this plan extends (envelope, `Result<T>`, cross-feature invalidation)
- [docs/02-Backend-Documentation.md](../02-Backend-Documentation.md) — §10 TickerQ samples are outdated; this plan supersedes them
- [docs/03-Frontend-Documentation.md](../03-Frontend-Documentation.md) — §7–8 dashboard/chart intent; samples superseded by the Recharts versions here
- [docs/01-Project-Structure.md](../01-Project-Structure.md) — §Allowed References (Reporting → Finance read-only contracts)
- TickerQ official docs — https://tickerq.net/docs (jobs), https://tickerq.net/docs/entity-framework (operational store, migrations)
- [docs/ai/ui-design-rules.md](./ui-design-rules.md) — mobile-first, accessibility, tap targets

---

*Last updated: 11/09/2026*
