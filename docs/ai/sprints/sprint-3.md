# Sprint 3 — Finance Module: Budgets

**Duration:** 1 week
**Status:** Done
**Overview:** [SPRINTS-OVERVIEW.md](./SPRINTS-OVERVIEW.md)

---

## Overview

Sprint 3 builds the Budgets feature on top of the Finance module established in Sprint 2. Users can create per-category budgets with a defined period (daily, weekly, monthly, yearly), track actual spending against their budget limit in real time, and visualise progress through a budget card with a progress bar. After this sprint, the Budgets page is fully functional end-to-end.

**This sprint depends on Sprint 2 being complete.** The Finance module project, `FinanceDbContext`, the `Category` entity, and the `finances` schema must all exist before any task in this sprint begins.

> **Convention alignment (08/09/2026):** This plan was rewritten to match the **as-built** conventions from Sprints 0–2. Earlier revisions assumed nullable service returns, bare endpoint payloads, and an Axios client — none of which match the implemented code. Every code sample below mirrors an existing file; follow them exactly.

---

## Sprint Completion Record (10/09/2026)

Sprint 3 is **complete** — delivered in `feat: add budgets end-to-end (sprint 3)` and refined by `feat: unify server error handling on finance pages`. All 17 tasks are Done and every success criterion has been verified:

- `dotnet build` — 0 errors, 0 new warnings (the 6 pre-existing test-project warnings remain — tracked in `SPRINTS-OVERVIEW.md` Known Gaps)
- `dotnet test` — 189/189 passing (130 `Finance.UnitTests` + 59 `Users.UnitTests`)
- `npm run build-lint` — ESLint, `tsc -b`, and Vite production build all green
- `finances.budgets` verified in PostgreSQL: all columns as specified (`limit_amount numeric(18,2)`, `timestamptz`, `is_active` default `true`), `idx_budgets_user_id`, and the partial unique index `idx_budgets_user_category WHERE is_active`

### As-Built Deviations from the Plan

The implementation deliberately evolved beyond this plan in four ways. The task samples below are the original plan, retained for history — where they disagree with this list, **the code wins**:

1. **Budget category is mutable on update** (the plan said immutable). `UpdateBudgetRequest` includes `CategoryId` on both backend and frontend; `Budget.Update(...)` accepts and changes the category; `BudgetService.UpdateAsync` validates category existence and the one-active-budget-per-category rule **only when the category actually changes**; the update endpoint maps `CategoryNotFound` → 400 and `DuplicateBudgetCategory` → 409.
2. **Validators gained a character whitelist** — both budget validators add `.Matches(@"^[a-zA-Z0-9áéíóúÁÉÍÓÚ\s'.,&()-*]+$")` on `Name` (accented characters allowed), mirroring the transaction/category validators.
3. **Unified server error handling** — `BudgetsPage` and `BudgetForm` use the `ApiError` / `modelErrors` / `ClientLogger` pattern (from `feat: unify server error handling on finance pages`), not the `getErrorMessage` approach shown in the Task 17 sample. Server-side field errors render inline via `BudgetForm`'s `modelErrors` prop.
4. **`BudgetForm` uses `Controller`** for the category `<select>` (instead of bare `register`) to support the `modelErrors` display.

### Tests Delivered Beyond the Plan

- `Finance.UnitTests/Domain/Entities/BudgetTests.cs` — `Create` / `Update` / `Deactivate` domain behaviour, including category change and same-category update
- `Finance.UnitTests/Application/Validators/UpdateBudgetValidatorTests.cs` — `CategoryId`, `Name`, `LimitAmount`, and `Period` structural rules

Still missing (Sprint 5 scope): `CreateBudgetValidatorTests` and `BudgetServiceTests`.

---

## Scope

### What's Included

**Backend**
- `BudgetPeriod` enum in the Domain layer (`Domain/Enums`)
- `Budget` domain entity: private constructor, static `Create(...)` factory, `Update(...)`, `Deactivate()` soft-delete, `IsActive` flag
- `IBudgetRepository` interface in **`Domain/Interfaces`** (where `ITransactionRepository` / `ICategoryRepository` live) and `BudgetRepository` EF Core implementation in `Infrastructure/Repositories`
- EF Core Fluent API configuration for `Budget` (snake_case columns, `idx_` index names, `is_active`, filtered unique index) and migration `AddBudgetsTable`
- Request/response DTOs: `CreateBudgetRequest`, `UpdateBudgetRequest`, `BudgetResponse`, `BudgetWithSpendingResponse` (sealed records in `Application/DTOs`)
- FluentValidation validators: `CreateBudgetValidator`, `UpdateBudgetValidator` (structural rules only — no DB calls)
- New error codes in `Shared/Constants/ApiErrorCode.cs`: `BudgetNotFound`, `DuplicateBudgetCategory`
- `IBudgetService` interface in **`Application/Interfaces`** and `BudgetService` implementation in `Infrastructure/Services` — all methods return `Result<T>`
- `BudgetEndpoints` — list (with spending), get, create, update, delete — every response wrapped in `ApiResponse<T>`
- Register budget services and endpoints in the existing `Finance` `DependencyInjection`

**Frontend**
- Type definitions added to `src/types/finance.ts`: `Budget`, `BudgetWithSpending`, `BudgetPeriod`, `CreateBudgetRequest`, `UpdateBudgetRequest`
- `budgetsApi` service module (`src/api/budgets.ts`) using the fetch-based `apiClient`, returning `ApiResponse<T>` envelopes
- Custom hooks: `useBudgets`, `useCreateBudget`, `useUpdateBudget`, `useDeleteBudget` with `budgetKeys` query key factory (2-minute `staleTime`)
- **Cross-feature cache invalidation:** transaction mutations also invalidate budget queries (spending data depends on transactions)
- Shared formatters extracted to `src/utils/formatters.ts` (`formatCurrency`, `formatDate`) — `TransactionList` refactored to use them (removes local duplicates)
- `BudgetForm` — Zod schema in `features/budgets/schemas.ts`, React Hook Form with the `useForm<Input, unknown, FormData>` triple-generic pattern, category selector from `useCategories`
- `BudgetCard` — name, category, period, spent vs limit, progress bar, over-budget state
- `BudgetList` — loading skeleton, error state, empty state
- `BudgetsPage` — page container following the `TransactionsPage` modal/confirm patterns, wired into the router at `/budgets`

### Out of Scope
- Budget alert background jobs — deferred to Sprint 4 (`BudgetAlertJob`)
- Email or push notifications for over-budget alerts
- Multi-currency budgets

---

## Pre-Sprint State (Verified 08/09/2026)

The cleanup items listed in earlier revisions of this document are **already resolved** — no pre-sprint cleanup task is required:

1. ~~Duplicate `UsersDbContext` DI registration in `Program.cs`~~ — resolved during Sprint 2 closure. `Program.cs` is clean; only intentional `TODO Sprint 4` / `TODO Sprint 6` placeholders remain.
2. ~~`AuthEnpoints.cs` filename typo~~ — file is correctly named `AuthEndpoints.cs`.
3. **`ITransactionRepository.GetTotalExpensesByCategoryAsync` already exists** — added during Sprint 2 (`Domain/Interfaces/ITransactionRepository.cs`, implemented in `TransactionRepository.cs`) with a doc comment referencing this sprint. No repository work is needed for spending calculation.

**Baseline health:** `npm run build` is green. `dotnet build` has 0 errors but 6 pre-existing warnings in test projects (null-handling in `UserServiceTests` / `CategoryServiceTests`, MSB3277 in `Finance.UnitTests`) and `TreatWarningsAsErrors` is commented out in `Directory.Build.props`. These are tracked separately — do not let this sprint add new warnings, and fix the pre-existing ones when touched.

---

## As-Built Conventions — MUST Follow

| Convention | Evidence (reference file) |
|------------|---------------------------|
| Repository interfaces → `Domain/Interfaces` | `ITransactionRepository.cs`, `ICategoryRepository.cs`, Users' `IUserRepository.cs` |
| Service interfaces → `Application/Interfaces` | `ITransactionService.cs`, `ICategoryService.cs` |
| Service implementations → `Infrastructure/Services` | `TransactionService.cs`, `CategoryService.cs` |
| Services return `Result<T>` with `ErrorResult(Code, Description)` | `Shared/Models/Result.cs`, `CategoryService.cs` |
| Error codes are SCREAMING_SNAKE constants in `ApiErrorCode` | `Shared/Constants/ApiErrorCode.cs` |
| Endpoints wrap every response in `ApiResponse<T>` (`IsOk`, `Data`, `Error`, `StatusCode`, `CodeText`) | `CategoryEndpoints.cs`, `TransactionEndpoints.cs` |
| Soft-delete: `IsActive` + `Deactivate()`, queries filter `IsActive` | `Category.cs`, `CategoryRepository.cs` |
| EF config: snake_case columns, `idx_` index prefix, `timestamptz`, partial unique index with `HasFilter("is_active")` | `CategoryConfiguration.cs` |
| Enums serialize as JSON strings | `JsonStringEnumConverter` in `Program.cs` |
| Frontend HTTP: fetch-based `apiClient` (`BASE_URL` already includes `/api`), functions return the full `ApiResponse<T>` envelope | `src/api/client.ts`, `src/api/transactions.ts` |
| Zod schemas in feature-level `schemas.ts` + `FormData`/`FormInput` types | `src/features/transactions/schemas.ts` |
| Query key factories: `{ all, lists, details, detail }` (no `list(filters)` for unfiltered lists) | `src/features/categories/hooks/useCategories.ts` |
| Pages own modal + delete-confirm state, unified `ApiError` / `modelErrors` / `ClientLogger` error handling, `setDocumentTitle` | `src/features/budgets/pages/BudgetsPage.tsx`, `src/features/transactions/pages/TransactionsPage.tsx` |
| Currency/date formatting: `Intl` with `es-MX` / MXN | `src/utils/formatters.ts` (extracted in this sprint) |
| Mobile-first, 44px tap targets, `aria-label` on icon buttons | `docs/ai/ui-design-rules.md` |

---

## Side Notes — Future Work

The following items are tracked here for visibility and **should be planned into a future sprint** (Sprint 5 testing or a dedicated polish sprint):

- **Update localStorage user data on profile update:** When a user updates their profile (name, email), the `AuthContext` user object and any value persisted in `localStorage` must be refreshed to reflect the change. This requires an `updateUser` action in `AuthContext` called from the profile update mutation's `onSuccess` handler. Without this, the `Header` will continue showing the stale name after an update.
- **`docs/ai/ui-design-rules.md` drift:** The design rules still reference MUI Grid / `sx` props — the codebase uses Tailwind CSS. The general principles (mobile-first, breakpoints, accessibility) remain valid; the MUI references should be cleaned up.

---

## Tasks

---

### Task 1 — BudgetPeriod Enum

**Status:** Done

**Description:**
Add the `BudgetPeriod` enum to the Finance module's Domain layer, mirroring `TransactionType`. This enum defines the time window used to calculate whether spending is within the budget limit.

**Steps:**

1. Create `backend/src/Modules/Finance/Domain/Enums/BudgetPeriod.cs`:
   ```csharp
   namespace Personal.FinanceTracker.Finance.Domain.Enums;

   public enum BudgetPeriod
   {
       Daily = 0,
       Weekly = 1,
       Monthly = 2,
       Yearly = 3
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- Enum is in the Domain layer with no external dependencies
- Style matches `TransactionType` exactly (explicit int values, file-scoped namespace)

---

### Task 2 — Budget Domain Entity

**Status:** Done

**Description:**
Create the `Budget` entity in the Finance module's Domain layer, mirroring `Category`. It extends `Entity` from `Personal.FinanceTracker.Shared.Abstractions`, uses a private constructor with a static `Create(...)` factory, validates inputs (throwing `ArgumentException` for invalid domain state, including the name-length guard `Category` has), and supports soft-delete via `IsActive` + `Deactivate()`.

**Steps:**

1. Create `backend/src/Modules/Finance/Domain/Entities/Budget.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Domain.Enums;
   using Personal.FinanceTracker.Shared.Abstractions;

   namespace Personal.FinanceTracker.Finance.Domain.Entities;

   public sealed class Budget : Entity
   {
       public Guid UserId { get; private set; }
       public Guid CategoryId { get; private set; }
       public string Name { get; private set; } = string.Empty;
       public decimal LimitAmount { get; private set; }
       public BudgetPeriod Period { get; private set; }
       public bool IsActive { get; private set; } = true;

       private Budget() { }

       public static Budget Create(
           Guid userId,
           Guid categoryId,
           string name,
           decimal limitAmount,
           BudgetPeriod period)
       {
           if (userId == Guid.Empty)
               throw new ArgumentException("User ID is required.", nameof(userId));

           if (categoryId == Guid.Empty)
               throw new ArgumentException("Category ID is required.", nameof(categoryId));

           if (string.IsNullOrWhiteSpace(name))
               throw new ArgumentException("Budget name is required.", nameof(name));

           if (name.Length > 150)
               throw new ArgumentException("Budget name cannot exceed 150 characters.", nameof(name));

           if (limitAmount <= 0)
               throw new ArgumentException("Limit amount must be greater than zero.", nameof(limitAmount));

           return new Budget
           {
               Id = Guid.NewGuid(),
               UserId = userId,
               CategoryId = categoryId,
               Name = name.Trim(),
               LimitAmount = limitAmount,
               Period = period,
               CreatedAt = DateTime.UtcNow
           };
       }

       public void Update(string name, decimal limitAmount, BudgetPeriod period)
       {
           if (string.IsNullOrWhiteSpace(name))
               throw new ArgumentException("Budget name is required.", nameof(name));

           if (name.Length > 150)
               throw new ArgumentException("Budget name cannot exceed 150 characters.", nameof(name));

           if (limitAmount <= 0)
               throw new ArgumentException("Limit amount must be greater than zero.", nameof(limitAmount));

           Name = name.Trim();
           LimitAmount = limitAmount;
           Period = period;
           UpdatedAt = DateTime.UtcNow;
       }

       public void Deactivate()
       {
           IsActive = false;
           UpdatedAt = DateTime.UtcNow;
       }
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- Entity extends `Personal.FinanceTracker.Shared.Abstractions.Entity`
- All properties have `private set`
- `Create(...)` and `Update(...)` throw `ArgumentException` for invalid state, including the 150-character name guard (defense-in-depth, matching `Category`)
- Soft-delete via `IsActive` + `Deactivate()` — consistent with `Category` and `Transaction`
- `Update(...)` does not replace the entity — only mutates allowed fields (category is immutable after creation)

---

### Task 3 — IBudgetRepository Interface

**Status:** Done

**Description:**
Define the `IBudgetRepository` interface in the Domain layer — **`Domain/Interfaces/`, not `Application/Interfaces/`**, matching where `ITransactionRepository` and `ICategoryRepository` live. The interface is pure — no EF Core or infrastructure references. All methods accept a `CancellationToken`. All read methods filter `IsActive`.

**Steps:**

1. Create `backend/src/Modules/Finance/Domain/Interfaces/IBudgetRepository.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Domain.Entities;

   namespace Personal.FinanceTracker.Finance.Domain.Interfaces;

   public interface IBudgetRepository
   {
       Task<IReadOnlyList<Budget>> GetAllByUserAsync(Guid userId, CancellationToken ct = default);
       Task<Budget?> GetByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default);
       Task<bool> ExistsByUserAndCategoryAsync(Guid userId, Guid categoryId, CancellationToken ct = default);
       Task AddAsync(Budget budget, CancellationToken ct = default);
       Task DeleteAsync(Budget budget, CancellationToken ct = default);
       Task SaveChangesAsync(CancellationToken ct = default);
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- Interface lives in `Domain/Interfaces` with namespace `Personal.FinanceTracker.Finance.Domain.Interfaces`
- `GetByUserAndIdAsync` scopes the lookup to the authenticated user — prevents cross-user access
- `ExistsByUserAndCategoryAsync` supports the "one active budget per category per user" validation rule — implementations must filter `IsActive` so a soft-deleted budget does not block re-creation
- Single-entity lookups return `Budget?` (nullable) — never throw for not-found

---

### Task 4 — Budget EF Core Configuration and Migration

**Status:** Done

**Description:**
Add an `IEntityTypeConfiguration<Budget>` Fluent API configuration mirroring `CategoryConfiguration`: snake_case column names, `timestamptz` for date columns, `HasPrecision(18, 2)` for the decimal limit, `is_active` with default `true`, `idx_` index names, and a **partial unique index** on `(user_id, category_id)` filtered by `is_active` to enforce the one-active-budget-per-category rule at the database level. Then generate and apply the migration.

**Steps:**

1. Create `backend/src/Modules/Finance/Infrastructure/Data/Configurations/BudgetConfiguration.cs`:
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using Microsoft.EntityFrameworkCore.Metadata.Builders;
   using Personal.FinanceTracker.Finance.Domain.Entities;
   using Personal.FinanceTracker.Finance.Domain.Enums;

   namespace Personal.FinanceTracker.Finance.Infrastructure.Data.Configurations;

   public sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
   {
       public void Configure(EntityTypeBuilder<Budget> builder)
       {
           builder.ToTable("budgets");

           builder.HasKey(b => b.Id);

           builder.Property(b => b.Id)
               .HasColumnName("id")
               .ValueGeneratedNever();

           builder.Property(b => b.UserId)
               .HasColumnName("user_id")
               .IsRequired();

           builder.Property(b => b.CategoryId)
               .HasColumnName("category_id")
               .IsRequired();

           builder.Property(b => b.Name)
               .HasColumnName("name")
               .HasMaxLength(150)
               .IsRequired();

           builder.Property(b => b.LimitAmount)
               .HasColumnName("limit_amount")
               .HasPrecision(18, 2)
               .IsRequired();

           builder.Property(b => b.Period)
               .HasColumnName("period")
               .HasConversion<int>()
               .IsRequired();

           builder.Property(b => b.CreatedAt)
               .HasColumnName("created_at")
               .HasColumnType("timestamptz")
               .HasDefaultValueSql("now()")
               .IsRequired();

           builder.Property(b => b.UpdatedAt)
               .HasColumnName("updated_at")
               .HasColumnType("timestamptz");

           builder.Property(b => b.IsActive)
               .HasColumnName("is_active")
               .HasDefaultValue(true)
               .IsRequired();

           builder.HasIndex(b => b.UserId)
               .HasDatabaseName("idx_budgets_user_id");

           builder.HasIndex(b => new { b.UserId, b.CategoryId })
               .IsUnique()
               .HasDatabaseName("idx_budgets_user_category")
               .HasFilter("is_active");
       }
   }
   ```

2. Register the `DbSet<Budget>` in `FinanceDbContext` (alongside the existing sets):
   ```csharp
   public DbSet<Budget> Budgets => Set<Budget>();
   ```
   EF Core will pick up the configuration automatically via `ApplyConfigurationsFromAssembly`.

3. From the `backend/` directory, generate the migration:
   ```bash
   dotnet ef migrations add AddBudgetsTable \
     --project src/Modules/Finance/Personal.FinanceTracker.Finance.csproj \
     --startup-project src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj \
     --context FinanceDbContext \
     --output-dir Infrastructure/Data/Migrations
   ```

4. Review the generated migration file:
   - `budgets` table exists in the `finances` schema
   - `limit_amount` has precision `(18, 2)`
   - `period` stored as `integer`
   - `created_at` and `updated_at` are `timestamptz`
   - `is_active` column with default `true`
   - `idx_budgets_user_id` index present
   - `idx_budgets_user_category` unique index with `WHERE is_active` filter present

5. Apply the migration:
   ```bash
   dotnet ef database update \
     --project src/Modules/Finance/Personal.FinanceTracker.Finance.csproj \
     --startup-project src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj \
     --context FinanceDbContext
   ```

**Success Criteria:**
- Migration file generates without errors
- `dotnet ef database update` succeeds
- `finances.budgets` table exists with all expected columns and both indexes (including the partial unique index)

---

### Task 5 — BudgetRepository Implementation

**Status:** Done

**Description:**
Implement `IBudgetRepository` in the Infrastructure layer using `FinanceDbContext`, mirroring `CategoryRepository`. Pass `CancellationToken` through to all EF Core async calls. `DeleteAsync` performs a soft-delete (`Deactivate()`), never a hard `Remove()`.

**Steps:**

1. Create `backend/src/Modules/Finance/Infrastructure/Repositories/BudgetRepository.cs`:
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using Personal.FinanceTracker.Finance.Domain.Entities;
   using Personal.FinanceTracker.Finance.Domain.Interfaces;
   using Personal.FinanceTracker.Finance.Infrastructure.Data;

   namespace Personal.FinanceTracker.Finance.Infrastructure.Repositories;

   public sealed class BudgetRepository(FinanceDbContext context) : IBudgetRepository
   {
       public async Task<IReadOnlyList<Budget>> GetAllByUserAsync(Guid userId, CancellationToken ct = default)
           => await context.Budgets
               .Where(b => b.UserId == userId && b.IsActive)
               .OrderBy(b => b.Name)
               .ToListAsync(ct);

       public async Task<Budget?> GetByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default)
           => await context.Budgets
               .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId && b.IsActive, ct);

       public async Task<bool> ExistsByUserAndCategoryAsync(Guid userId, Guid categoryId, CancellationToken ct = default)
           => await context.Budgets
               .AnyAsync(b => b.UserId == userId && b.CategoryId == categoryId && b.IsActive, ct);

       public async Task AddAsync(Budget budget, CancellationToken ct = default)
           => await context.Budgets.AddAsync(budget, ct);

       public Task DeleteAsync(Budget budget, CancellationToken ct = default)
       {
           budget.Deactivate();
           return Task.CompletedTask;
       }

       public async Task SaveChangesAsync(CancellationToken ct = default)
           => await context.SaveChangesAsync(ct);
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- `BudgetRepository` fully implements `IBudgetRepository`
- Every read method filters `IsActive` — soft-deleted budgets never appear in lists, lookups, or duplicate checks
- `GetByUserAndIdAsync` always filters by `UserId` — no cross-user data leakage
- No business logic in the repository — only data access

---

### Task 6 — Budget DTOs

**Status:** Done

**Description:**
Create request and response DTOs as sealed `record` types in the Application layer, mirroring `TransactionResponse` / `CategoryResponse`. The `BudgetWithSpendingResponse` is the primary response type used on the list and detail endpoints — it includes the computed spent amount and percentage.

**Steps:**

1. Create `backend/src/Modules/Finance/Application/DTOs/Requests/CreateBudgetRequest.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Domain.Enums;

   namespace Personal.FinanceTracker.Finance.Application.DTOs.Requests;

   public sealed record CreateBudgetRequest(
       Guid CategoryId,
       string Name,
       decimal LimitAmount,
       BudgetPeriod Period);
   ```

2. Create `backend/src/Modules/Finance/Application/DTOs/Requests/UpdateBudgetRequest.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Domain.Enums;

   namespace Personal.FinanceTracker.Finance.Application.DTOs.Requests;

   public sealed record UpdateBudgetRequest(
       string Name,
       decimal LimitAmount,
       BudgetPeriod Period);
   ```
    > **Superseded (as-built):** `CategoryId` IS present in `UpdateBudgetRequest` — budgets can change category on update, with existence and duplicate validation when the category changes. See Sprint Completion Record, deviation 1.

3. Create `backend/src/Modules/Finance/Application/DTOs/Responses/BudgetResponse.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Domain.Enums;

   namespace Personal.FinanceTracker.Finance.Application.DTOs.Responses;

   public sealed record BudgetResponse(
       Guid Id,
       Guid CategoryId,
       string Name,
       decimal LimitAmount,
       BudgetPeriod Period,
       DateTime CreatedAt,
       DateTime? UpdatedAt);
   ```

4. Create `backend/src/Modules/Finance/Application/DTOs/Responses/BudgetWithSpendingResponse.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Domain.Enums;

   namespace Personal.FinanceTracker.Finance.Application.DTOs.Responses;

   public sealed record BudgetWithSpendingResponse(
       Guid Id,
       Guid CategoryId,
       string CategoryName,
       string Name,
       decimal LimitAmount,
       BudgetPeriod Period,
       decimal SpentAmount,
       decimal RemainingAmount,
       decimal PercentageUsed,
       bool IsOverBudget,
       DateTime CreatedAt,
       DateTime? UpdatedAt);
   ```

5. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- All DTOs are `sealed record` types with no mutable setters
- `BudgetWithSpendingResponse` includes all computed fields needed for the frontend progress bar
- `PercentageUsed` is a `decimal` — formatting to a percentage display is the frontend's responsibility
- Enums serialize as strings (`"Monthly"`) via the global `JsonStringEnumConverter` — matching the frontend string literal union

---

### Task 7 — FluentValidation Validators

**Status:** Done

**Description:**
Create one `AbstractValidator<T>` per mutating request type in `Application/Validators/`, mirroring `CreateCategoryValidator`. Validators contain **structural rules only** (not-empty, range, length) — the one-budget-per-category and category-existence rules require the authenticated user's ID and the database, so they are enforced in `BudgetService` via `Result<T>` failures. This keeps validators infrastructure-free and testable without a DB.

**Steps:**

1. Create `backend/src/Modules/Finance/Application/Validators/CreateBudgetValidator.cs`:
   ```csharp
   using FluentValidation;
   using Personal.FinanceTracker.Finance.Application.DTOs.Requests;

   namespace Personal.FinanceTracker.Finance.Application.Validators;

   public sealed class CreateBudgetValidator : AbstractValidator<CreateBudgetRequest>
   {
       public CreateBudgetValidator()
       {
           RuleFor(x => x.CategoryId)
               .NotEmpty().WithMessage("Category is required.");

           RuleFor(x => x.Name)
               .NotEmpty().WithMessage("Budget name is required.")
               .MaximumLength(150).WithMessage("Budget name cannot exceed 150 characters.");

           RuleFor(x => x.LimitAmount)
               .GreaterThan(0).WithMessage("Limit amount must be greater than zero.")
               .LessThanOrEqualTo(1_000_000_000).WithMessage("Limit amount is unreasonably large.");

           RuleFor(x => x.Period)
               .IsInEnum().WithMessage("Invalid budget period.");
       }
   }
   ```

2. Create `backend/src/Modules/Finance/Application/Validators/UpdateBudgetValidator.cs`:
   ```csharp
   using FluentValidation;
   using Personal.FinanceTracker.Finance.Application.DTOs.Requests;

   namespace Personal.FinanceTracker.Finance.Application.Validators;

   public sealed class UpdateBudgetValidator : AbstractValidator<UpdateBudgetRequest>
   {
       public UpdateBudgetValidator()
       {
           RuleFor(x => x.Name)
               .NotEmpty().WithMessage("Budget name is required.")
               .MaximumLength(150).WithMessage("Budget name cannot exceed 150 characters.");

           RuleFor(x => x.LimitAmount)
               .GreaterThan(0).WithMessage("Limit amount must be greater than zero.")
               .LessThanOrEqualTo(1_000_000_000).WithMessage("Limit amount is unreasonably large.");

           RuleFor(x => x.Period)
               .IsInEnum().WithMessage("Invalid budget period.");
       }
   }
   ```

3. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- Both validators compile as `sealed class` extending `AbstractValidator<T>` in `Application/Validators`
- No database calls in validators — business rules are enforced in `BudgetService`
- `IsInEnum()` prevents invalid period values at the API boundary

---

### Task 8 — ApiErrorCode Constants, IBudgetService and BudgetService

**Status:** Done

**Description:**
Add budget error codes to the shared `ApiErrorCode` constants, then create the budget service interface in `Application/Interfaces` (where `ITransactionService` / `ICategoryService` live) and its implementation in `Infrastructure/Services`. All service methods return `Result<T>` — failures carry distinct `ErrorResult` codes so endpoints can map them to precise HTTP status codes. `BudgetService` handles all business logic: ownership validation, the one-active-budget-per-category rule, and spending calculation (querying `ITransactionRepository.GetTotalExpensesByCategoryAsync`, which already exists from Sprint 2).

**Steps:**

1. In `backend/src/Personal.FinanceTracker.Shared/Constants/ApiErrorCode.cs`, add alongside the existing constants:
   ```csharp
   public const string BudgetNotFound = "BUDGET_NOT_FOUND";
   public const string DuplicateBudgetCategory = "DUPLICATE_BUDGET_CATEGORY";
   ```
   Reuse the existing `CategoryNotFound` constant for the invalid-category-on-create case.

2. Create `backend/src/Modules/Finance/Application/Interfaces/IBudgetService.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
   using Personal.FinanceTracker.Finance.Application.DTOs.Responses;
   using Personal.FinanceTracker.Shared.Models;

   namespace Personal.FinanceTracker.Finance.Application.Interfaces;

   public interface IBudgetService
   {
       Task<Result<IReadOnlyList<BudgetWithSpendingResponse>>> GetAllAsync(Guid userId, CancellationToken ct = default);
       Task<Result<BudgetWithSpendingResponse>> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default);
       Task<Result<BudgetWithSpendingResponse>> CreateAsync(Guid userId, CreateBudgetRequest request, CancellationToken ct = default);
       Task<Result<BudgetWithSpendingResponse>> UpdateAsync(Guid userId, Guid id, UpdateBudgetRequest request, CancellationToken ct = default);
       Task<Result<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default);
   }
   ```

3. Create `backend/src/Modules/Finance/Infrastructure/Services/BudgetService.cs`:
   ```csharp
   using Microsoft.Extensions.Logging;
   using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
   using Personal.FinanceTracker.Finance.Application.DTOs.Responses;
   using Personal.FinanceTracker.Finance.Application.Interfaces;
   using Personal.FinanceTracker.Finance.Domain.Entities;
   using Personal.FinanceTracker.Finance.Domain.Enums;
   using Personal.FinanceTracker.Finance.Domain.Interfaces;
   using Personal.FinanceTracker.Shared.Constants;
   using Personal.FinanceTracker.Shared.Models;

   namespace Personal.FinanceTracker.Finance.Infrastructure.Services;

   public sealed class BudgetService(
       IBudgetRepository budgetRepository,
       ICategoryRepository categoryRepository,
       ITransactionRepository transactionRepository,
       ILogger<BudgetService> logger) : IBudgetService
   {
       public async Task<Result<IReadOnlyList<BudgetWithSpendingResponse>>> GetAllAsync(Guid userId, CancellationToken ct = default)
       {
           var budgets = await budgetRepository.GetAllByUserAsync(userId, ct);

           // Single category fetch — avoids a per-budget category query
           var categoryNames = (await categoryRepository.GetAllByUserAsync(userId, ct))
               .ToDictionary(c => c.Id, c => c.Name);

           var results = new List<BudgetWithSpendingResponse>(budgets.Count);
           foreach (var budget in budgets)
           {
               var spent = await GetSpendingForPeriodAsync(userId, budget.CategoryId, budget.Period, ct);
               results.Add(MapToWithSpending(budget, categoryNames.GetValueOrDefault(budget.CategoryId, "Unknown"), spent));
           }

           return Result<IReadOnlyList<BudgetWithSpendingResponse>>.Success(results);
       }

       public async Task<Result<BudgetWithSpendingResponse>> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default)
       {
           var budget = await budgetRepository.GetByUserAndIdAsync(userId, id, ct);
           if (budget is null)
           {
               logger.LogWarning("Budget {BudgetId} not found for user {UserId}", id, userId);
               return Result<BudgetWithSpendingResponse>.Failure(new(ApiErrorCode.BudgetNotFound, "Budget not found."));
           }

           var spent = await GetSpendingForPeriodAsync(userId, budget.CategoryId, budget.Period, ct);
           var categoryName = await GetCategoryNameAsync(userId, budget.CategoryId, ct);
           return Result<BudgetWithSpendingResponse>.Success(MapToWithSpending(budget, categoryName, spent));
       }

       public async Task<Result<BudgetWithSpendingResponse>> CreateAsync(Guid userId, CreateBudgetRequest request, CancellationToken ct = default)
       {
           if (!await categoryRepository.ExistsByUserAndIdAsync(userId, request.CategoryId, ct))
           {
               logger.LogWarning("Budget creation failed: category {CategoryId} not found for user {UserId}", request.CategoryId, userId);
               return Result<BudgetWithSpendingResponse>.Failure(new(ApiErrorCode.CategoryNotFound, "Category not found."));
           }

           if (await budgetRepository.ExistsByUserAndCategoryAsync(userId, request.CategoryId, ct))
           {
               logger.LogWarning("Budget creation failed: a budget for category {CategoryId} already exists for user {UserId}", request.CategoryId, userId);
               return Result<BudgetWithSpendingResponse>.Failure(new(ApiErrorCode.DuplicateBudgetCategory, "A budget already exists for this category."));
           }

           var budget = Budget.Create(userId, request.CategoryId, request.Name, request.LimitAmount, request.Period);
           await budgetRepository.AddAsync(budget, ct);
           await budgetRepository.SaveChangesAsync(ct);

           logger.LogInformation("Budget {BudgetId} created for user {UserId}", budget.Id, userId);

           var spent = await GetSpendingForPeriodAsync(userId, budget.CategoryId, budget.Period, ct);
           var categoryName = await GetCategoryNameAsync(userId, budget.CategoryId, ct);
           return Result<BudgetWithSpendingResponse>.Success(MapToWithSpending(budget, categoryName, spent));
       }

       public async Task<Result<BudgetWithSpendingResponse>> UpdateAsync(Guid userId, Guid id, UpdateBudgetRequest request, CancellationToken ct = default)
       {
           var budget = await budgetRepository.GetByUserAndIdAsync(userId, id, ct);
           if (budget is null)
           {
               logger.LogWarning("Budget update failed: {BudgetId} not found for user {UserId}", id, userId);
               return Result<BudgetWithSpendingResponse>.Failure(new(ApiErrorCode.BudgetNotFound, "Budget not found."));
           }

           budget.Update(request.Name, request.LimitAmount, request.Period);
           await budgetRepository.SaveChangesAsync(ct);

           logger.LogInformation("Budget {BudgetId} updated by user {UserId}", budget.Id, userId);

           var spent = await GetSpendingForPeriodAsync(userId, budget.CategoryId, budget.Period, ct);
           var categoryName = await GetCategoryNameAsync(userId, budget.CategoryId, ct);
           return Result<BudgetWithSpendingResponse>.Success(MapToWithSpending(budget, categoryName, spent));
       }

       public async Task<Result<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
       {
           var budget = await budgetRepository.GetByUserAndIdAsync(userId, id, ct);
           if (budget is null)
           {
               logger.LogWarning("Budget delete failed: {BudgetId} not found for user {UserId}", id, userId);
               return Result<bool>.Failure(new(ApiErrorCode.BudgetNotFound, "Budget not found."));
           }

           await budgetRepository.DeleteAsync(budget, ct);
           await budgetRepository.SaveChangesAsync(ct);

           logger.LogInformation("Budget {BudgetId} deleted by user {UserId}", budget.Id, userId);
           return Result<bool>.Success(true);
       }

       private async Task<string> GetCategoryNameAsync(Guid userId, Guid categoryId, CancellationToken ct)
       {
           var category = await categoryRepository.GetByUserAndIdAsync(userId, categoryId, ct);
           return category?.Name ?? "Unknown";
       }

       private async Task<decimal> GetSpendingForPeriodAsync(
           Guid userId,
           Guid categoryId,
           BudgetPeriod period,
           CancellationToken ct)
       {
           var (from, to) = GetPeriodRange(period);
           return await transactionRepository.GetTotalExpensesByCategoryAsync(userId, categoryId, from, to, ct);
       }

       private static (DateTime From, DateTime To) GetPeriodRange(BudgetPeriod period)
       {
           var now = DateTime.UtcNow;
           return period switch
           {
               BudgetPeriod.Daily   => (now.Date, now.Date.AddDays(1).AddTicks(-1)),
               BudgetPeriod.Weekly  => (now.Date.AddDays(-(int)now.DayOfWeek), now.Date.AddDays(7 - (int)now.DayOfWeek).AddTicks(-1)),
               BudgetPeriod.Monthly => (new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddTicks(-1)),
               BudgetPeriod.Yearly  => (new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(now.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(-1)),
               _                    => throw new ArgumentOutOfRangeException(nameof(period))
           };
       }
       // Note: weekly ranges are Sunday-based (DayOfWeek Sunday = 0).

       private static BudgetWithSpendingResponse MapToWithSpending(Budget budget, string categoryName, decimal spent)
       {
           var remaining = budget.LimitAmount - spent;
           var percentage = budget.LimitAmount > 0
               ? Math.Round(spent / budget.LimitAmount * 100, 2)
               : 0m;

           return new BudgetWithSpendingResponse(
               Id: budget.Id,
               CategoryId: budget.CategoryId,
               CategoryName: categoryName,
               Name: budget.Name,
               LimitAmount: budget.LimitAmount,
               Period: budget.Period,
               SpentAmount: spent,
               RemainingAmount: remaining,
               PercentageUsed: percentage,
               IsOverBudget: spent > budget.LimitAmount,
               CreatedAt: budget.CreatedAt,
               UpdatedAt: budget.UpdatedAt);
       }
   }
   ```

4. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- `IBudgetService` is in `Application/Interfaces` with no Infrastructure references
- `BudgetService` is in `Infrastructure/Services` and all business rules (duplicate check, ownership, category existence) are enforced here via `Result<T>` failures with distinct `ApiErrorCode` values
- `GetPeriodRange` always uses `DateTimeKind.Utc` — no local time leakage
- `PercentageUsed` is division-safe: division only occurs when `LimitAmount > 0`
- `GetAllAsync` fetches categories in a single query instead of one per budget

---

### Task 9 — BudgetEndpoints Minimal API

**Status:** Done

**Description:**
Create the `BudgetEndpoints` static class in the Api layer, mirroring `CategoryEndpoints` / `TransactionEndpoints`. All endpoints are scoped to the authenticated user via `ClaimsPrincipalExtensions.GetUserId()`. The group requires authorization. `ValidationFilter<T>` is applied to create and update endpoints. **Every response is wrapped in `ApiResponse<T>`** — the frontend client (`parseResponseAsync`) parses errors from the envelope, so bare payloads or raw `Conflict<string>` bodies break the client contract. The create endpoint maps failure codes to precise status codes: `CategoryNotFound` → 400, `DuplicateBudgetCategory` → 409.

**Steps:**

1. Create `backend/src/Modules/Finance/Api/Endpoints/BudgetEndpoints.cs`:
   ```csharp
   using System.Security.Claims;
   using Microsoft.AspNetCore.Builder;
   using Microsoft.AspNetCore.Http;
   using Microsoft.AspNetCore.Http.HttpResults;
   using Microsoft.AspNetCore.Routing;
   using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
   using Personal.FinanceTracker.Finance.Application.DTOs.Responses;
   using Personal.FinanceTracker.Finance.Application.Interfaces;
   using Personal.FinanceTracker.Shared.Constants;
   using Personal.FinanceTracker.Shared.Extensions;
   using Personal.FinanceTracker.Shared.Filters;
   using Personal.FinanceTracker.Shared.Models;

   namespace Personal.FinanceTracker.Finance.Api.Endpoints;

   public static class BudgetEndpoints
   {
       public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder app)
       {
           var group = app.MapGroup("/api/budgets")
               .WithTags("Budgets")
               .RequireAuthorization();

           group.MapGet("/", GetAllAsync)
               .WithName("GetBudgets")
               .WithDescription("Get all budgets for the authenticated user, including current period spending.");

           group.MapGet("/{id:guid}", GetByIdAsync)
               .WithName("GetBudgetById")
               .WithDescription("Get a single budget with current period spending.");

           group.MapPost("/", CreateAsync)
               .WithName("CreateBudget")
               .WithDescription("Create a new budget for a category.")
               .AddEndpointFilter<ValidationFilter<CreateBudgetRequest>>();

           group.MapPut("/{id:guid}", UpdateAsync)
               .WithName("UpdateBudget")
               .WithDescription("Update an existing budget's name, limit, or period.")
               .AddEndpointFilter<ValidationFilter<UpdateBudgetRequest>>();

           group.MapDelete("/{id:guid}", DeleteAsync)
               .WithName("DeleteBudget")
               .WithDescription("Soft-delete a budget. It is excluded from lists and duplicate checks.");

           return app;
       }

       private static async Task<Ok<ApiResponse<IReadOnlyList<BudgetWithSpendingResponse>>>> GetAllAsync(
           ClaimsPrincipal user,
           IBudgetService budgetService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var result = await budgetService.GetAllAsync(userId, ct);

           return TypedResults.Ok(new ApiResponse<IReadOnlyList<BudgetWithSpendingResponse>>
           {
               IsOk = true,
               Data = result.Value,
               StatusCode = StatusCodes.Status200OK,
               CodeText = "OK"
           });
       }

       private static async Task<Results<Ok<ApiResponse<BudgetWithSpendingResponse>>, NotFound<ApiResponse<BudgetWithSpendingResponse>>>> GetByIdAsync(
           Guid id,
           ClaimsPrincipal user,
           IBudgetService budgetService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var result = await budgetService.GetByIdAsync(userId, id, ct);

           if (result.IsFailure)
               return TypedResults.NotFound(new ApiResponse<BudgetWithSpendingResponse>
               {
                   IsOk = false,
                   Error = new ApiError
                   {
                       Title = "Budget Not Found",
                       Status = StatusCodes.Status404NotFound,
                       Detail = result.Error?.Description,
                   },
                   StatusCode = StatusCodes.Status404NotFound,
                   CodeText = "NOT_FOUND"
               });

           return TypedResults.Ok(new ApiResponse<BudgetWithSpendingResponse>
           {
               IsOk = true,
               Data = result.Value,
               StatusCode = StatusCodes.Status200OK,
               CodeText = "OK"
           });
       }

       private static async Task<Results<Created<ApiResponse<BudgetWithSpendingResponse>>, BadRequest<ApiResponse<BudgetWithSpendingResponse>>, Conflict<ApiResponse<BudgetWithSpendingResponse>>>> CreateAsync(
           CreateBudgetRequest request,
           ClaimsPrincipal user,
           IBudgetService budgetService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var result = await budgetService.CreateAsync(userId, request, ct);

           if (result.IsFailure)
           {
               if (result.Error?.Code == ApiErrorCode.CategoryNotFound)
                   return TypedResults.BadRequest(new ApiResponse<BudgetWithSpendingResponse>
                   {
                       IsOk = false,
                       Error = new ApiError
                       {
                           Title = "Budget Creation Failed",
                           Status = StatusCodes.Status400BadRequest,
                           Detail = result.Error?.Description,
                       },
                       StatusCode = StatusCodes.Status400BadRequest,
                       CodeText = "BAD_REQUEST"
                   });

               return TypedResults.Conflict(new ApiResponse<BudgetWithSpendingResponse>
               {
                   IsOk = false,
                   Error = new ApiError
                   {
                       Title = "Budget Creation Failed",
                       Status = StatusCodes.Status409Conflict,
                       Detail = result.Error?.Description,
                   },
                   StatusCode = StatusCodes.Status409Conflict,
                   CodeText = "CONFLICT"
               });
           }

           return TypedResults.Created($"/api/budgets/{result.Value!.Id}", new ApiResponse<BudgetWithSpendingResponse>
           {
               IsOk = true,
               Data = result.Value,
               StatusCode = StatusCodes.Status201Created,
               CodeText = "CREATED"
           });
       }

       private static async Task<Results<Ok<ApiResponse<BudgetWithSpendingResponse>>, NotFound<ApiResponse<BudgetWithSpendingResponse>>>> UpdateAsync(
           Guid id,
           UpdateBudgetRequest request,
           ClaimsPrincipal user,
           IBudgetService budgetService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var result = await budgetService.UpdateAsync(userId, id, request, ct);

           if (result.IsFailure)
               return TypedResults.NotFound(new ApiResponse<BudgetWithSpendingResponse>
               {
                   IsOk = false,
                   Error = new ApiError
                   {
                       Title = "Budget Update Failed",
                       Status = StatusCodes.Status404NotFound,
                       Detail = result.Error?.Description,
                   },
                   StatusCode = StatusCodes.Status404NotFound,
                   CodeText = "NOT_FOUND"
               });

           return TypedResults.Ok(new ApiResponse<BudgetWithSpendingResponse>
           {
               IsOk = true,
               Data = result.Value,
               StatusCode = StatusCodes.Status200OK,
               CodeText = "OK"
           });
       }

       private static async Task<Results<NoContent, NotFound<ApiResponse<object>>>> DeleteAsync(
           Guid id,
           ClaimsPrincipal user,
           IBudgetService budgetService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var result = await budgetService.DeleteAsync(userId, id, ct);

           if (result.IsFailure)
               return TypedResults.NotFound(new ApiResponse<object>
               {
                   IsOk = false,
                   Error = new ApiError
                   {
                       Title = "Budget Not Found",
                       Status = StatusCodes.Status404NotFound,
                       Detail = result.Error?.Description,
                   },
                   StatusCode = StatusCodes.Status404NotFound,
                   CodeText = "NOT_FOUND"
               });

           return TypedResults.NoContent();
       }
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- All endpoints use `TypedResults` (not `Results`) and wrap payloads in `ApiResponse<T>`
- `RequireAuthorization()` is applied at group level
- `ValidationFilter<T>` is applied to create and update endpoints
- No business logic in endpoint handlers — all delegated to `IBudgetService`, with status codes mapped from `Error.Code`
- `GetUserId()` called on every handler — no endpoint is user-agnostic

---

### Task 10 — Register Budgets in FinanceModule

**Status:** Done

**Description:**
Register `IBudgetRepository` and `IBudgetService` in the Finance module's `DependencyInjection` class alongside the existing transaction and category registrations. Validators are picked up automatically by the existing `AddValidatorsFromAssemblyContaining<CreateCategoryValidator>()` call (same assembly) — no extra registration is needed.

**Steps:**

1. In `backend/src/Modules/Finance/DependencyInjection.cs`, add alongside the existing registrations (with the required `using` statements for the new types):
   ```csharp
   // Repositories
   services.AddScoped<IBudgetRepository, BudgetRepository>();

   // Services
   services.AddScoped<IBudgetService, BudgetService>();
   ```

   And in `MapFinanceEndpoints`:
   ```csharp
   app.MapBudgetEndpoints();
   ```

   Note: `IBudgetRepository` comes from `Personal.FinanceTracker.Finance.Domain.Interfaces` (already imported); `IBudgetService` from `Personal.FinanceTracker.Finance.Application.Interfaces` (already imported).

2. Run `dotnet build` — confirm 0 errors, 0 **new** warnings.

**Success Criteria:**
- `IBudgetRepository` → `BudgetRepository` registered as `Scoped`
- `IBudgetService` → `BudgetService` registered as `Scoped`
- `MapBudgetEndpoints()` called in `MapFinanceEndpoints`
- No duplicate validator registration — the existing assembly scan covers the new validators

---

### Task 11 — Frontend: Type Definitions

**Status:** Done

**Description:**
Add budget type definitions to `src/types/finance.ts` — the single file Sprint 2 established for finance-domain types (`Category`, `Transaction`, etc.). Mirror the backend DTOs exactly. Use `BudgetPeriod` as a string literal union type, consistent with `TransactionType` (the backend's `JsonStringEnumConverter` serializes enum values as strings).

**Steps:**

1. Append to `frontend/src/types/finance.ts`:
   ```typescript
   export type BudgetPeriod = 'Daily' | 'Weekly' | 'Monthly' | 'Yearly';

   export interface Budget {
     id: string;
     categoryId: string;
     categoryName: string;
     name: string;
     limitAmount: number;
     period: BudgetPeriod;
     createdAt: string;
     updatedAt: string | null;
   }

   export interface BudgetWithSpending extends Budget {
     spentAmount: number;
     remainingAmount: number;
     percentageUsed: number;
     isOverBudget: boolean;
   }

   export interface CreateBudgetRequest {
     categoryId: string;
     name: string;
     limitAmount: number;
     period: BudgetPeriod;
   }

   export interface UpdateBudgetRequest {
     name: string;
     limitAmount: number;
     period: BudgetPeriod;
   }
   ```

2. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- Types mirror backend DTOs exactly (camelCase JSON, `BudgetWithSpendingResponse` → `BudgetWithSpending`)
- `BudgetPeriod` is a string literal union — not an enum (consistent with `TransactionType`)
- `BudgetWithSpending` extends `Budget` — no duplicate fields
- Types live in `types/finance.ts` — no new type file

---

### Task 12 — Frontend: budgetsApi Service Module

**Status:** Done

**Description:**
Create the `budgetsApi` object in `src/api/budgets.ts`, mirroring `transactionsApi` / `categoriesApi`. The fetch-based `apiClient` from `src/api/client.ts` handles auth tokens and refresh — no manual header management. `BASE_URL` already includes `/api`, so paths are `/budgets`, **not** `/api/budgets`. Functions return the full `ApiResponse<T>` envelope — do not unwrap `.data` here; consumers unwrap at the component level.

**Steps:**

1. Create `frontend/src/api/budgets.ts`:
   ```typescript
   import { apiClient } from "@/api/client";
   import type { ApiResponse } from "@/types/http";
   import type {
     BudgetWithSpending,
     CreateBudgetRequest,
     UpdateBudgetRequest,
   } from "@/types/finance";

   export const budgetsApi = {
     getAll: (): Promise<ApiResponse<BudgetWithSpending[]>> =>
       apiClient.get<BudgetWithSpending[]>("/budgets"),

     getById: (id: string): Promise<ApiResponse<BudgetWithSpending>> =>
       apiClient.get<BudgetWithSpending>(`/budgets/${id}`),

     create: (data: CreateBudgetRequest): Promise<ApiResponse<BudgetWithSpending>> =>
       apiClient.post<BudgetWithSpending>("/budgets", data),

     update: (
       id: string,
       data: UpdateBudgetRequest,
     ): Promise<ApiResponse<BudgetWithSpending>> =>
       apiClient.put<BudgetWithSpending>(`/budgets/${id}`, data),

     delete: (id: string): Promise<ApiResponse<void>> =>
       apiClient.delete<void>(`/budgets/${id}`),
   };
   ```

2. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- `budgetsApi` follows the same shape as `transactionsApi` and `categoriesApi`
- All functions typed with request/response types from `src/types/` and the `ApiResponse<T>` envelope from `src/types/http`
- No `any` types, no unwrapped payloads, no `/api` path prefix

---

### Task 13 — Frontend: Custom Hooks and Cross-Feature Invalidation

**Status:** Done

**Description:**
Create the TanStack Query hooks for budgets, mirroring `useCategories` (unfiltered list → `budgetKeys` has no `list(filters)` level). Mutations invalidate the `budgetKeys.lists()` key on success. **Additionally**, add budget invalidation to the existing transaction mutation hooks: budget spending is computed from transactions, so any transaction create/update/delete makes budget data stale — relying only on `staleTime` would serve outdated progress bars.

**Steps:**

1. Create `frontend/src/features/budgets/hooks/useBudgets.ts`:
   ```typescript
   import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
   import { budgetsApi } from '@/api/budgets';
   import type { CreateBudgetRequest, UpdateBudgetRequest } from '@/types/finance';

   export const budgetKeys = {
     all: ['budgets'] as const,
     lists: () => [...budgetKeys.all, 'list'] as const,
     details: () => [...budgetKeys.all, 'detail'] as const,
     detail: (id: string) => [...budgetKeys.all, 'detail', id] as const,
   };

   export function useBudgets() {
     return useQuery({
       queryKey: budgetKeys.lists(),
       queryFn: () => budgetsApi.getAll(),
       staleTime: 1000 * 60 * 2, // 2 minutes — spending data changes with transactions
     });
   }

   export function useCreateBudget() {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: (data: CreateBudgetRequest) => budgetsApi.create(data),
       onSuccess: () => {
         void queryClient.invalidateQueries({ queryKey: budgetKeys.lists() });
       },
     });
   }

   export function useUpdateBudget() {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: ({ id, data }: { id: string; data: UpdateBudgetRequest }) =>
         budgetsApi.update(id, data),
       onSuccess: () => {
         void queryClient.invalidateQueries({ queryKey: budgetKeys.lists() });
       },
     });
   }

   export function useDeleteBudget() {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: (id: string) => budgetsApi.delete(id),
       onSuccess: () => {
         void queryClient.invalidateQueries({ queryKey: budgetKeys.lists() });
       },
     });
   }
   ```

2. In `frontend/src/features/transactions/hooks/useTransactions.ts`, add budget invalidation to **all three** mutation hooks (`useCreateTransaction`, `useUpdateTransaction`, `useDeleteTransaction`). Add the import and extend each `onSuccess`:
   ```typescript
   import { budgetKeys } from '@/features/budgets/hooks/useBudgets';

   // inside each mutation's onSuccess:
   onSuccess: () => {
     void queryClient.invalidateQueries({ queryKey: transactionKeys.lists() });
     void queryClient.invalidateQueries({ queryKey: budgetKeys.all });
   },
   ```
   > Cross-feature hook imports are an established pattern (`TransactionForm` imports `useCategories` from the categories feature). There is no circular-import risk: the budgets feature does not import from the transactions feature.

3. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- `budgetKeys` factory used consistently across all hooks, mirroring `categoryKeys` (no `list(filters)` level — budgets are an unfiltered list)
- `staleTime` is 2 minutes — shorter than transactions because spending data changes whenever a transaction is created
- Transaction mutations invalidate `budgetKeys.all` — budget progress never shows stale spending after a transaction change
- No TanStack Query calls outside of hook files

---

### Task 14 — Frontend: Shared Formatters

**Status:** Done

**Description:**
Extract `formatCurrency` and `formatDate` from `TransactionList` into a shared `src/utils/formatters.ts` (Sprint 4's dashboard also needs them — this avoids a third and fourth copy). Refactor `TransactionList` to import from the util and delete its local copies.

**Steps:**

1. Create `frontend/src/utils/formatters.ts`:
   ```typescript
   export function formatCurrency(amount: number): string {
     return new Intl.NumberFormat("es-MX", {
       style: "currency",
       currency: "MXN",
     }).format(amount);
   }

   export function formatDate(dateString: string): string {
     return new Date(`${dateString.slice(0, 10)}T00:00:00`).toLocaleDateString(
       "es-MX",
       {
         year: "numeric",
         month: "short",
         day: "numeric",
       },
     );
   }
   ```

2. In `frontend/src/features/transactions/components/TransactionList.tsx`:
   - Remove the local `formatCurrency` and `formatDate` functions
   - Add `import { formatCurrency, formatDate } from "@/utils/formatters";`

3. Run `npm run build` — confirm 0 TypeScript errors. Verify the Transactions page still renders amounts and dates identically.

**Success Criteria:**
- Single source of truth for currency/date formatting
- `TransactionList` behavior unchanged (es-MX / MXN formatting preserved)

---

### Task 15 — Frontend: BudgetForm Component

**Status:** Done

**Description:**
Create the Zod schema in a feature-level `schemas.ts` file and `BudgetForm` mirroring `TransactionForm`: React Hook Form with the `useForm<BudgetFormInput, unknown, BudgetFormData>` triple-generic pattern (required for `z.coerce`), category dropdown populated from `useCategories` (consumed via the `ApiResponse` envelope), inline errors via `errors.field.message`, and the same Tailwind classes as `TransactionForm`. The form handles both create and update modes via an optional `defaultValues` prop. The category select is required — no "Uncategorized" option (a budget must target a category).

**Steps:**

1. Create `frontend/src/features/budgets/schemas.ts`:
   ```typescript
   import { z } from 'zod';

   export const budgetSchema = z.object({
     categoryId: z.string().min(1, 'Please select a category.'),
     name: z
       .string()
       .min(1, 'Budget name is required.')
       .max(150, 'Budget name cannot exceed 150 characters.'),
     limitAmount: z.coerce.number().positive('Limit amount must be greater than zero.'),
     period: z.enum(['Daily', 'Weekly', 'Monthly', 'Yearly'] as const),
   });

   export type BudgetFormData = z.infer<typeof budgetSchema>;
   export type BudgetFormInput = z.input<typeof budgetSchema>;
   ```

2. Create `frontend/src/features/budgets/components/BudgetForm.tsx`:
   ```typescript
   import { zodResolver } from '@hookform/resolvers/zod';
   import { useForm } from 'react-hook-form';
   import { budgetSchema } from '@/features/budgets/schemas';
   import type {
     BudgetFormData,
     BudgetFormInput,
   } from '@/features/budgets/schemas';
   import type { BudgetPeriod } from '@/types/finance';
   import { useCategories } from '@/features/categories/hooks/useCategories';

   interface BudgetFormProps {
     defaultValues?: Partial<BudgetFormData>;
     onSubmit: (data: BudgetFormData) => void;
     isSubmitting: boolean;
     submitLabel?: string;
   }

   const PERIOD_OPTIONS: { value: BudgetPeriod; label: string }[] = [
     { value: 'Daily', label: 'Daily' },
     { value: 'Weekly', label: 'Weekly' },
     { value: 'Monthly', label: 'Monthly' },
     { value: 'Yearly', label: 'Yearly' },
   ];

   export function BudgetForm({
     defaultValues,
     onSubmit,
     isSubmitting,
     submitLabel = 'Save Budget',
   }: BudgetFormProps) {
     const { data: categoriesResponse } = useCategories();
     const categories = categoriesResponse?.data ?? [];

     const {
       register,
       handleSubmit,
       formState: { errors },
     } = useForm<BudgetFormInput, unknown, BudgetFormData>({
       resolver: zodResolver(budgetSchema),
       defaultValues: {
         categoryId: '',
         name: '',
         limitAmount: 0,
         period: 'Monthly',
         ...defaultValues,
       },
     });

     return (
       <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
         <div>
           <label htmlFor="categoryId" className="block text-sm font-medium text-gray-700 mb-1">
             Category
           </label>
           <select
             id="categoryId"
             {...register('categoryId')}
             className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
           >
             <option value="">Select a category…</option>
             {categories.map(c => (
               <option key={c.id} value={c.id}>{c.name}</option>
             ))}
           </select>
           {errors.categoryId && (
             <p className="mt-1 text-xs text-red-600">{errors.categoryId.message}</p>
           )}
         </div>

         <div>
           <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
             Budget Name
           </label>
           <input
             id="name"
             type="text"
             {...register('name')}
             className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
             placeholder="e.g., Monthly groceries"
           />
           {errors.name && (
             <p className="mt-1 text-xs text-red-600">{errors.name.message}</p>
           )}
         </div>

         <div className="grid grid-cols-2 gap-4">
           <div>
             <label htmlFor="limitAmount" className="block text-sm font-medium text-gray-700 mb-1">
               Limit Amount
             </label>
             <input
               id="limitAmount"
               type="number"
               step="0.01"
               {...register('limitAmount')}
               className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
               placeholder="0.00"
             />
             {errors.limitAmount && (
               <p className="mt-1 text-xs text-red-600">{errors.limitAmount.message}</p>
             )}
           </div>

           <div>
             <label htmlFor="period" className="block text-sm font-medium text-gray-700 mb-1">
               Period
             </label>
             <select
               id="period"
               {...register('period')}
               className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
             >
               {PERIOD_OPTIONS.map(opt => (
                 <option key={opt.value} value={opt.value}>{opt.label}</option>
               ))}
             </select>
             {errors.period && (
               <p className="mt-1 text-xs text-red-600">{errors.period.message}</p>
             )}
           </div>
         </div>

         <button
           type="submit"
           disabled={isSubmitting}
           className="w-full rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
         >
           {isSubmitting ? 'Saving…' : submitLabel}
         </button>
       </form>
     );
   }
   ```

3. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- Schema lives in `features/budgets/schemas.ts` with `BudgetFormData` / `BudgetFormInput` types — not inlined in the component
- `BudgetForm` handles both create and update via `defaultValues` prop
- Every field displays an inline error message via `errors.field.message`
- Category dropdown uses the `useCategories` hook with envelope unwrapping (`categoriesResponse?.data ?? []`) — not a hardcoded list
- `isSubmitting` disables the submit button to prevent double-submit
- Styling matches `TransactionForm` exactly

---

### Task 16 — Frontend: BudgetCard and BudgetList Components

**Status:** Done

**Description:**
Create `BudgetCard` and `BudgetList`, mirroring the list/card patterns established in Sprint 2. `BudgetCard` displays budget info and a progress bar showing spending vs limit. `BudgetList` renders the list with loading skeleton, error state, and empty state. No data fetching inside these components — data arrives via props from `BudgetsPage`.

> **Dynamic width exception:** The progress bar's fill width is a runtime value, so a single inline `style={{ width }}` on the fill element is the accepted exception to the "no inline styles" rule — static styling remains Tailwind-only. Tailwind cannot express arbitrary runtime percentages as static classes.

**Steps:**

1. Create `frontend/src/features/budgets/components/BudgetCard.tsx`:
   ```typescript
   import { Pencil, Trash2, AlertTriangle } from "lucide-react";
   import type { BudgetWithSpending } from "@/types/finance";
   import { formatCurrency } from "@/utils/formatters";

   interface BudgetCardProps {
     budget: BudgetWithSpending;
     onEdit: (budget: BudgetWithSpending) => void;
     onDelete: (budget: BudgetWithSpending) => void;
   }

   function getProgressColor(percentageUsed: number): string {
     if (percentageUsed >= 100) return "bg-red-600";
     if (percentageUsed >= 75) return "bg-amber-500";
     return "bg-green-600";
   }

   export function BudgetCard({ budget, onEdit, onDelete }: BudgetCardProps) {
     const progressWidth = Math.min(budget.percentageUsed, 100);

     return (
       <div className="rounded-lg border border-gray-200 bg-white px-4 py-3">
         <div className="flex items-center justify-between gap-3">
           <div className="min-w-0">
             <div className="flex flex-wrap items-center gap-2">
               <p className="truncate text-sm font-medium text-gray-900">{budget.name}</p>
               {budget.isOverBudget && (
                 <span className="inline-flex items-center gap-1 rounded-full bg-red-50 px-2 py-0.5 text-xs font-medium text-red-700">
                   <AlertTriangle className="h-3 w-3" aria-hidden="true" />
                   Over budget
                 </span>
               )}
             </div>
             <p className="text-xs text-gray-500">
               {budget.categoryName} · {budget.period}
             </p>
           </div>
           <div className="flex items-center gap-3 shrink-0">
             <div className="text-right">
               <p className="text-sm font-semibold text-gray-900">
                 {formatCurrency(budget.spentAmount)} / {formatCurrency(budget.limitAmount)}
               </p>
               <p className={`text-xs font-medium ${budget.isOverBudget ? "text-red-600" : "text-gray-500"}`}>
                 {budget.percentageUsed}% used
               </p>
             </div>
             <button
               onClick={() => onEdit(budget)}
               className="rounded-md p-2 text-gray-400 hover:text-indigo-600 hover:bg-gray-50 transition-colors"
               aria-label={`Edit ${budget.name}`}
             >
               <Pencil className="h-4 w-4" />
             </button>
             <button
               onClick={() => onDelete(budget)}
               className="rounded-md p-2 text-gray-400 hover:text-red-600 hover:bg-gray-50 transition-colors"
               aria-label={`Delete ${budget.name}`}
             >
               <Trash2 className="h-4 w-4" />
             </button>
           </div>
         </div>

         <div
           className="mt-3 h-2 w-full overflow-hidden rounded-full bg-gray-100"
           role="progressbar"
           aria-valuenow={budget.percentageUsed}
           aria-valuemin={0}
           aria-valuemax={100}
           aria-label={`${budget.name} budget usage`}
         >
           <div
             className={`h-full rounded-full transition-colors ${getProgressColor(budget.percentageUsed)}`}
             style={{ width: `${progressWidth}%` }}
           />
         </div>
       </div>
     );
   }
   ```

2. Create `frontend/src/features/budgets/components/BudgetList.tsx`:
   ```typescript
   import type { BudgetWithSpending } from "@/types/finance";
   import { BudgetCard } from "@/features/budgets/components/BudgetCard";

   interface BudgetListProps {
     budgets: BudgetWithSpending[];
     isLoading: boolean;
     error: Error | null;
     onEdit: (budget: BudgetWithSpending) => void;
     onDelete: (budget: BudgetWithSpending) => void;
   }

   export function BudgetList({ budgets, isLoading, error, onEdit, onDelete }: BudgetListProps) {
     if (isLoading) {
       return (
         <div className="space-y-2">
           {[1, 2, 3].map((i) => (
             <div key={i} className="h-24 rounded-lg bg-gray-100 animate-pulse" />
           ))}
         </div>
       );
     }

     if (error) {
       return (
         <div className="rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
           Failed to load budgets. Please try again.
         </div>
       );
     }

     if (budgets.length === 0) {
       return (
         <div className="text-center py-12 text-gray-500 text-sm">
           No budgets yet. Create your first budget to start tracking spending.
         </div>
       );
     }

     return (
       <div className="space-y-2">
         {budgets.map((budget) => (
           <BudgetCard key={budget.id} budget={budget} onEdit={onEdit} onDelete={onDelete} />
         ))}
       </div>
     );
   }
   ```

3. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- Progress bar width is capped at 100% via `Math.min` — never overflows visually
- Colour thresholds: green below 75%, amber 75–99%, red at 100%+; over-budget badge clearly visible
- Progress bar is accessible (`role="progressbar"` with `aria` values)
- Loading, error, and empty states are all handled — matching `TransactionList` patterns
- Icon buttons have `aria-label`s; tap targets meet the 44px minimum (design rules)
- No data fetching inside these components — data arrives via props

---

### Task 17 — Frontend: BudgetsPage and Router Wire-up

**Status:** Done

**Description:**
Create `BudgetsPage` mirroring `TransactionsPage`: it owns the data fetching (via `useBudgets` with envelope unwrapping), the create/edit modal state, the delete confirmation flow, `errorDetails`/`modelErrors` handling via the unified `ApiError` pattern (see Sprint Completion Record, deviation 3), and `setDocumentTitle`. **Superseded (as-built):** the update payload **includes `categoryId`** — budgets can change category on update (deviation 1). Wire the page into the router at `/budgets`, replacing the placeholder.

**Steps:**

1. Create `frontend/src/features/budgets/pages/BudgetsPage.tsx`:
   ```typescript
   import { useEffect, useState } from "react";
   import { Plus, X } from "lucide-react";
   import {
     useBudgets,
     useCreateBudget,
     useUpdateBudget,
     useDeleteBudget,
   } from "@/features/budgets/hooks/useBudgets";
   import { BudgetForm } from "@/features/budgets/components/BudgetForm";
   import { BudgetList } from "@/features/budgets/components/BudgetList";
   import type { BudgetWithSpending } from "@/types/finance";
   import type { BudgetFormData } from "@/features/budgets/schemas";
   import { setDocumentTitle } from "@/utils/documentTitle";
   import { getErrorMessage } from "@/utils/errors";

   export function BudgetsPage() {
     useEffect(() => {
       setDocumentTitle("Budgets");
     }, []);

     const { data: response, isLoading, error } = useBudgets();
     const createMutation = useCreateBudget();
     const updateMutation = useUpdateBudget();
     const deleteMutation = useDeleteBudget();

     const [isModalOpen, setIsModalOpen] = useState(false);
     const [editingBudget, setEditingBudget] = useState<BudgetWithSpending | null>(null);
     const [deleteTarget, setDeleteTarget] = useState<BudgetWithSpending | null>(null);
     const [mutationError, setMutationError] = useState<string | null>(null);

     const budgets = response?.data ?? [];

     function handleOpenCreate() {
       setEditingBudget(null);
       setIsModalOpen(true);
     }

     function handleOpenEdit(budget: BudgetWithSpending) {
       setEditingBudget(budget);
       setIsModalOpen(true);
     }

     function handleCloseModal() {
       setIsModalOpen(false);
       setEditingBudget(null);
       setMutationError(null);
     }

     function handleCloseDelete() {
       setDeleteTarget(null);
       setMutationError(null);
     }

     async function handleSubmit(data: BudgetFormData) {
       try {
         if (editingBudget) {
           await updateMutation.mutateAsync({
             id: editingBudget.id,
             data: {
               name: data.name,
               limitAmount: data.limitAmount,
               period: data.period,
             },
           });
         } else {
           await createMutation.mutateAsync({
             categoryId: data.categoryId,
             name: data.name,
             limitAmount: data.limitAmount,
             period: data.period,
           });
         }
         handleCloseModal();
       } catch (error) {
         setMutationError(getErrorMessage(error));
       }
     }

     async function handleConfirmDelete() {
       if (!deleteTarget) return;
       try {
         await deleteMutation.mutateAsync(deleteTarget.id);
         handleCloseDelete();
       } catch (error) {
         setMutationError(getErrorMessage(error));
       }
     }

     const isSubmitting = createMutation.isPending || updateMutation.isPending;

     return (
       <div className="space-y-6">
         <div className="flex items-center justify-between">
           <h1 className="text-2xl font-bold text-gray-900">Budgets</h1>
           <button
             onClick={handleOpenCreate}
             className="inline-flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 transition-colors"
           >
             <Plus className="h-4 w-4" />
             Add Budget
           </button>
         </div>

         <BudgetList
           budgets={budgets}
           isLoading={isLoading}
           error={error}
           onEdit={handleOpenEdit}
           onDelete={setDeleteTarget}
         />

         {isModalOpen && (
           <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
             <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl max-h-[90dvh] overflow-y-auto">
               <div className="mb-4 flex items-center justify-between">
                 <h2 className="text-lg font-semibold text-gray-900">
                   {editingBudget ? "Edit Budget" : "New Budget"}
                 </h2>
                 <button
                   onClick={handleCloseModal}
                   className="rounded-md p-1 text-gray-400 hover:text-gray-600"
                   aria-label="Close"
                 >
                   <X className="h-5 w-5" />
                 </button>
               </div>
               {mutationError && (
                 <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                   {mutationError}
                 </div>
               )}
               <BudgetForm
                 defaultValues={
                   editingBudget
                     ? {
                         categoryId: editingBudget.categoryId,
                         name: editingBudget.name,
                         limitAmount: editingBudget.limitAmount,
                         period: editingBudget.period,
                       }
                     : undefined
                 }
                 onSubmit={handleSubmit}
                 isSubmitting={isSubmitting}
                 submitLabel={editingBudget ? "Update Budget" : "Create Budget"}
               />
             </div>
           </div>
         )}

         {deleteTarget && (
           <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
             <div className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-xl">
               <h2 className="text-lg font-semibold text-gray-900">Delete Budget</h2>
               <p className="mt-2 text-sm text-gray-600">
                 Are you sure you want to delete "{deleteTarget.name}"?
               </p>
               {mutationError && (
                 <div className="mt-3 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                   {mutationError}
                 </div>
               )}
               <div className="mt-6 flex gap-3">
                 <button
                   onClick={handleCloseDelete}
                   className="flex-1 rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors"
                 >
                   Cancel
                 </button>
                 <button
                   onClick={handleConfirmDelete}
                   disabled={deleteMutation.isPending}
                   className="flex-1 rounded-lg bg-red-600 px-4 py-2 text-sm font-semibold text-white hover:bg-red-700 disabled:opacity-50 transition-colors"
                 >
                   {deleteMutation.isPending ? "Deleting…" : "Delete"}
                 </button>
               </div>
             </div>
           </div>
         )}
       </div>
     );
   }
   ```
   > **Edit-mode category field:** when editing, the category `<select>` renders with the current category selected but changing it has no effect on the update payload (category is immutable). If you want to prevent confusion, disable the select when `defaultValues` contains an existing budget — the modal markup above leaves it enabled for simplicity; either is acceptable as long as the update payload never sends `categoryId`.

2. Update `frontend/src/routes/index.tsx`:
   - Import `BudgetsPage` from `@/features/budgets/pages/BudgetsPage`
   - Replace the `/budgets` placeholder with `<BudgetsPage />`:
   ```typescript
   {
     path: "budgets",
     element: <BudgetsPage />,
   },
   ```

3. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- `/budgets` route renders the real `BudgetsPage` (not the placeholder)
- Create, update, and delete flows work end-to-end
- Update payload includes `categoryId` (category is mutable on update, with duplicate/existence validation — deviation 1)
- Error state from `useBudgets` is rendered as a user-facing message
- `BudgetsPage` contains no direct HTTP calls — only custom hook calls
- Modal, delete-confirm, and `mutationError` patterns match `TransactionsPage`

---

## Success Criteria — Sprint Complete

- [x] `dotnet build` passes with 0 errors and no new warnings (the 6 pre-existing test-project warnings are tracked separately)
- [x] `npm run build` passes with 0 TypeScript errors
- [x] `finances.budgets` table exists with all expected columns, `is_active`, and the partial unique index `idx_budgets_user_category`
- [x] `GET /api/budgets` returns all budgets for the authenticated user with spending data, wrapped in `ApiResponse<T>`
- [x] `POST /api/budgets` creates a budget; returns enveloped 409 if an active budget for the category exists; enveloped 400 if the category is not found
- [x] `PUT /api/budgets/{id}` updates a budget; returns enveloped 404 if not owned by the user
- [x] `DELETE /api/budgets/{id}` soft-deletes a budget; returns enveloped 404 if not owned by the user; a soft-deleted budget does not block creating a new budget for the same category
- [x] Frontend `/budgets` page is functional end-to-end (create, view, update, delete)
- [x] Progress bar displays correct spending percentage, threshold colours, and over-budget state
- [x] Creating/updating/deleting a transaction refreshes budget progress bars (cross-feature invalidation)
- [x] `formatCurrency` / `formatDate` live in `src/utils/formatters.ts` and `TransactionList` uses them with no visual change

---

*Last updated: 10/09/2026*
