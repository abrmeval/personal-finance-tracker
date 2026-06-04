# Sprint 3 — Finance Module: Budgets

**Duration:** TBD
**Status:** New
**Overview:** [SPRINTS-OVERVIEW.md](./SPRINTS-OVERVIEW.md)

---

## Overview

Sprint 3 builds the Budgets feature on top of the Finance module established in Sprint 2. Users can create per-category budgets with a defined period (daily, weekly, monthly, yearly), track actual spending against their budget limit in real time, and visualise progress through a budget card with a progress bar. After this sprint, the Budgets page is fully functional end-to-end.

**This sprint depends on Sprint 2 being complete.** The Finance module project, `FinanceDbContext`, the `Category` entity, and the `finances` schema must all exist before any task in this sprint begins.

---

## Scope

### What's Included

**Backend**
- `BudgetPeriod` enum in the Domain layer
- `Budget` domain entity with private constructor, static `Create(...)` factory, `Update(...)` method
- `IBudgetRepository` interface (Application layer) and `BudgetRepository` EF Core implementation (Infrastructure layer)
- EF Core Fluent API configuration for `Budget`; migration `AddBudgetsTable`
- `IBudgetService` interface (Application) and `BudgetService` implementation (Infrastructure)
- `BudgetService.GetSpendingForPeriodAsync` — calculates total expenses for a category within the active budget period by querying the `transactions` table cross-entity
- Request/response DTOs: `CreateBudgetRequest`, `UpdateBudgetRequest`, `BudgetResponse`, `BudgetWithSpendingResponse`
- FluentValidation validators: `CreateBudgetValidator`, `UpdateBudgetValidator`
- `BudgetEndpoints` — list (with spending), get, create, update, delete
- Register budget services and endpoints in the existing `FinanceModule` / `DependencyInjection`

**Frontend**
- Type definitions: `Budget`, `BudgetWithSpending`, `BudgetPeriod`, `CreateBudgetRequest`, `UpdateBudgetRequest`
- `budgetsApi` service module (`src/api/budgets.ts`)
- Custom hooks: `useBudgets`, `useCreateBudget`, `useUpdateBudget`, `useDeleteBudget` with `budgetKeys` query key factory
- `BudgetForm` — Zod schema, React Hook Form, category selector dropdown
- `BudgetCard` — displays budget name, category, period, spent vs limit, percentage progress bar
- `BudgetList` — renders a list of `BudgetCard` components with empty state
- `BudgetsPage` — page container with add button, `BudgetList`, modal/drawer for `BudgetForm`
- Wire `BudgetsPage` into the router at `/budgets`

### Out of Scope
- Budget alert background jobs — deferred to Sprint 4 (`BudgetAlertJob`)
- Email or push notifications for over-budget alerts
- Multi-currency budgets

### Known Gaps and Pre-Sprint Cleanup

The following items were left incomplete at the end of Sprint 1 and **must be resolved before starting Sprint 3 backend work**:

1. **Duplicate `UsersDbContext` DI registration in `Program.cs`** — The temporary `AddDbContext<UsersDbContext>` added in Sprint 1 Task 8 Step 2 was never removed. It must be cleaned up to prevent conflicts when registering `FinanceDbContext`. See Sprint 1 Task 8 note.
2. **Stale TODO comment in `Program.cs`** — `// TODO Sprint 1:` may still be present. Remove it.
3. **`AuthEnpoints.cs` filename typo** — Missing `d` in `Endpoints`. Cosmetic but worth correcting during cleanup.

### Side Notes — Future Work

The following items are tracked here for visibility and **should be planned into a future sprint** (Sprint 5 testing or a dedicated polish sprint):

- **Update localStorage user data on profile update:** When a user updates their profile (name, email), the `AuthContext` user object and any value persisted in `localStorage` must be refreshed to reflect the change. This requires a `updateUser` action in `AuthContext` called from the profile update mutation's `onSuccess` handler. Without this, the `Header` will continue showing the stale name after an update.

---

## Tasks

---

### Task 1 — Pre-Sprint Cleanup: Fix Program.cs

**Status:** New

**Description:**
Remove the duplicate `UsersDbContext` DI registration and any stale TODO comments left over from Sprint 1. The Finance module added in Sprint 2 will register its own `FinanceDbContext` — a clean `Program.cs` is required before adding it.

**Steps:**

1. Open `backend/src/Personal.FinanceTracker.Api/Program.cs`.

2. Remove the temporary `AddDbContext<UsersDbContext>` call (Sprint 1 Task 8 Step 2 leftover):
   ```csharp
   // Remove this block entirely:
   builder.Services.AddDbContext<Personal.FinanceTracker.Users.Infrastructure.Data.UsersDbContext>(options =>
       options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
   ```

3. Remove any remaining `// TODO Sprint 1:` comments.

4. Run `dotnet build` — confirm 0 errors, 0 warnings.

**Success Criteria:**
- `Program.cs` has no duplicate `UsersDbContext` registration
- No stale TODO comments
- `dotnet build` passes cleanly

---

### Task 2 — BudgetPeriod Enum

**Status:** New

**Description:**
Add the `BudgetPeriod` enum to the Finance module's Domain layer. This enum defines the time window used to calculate whether spending is within the budget limit.

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
- Four values covering all supported periods

---

### Task 3 — Budget Domain Entity

**Status:** New

**Description:**
Create the `Budget` entity in the Finance module's Domain layer. It extends `Entity` from `Personal.FinanceTracker.Shared.Abstractions`. All properties have `private set`. The static `Create(...)` factory validates inputs and throws `ArgumentException` for invalid domain state. An `Update(...)` method allows modifying the name, limit, and period without replacing the entity.

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

           if (limitAmount <= 0)
               throw new ArgumentException("Limit amount must be greater than zero.", nameof(limitAmount));

           Name = name.Trim();
           LimitAmount = limitAmount;
           Period = period;
           UpdatedAt = DateTime.UtcNow;
       }
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- Entity extends `Personal.FinanceTracker.Shared.Abstractions.Entity`
- All properties have `private set`
- `Create(...)` throws `ArgumentException` for invalid state
- `Update(...)` does not replace the entity — only mutates allowed fields

---

### Task 4 — IBudgetRepository Interface

**Status:** New

**Description:**
Define the `IBudgetRepository` interface in the Application layer. The interface is pure — no EF Core or infrastructure references. All methods accept a `CancellationToken`.

**Steps:**

1. Create `backend/src/Modules/Finance/Application/Interfaces/IBudgetRepository.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Domain.Entities;

   namespace Personal.FinanceTracker.Finance.Application.Interfaces;

   public interface IBudgetRepository
   {
       Task<IReadOnlyList<Budget>> GetAllByUserAsync(Guid userId, CancellationToken ct = default);
       Task<Budget?> GetByIdAsync(Guid id, CancellationToken ct = default);
       Task<Budget?> GetByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default);
       Task<bool> ExistsByUserAndCategoryAsync(Guid userId, Guid categoryId, CancellationToken ct = default);
       Task AddAsync(Budget budget, CancellationToken ct = default);
       Task DeleteAsync(Budget budget, CancellationToken ct = default);
       Task SaveChangesAsync(CancellationToken ct = default);
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- Interface is in the Application layer — no Infrastructure references
- `GetByUserAndIdAsync` scopes the lookup to the authenticated user — prevents cross-user access
- `ExistsByUserAndCategoryAsync` supports the "one budget per category per user" validation rule

---

### Task 5 — Budget EF Core Configuration and Migration

**Status:** New

**Description:**
Add an `IEntityTypeConfiguration<Budget>` Fluent API configuration to the Finance module's `FinanceDbContext`. Use snake_case column names, `timestamptz` for date columns, and `HasPrecision(18, 2)` for the decimal limit. Then generate and apply the migration.

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

           builder.HasIndex(b => b.UserId)
               .HasDatabaseName("ix_budgets_user_id");

           builder.HasIndex(b => new { b.UserId, b.CategoryId })
               .HasDatabaseName("ix_budgets_user_category");
       }
   }
   ```

2. Register `DbSet<Budget>` in `FinanceDbContext`:
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
   - `budgets` table exists in `finances` schema
   - `limit_amount` has precision `(18, 2)`
   - `period` stored as `integer`
   - `created_at` and `updated_at` are `timestamptz`
   - Both indexes are present

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
- `finances.budgets` table exists with all expected columns

---

### Task 6 — BudgetRepository Implementation

**Status:** New

**Description:**
Implement `IBudgetRepository` in the Infrastructure layer using `FinanceDbContext`. Pass `CancellationToken` through to all EF Core async calls.

**Steps:**

1. Create `backend/src/Modules/Finance/Infrastructure/Repositories/BudgetRepository.cs`:
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using Personal.FinanceTracker.Finance.Application.Interfaces;
   using Personal.FinanceTracker.Finance.Domain.Entities;
   using Personal.FinanceTracker.Finance.Infrastructure.Data;

   namespace Personal.FinanceTracker.Finance.Infrastructure.Repositories;

   public sealed class BudgetRepository(FinanceDbContext context) : IBudgetRepository
   {
       public async Task<IReadOnlyList<Budget>> GetAllByUserAsync(Guid userId, CancellationToken ct = default)
           => await context.Budgets
               .Where(b => b.UserId == userId)
               .OrderBy(b => b.Name)
               .ToListAsync(ct);

       public async Task<Budget?> GetByIdAsync(Guid id, CancellationToken ct = default)
           => await context.Budgets.FirstOrDefaultAsync(b => b.Id == id, ct);

       public async Task<Budget?> GetByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default)
           => await context.Budgets
               .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId, ct);

       public async Task<bool> ExistsByUserAndCategoryAsync(Guid userId, Guid categoryId, CancellationToken ct = default)
           => await context.Budgets
               .AnyAsync(b => b.UserId == userId && b.CategoryId == categoryId, ct);

       public async Task AddAsync(Budget budget, CancellationToken ct = default)
           => await context.Budgets.AddAsync(budget, ct);

       public Task DeleteAsync(Budget budget, CancellationToken ct = default)
       {
           context.Budgets.Remove(budget);
           return Task.CompletedTask;
       }

       public async Task SaveChangesAsync(CancellationToken ct = default)
           => await context.SaveChangesAsync(ct);
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- `BudgetRepository` fully implements `IBudgetRepository`
- `GetByUserAndIdAsync` always filters by `UserId` — no cross-user data leakage
- No business logic in the repository — only data access

---

### Task 7 — Budget DTOs

**Status:** New

**Description:**
Create request and response DTOs as `record` types in the Application layer. The `BudgetWithSpendingResponse` is the primary response type used on the list and detail endpoints — it includes the computed spent amount and percentage.

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

3. Create `backend/src/Modules/Finance/Application/DTOs/Responses/BudgetResponse.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Domain.Enums;

   namespace Personal.FinanceTracker.Finance.Application.DTOs.Responses;

   public sealed record BudgetResponse(
       Guid Id,
       Guid CategoryId,
       string CategoryName,
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

---

### Task 8 — FluentValidation Validators

**Status:** New

**Description:**
Create one `AbstractValidator<T>` per mutating request type. Both live in `Application/Validators/`. The `CreateBudgetValidator` includes a `MustAsync` check to enforce the one-budget-per-category-per-user rule.

> **Note:** The `MustAsync` DB check in `CreateBudgetValidator` requires access to `IBudgetRepository`. The `userId` for this check is not in the request body — it comes from claims. This validator will receive the repository via DI but the `userId` must be injected at the endpoint level before validation, or validated in the service. **Recommended approach:** Do the one-budget-per-category check in `BudgetService.CreateAsync` and return `null` to signal conflict — keep the validator to structural rules only (not-empty, range, etc.). This keeps validators infrastructure-free and testable without a DB.

**Steps:**

1. Create `backend/src/Modules/Finance/Application/Validators/CreateBudgetValidator.cs`:
   ```csharp
   using FluentValidation;
   using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
   using Personal.FinanceTracker.Finance.Domain.Enums;

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
- Both validators compile as `sealed class` extending `AbstractValidator<T>`
- No database calls in validators — the one-budget-per-category rule is enforced in `BudgetService`
- `IsInEnum()` prevents invalid period values at the API boundary

---

### Task 9 — IBudgetService and BudgetService

**Status:** New

**Description:**
Create the budget service interface in the Application layer and its implementation in the Infrastructure layer. `BudgetService` handles all business logic: ownership validation, the one-budget-per-category rule, and spending calculation. The spending calculation queries `ITransactionRepository` to sum all expense transactions for a category within the active period window.

**Steps:**

1. Create `backend/src/Modules/Finance/Application/Services/IBudgetService.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
   using Personal.FinanceTracker.Finance.Application.DTOs.Responses;

   namespace Personal.FinanceTracker.Finance.Application.Services;

   public interface IBudgetService
   {
       Task<IReadOnlyList<BudgetWithSpendingResponse>> GetAllAsync(Guid userId, CancellationToken ct = default);
       Task<BudgetWithSpendingResponse?> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default);
       Task<BudgetWithSpendingResponse?> CreateAsync(Guid userId, CreateBudgetRequest request, CancellationToken ct = default);
       Task<BudgetWithSpendingResponse?> UpdateAsync(Guid userId, Guid id, UpdateBudgetRequest request, CancellationToken ct = default);
       Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default);
   }
   ```

2. Create `backend/src/Modules/Finance/Infrastructure/Services/BudgetService.cs`:
   ```csharp
   using Microsoft.Extensions.Logging;
   using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
   using Personal.FinanceTracker.Finance.Application.DTOs.Responses;
   using Personal.FinanceTracker.Finance.Application.Interfaces;
   using Personal.FinanceTracker.Finance.Application.Services;
   using Personal.FinanceTracker.Finance.Domain.Entities;
   using Personal.FinanceTracker.Finance.Domain.Enums;

   namespace Personal.FinanceTracker.Finance.Infrastructure.Services;

   public sealed class BudgetService(
       IBudgetRepository budgetRepository,
       ICategoryRepository categoryRepository,
       ITransactionRepository transactionRepository,
       ILogger<BudgetService> logger) : IBudgetService
   {
       public async Task<IReadOnlyList<BudgetWithSpendingResponse>> GetAllAsync(Guid userId, CancellationToken ct = default)
       {
           var budgets = await budgetRepository.GetAllByUserAsync(userId, ct);
           var results = new List<BudgetWithSpendingResponse>(budgets.Count);

           foreach (var budget in budgets)
           {
               var category = await categoryRepository.GetByIdAsync(budget.CategoryId, ct);
               var spent = await GetSpendingForPeriodAsync(userId, budget.CategoryId, budget.Period, ct);
               results.Add(MapToWithSpending(budget, category?.Name ?? "Unknown", spent));
           }

           return results;
       }

       public async Task<BudgetWithSpendingResponse?> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default)
       {
           var budget = await budgetRepository.GetByUserAndIdAsync(userId, id, ct);
           if (budget is null) return null;

           var category = await categoryRepository.GetByIdAsync(budget.CategoryId, ct);
           var spent = await GetSpendingForPeriodAsync(userId, budget.CategoryId, budget.Period, ct);
           return MapToWithSpending(budget, category?.Name ?? "Unknown", spent);
       }

       public async Task<BudgetWithSpendingResponse?> CreateAsync(Guid userId, CreateBudgetRequest request, CancellationToken ct = default)
       {
           var categoryExists = await categoryRepository.ExistsByUserAndIdAsync(userId, request.CategoryId, ct);
           if (!categoryExists)
           {
               logger.LogWarning("Budget creation failed: category {CategoryId} not found for user {UserId}", request.CategoryId, userId);
               return null;
           }

           var alreadyExists = await budgetRepository.ExistsByUserAndCategoryAsync(userId, request.CategoryId, ct);
           if (alreadyExists)
           {
               logger.LogWarning("Budget creation failed: budget for category {CategoryId} already exists for user {UserId}", request.CategoryId, userId);
               return null;
           }

           var budget = Budget.Create(userId, request.CategoryId, request.Name, request.LimitAmount, request.Period);
           await budgetRepository.AddAsync(budget, ct);
           await budgetRepository.SaveChangesAsync(ct);

           logger.LogInformation("Budget {BudgetId} created for user {UserId}", budget.Id, userId);

           var category = await categoryRepository.GetByIdAsync(request.CategoryId, ct);
           var spent = await GetSpendingForPeriodAsync(userId, request.CategoryId, request.Period, ct);
           return MapToWithSpending(budget, category?.Name ?? "Unknown", spent);
       }

       public async Task<BudgetWithSpendingResponse?> UpdateAsync(Guid userId, Guid id, UpdateBudgetRequest request, CancellationToken ct = default)
       {
           var budget = await budgetRepository.GetByUserAndIdAsync(userId, id, ct);
           if (budget is null) return null;

           budget.Update(request.Name, request.LimitAmount, request.Period);
           await budgetRepository.SaveChangesAsync(ct);

           logger.LogInformation("Budget {BudgetId} updated by user {UserId}", budget.Id, userId);

           var category = await categoryRepository.GetByIdAsync(budget.CategoryId, ct);
           var spent = await GetSpendingForPeriodAsync(userId, budget.CategoryId, budget.Period, ct);
           return MapToWithSpending(budget, category?.Name ?? "Unknown", spent);
       }

       public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
       {
           var budget = await budgetRepository.GetByUserAndIdAsync(userId, id, ct);
           if (budget is null) return false;

           await budgetRepository.DeleteAsync(budget, ct);
           await budgetRepository.SaveChangesAsync(ct);

           logger.LogInformation("Budget {BudgetId} deleted by user {UserId}", budget.Id, userId);
           return true;
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

   > **Note:** `ITransactionRepository` must expose `GetTotalExpensesByCategoryAsync(Guid userId, Guid categoryId, DateTime from, DateTime to, CancellationToken ct)`. If this method does not exist after Sprint 2, add it to the interface and implementation before proceeding.

3. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- `IBudgetService` is in Application with no Infrastructure references
- `BudgetService` is in Infrastructure and all business rules (duplicate check, ownership) are enforced here
- `GetPeriodRange` always uses `DateTimeKind.Utc` — no local time leakage
- `PercentageUsed` is capped-safe: division only occurs when `LimitAmount > 0`

---

### Task 10 — BudgetEndpoints Minimal API

**Status:** New

**Description:**
Create the `BudgetEndpoints` static class in the Api layer. All endpoints are scoped to the authenticated user via `ClaimsPrincipalExtensions.GetUserId()`. The group requires authorization. Apply `ValidationFilter<T>` to create and update endpoints.

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
   using Personal.FinanceTracker.Finance.Application.Services;
   using Personal.FinanceTracker.Shared.Extensions;
   using Personal.FinanceTracker.Shared.Filters;

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
               .WithDescription("Delete a budget.");

           return app;
       }

       private static async Task<Ok<IReadOnlyList<BudgetWithSpendingResponse>>> GetAllAsync(
           ClaimsPrincipal user,
           IBudgetService budgetService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var budgets = await budgetService.GetAllAsync(userId, ct);
           return TypedResults.Ok(budgets);
       }

       private static async Task<Results<Ok<BudgetWithSpendingResponse>, NotFound>> GetByIdAsync(
           Guid id,
           ClaimsPrincipal user,
           IBudgetService budgetService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var budget = await budgetService.GetByIdAsync(userId, id, ct);
           return budget is null ? TypedResults.NotFound() : TypedResults.Ok(budget);
       }

       private static async Task<Results<Created<BudgetWithSpendingResponse>, Conflict<string>>> CreateAsync(
           CreateBudgetRequest request,
           ClaimsPrincipal user,
           IBudgetService budgetService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var budget = await budgetService.CreateAsync(userId, request, ct);

           if (budget is null)
               return TypedResults.Conflict("A budget for this category already exists, or the category was not found.");

           return TypedResults.Created($"/api/budgets/{budget.Id}", budget);
       }

       private static async Task<Results<Ok<BudgetWithSpendingResponse>, NotFound>> UpdateAsync(
           Guid id,
           UpdateBudgetRequest request,
           ClaimsPrincipal user,
           IBudgetService budgetService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var budget = await budgetService.UpdateAsync(userId, id, request, ct);
           return budget is null ? TypedResults.NotFound() : TypedResults.Ok(budget);
       }

       private static async Task<Results<NoContent, NotFound>> DeleteAsync(
           Guid id,
           ClaimsPrincipal user,
           IBudgetService budgetService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var success = await budgetService.DeleteAsync(userId, id, ct);
           return success ? TypedResults.NoContent() : TypedResults.NotFound();
       }
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- All endpoints use `TypedResults` (not `Results`)
- `RequireAuthorization()` is applied at group level
- `ValidationFilter<T>` is applied to create and update endpoints
- No business logic in endpoint handlers — all delegated to `IBudgetService`
- `GetUserId()` called on every handler — no endpoint is user-agnostic

---

### Task 11 — Register Budgets in FinanceModule

**Status:** New

**Description:**
Register `IBudgetRepository`, `IBudgetService`, and `BudgetEndpoints` in the Finance module's `DependencyInjection` class (the `AddFinanceModule` / `MapFinanceEndpoints` registration entry point established in Sprint 2).

**Steps:**

1. In `backend/src/Modules/Finance/DependencyInjection.cs`, add the budget registrations alongside existing transaction and category registrations:

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

2. Add the required `using` statements for the new types.

3. Run `dotnet build` — confirm 0 errors, 0 warnings.

**Success Criteria:**
- `IBudgetRepository` → `BudgetRepository` registered as `Scoped`
- `IBudgetService` → `BudgetService` registered as `Scoped`
- `MapBudgetEndpoints()` called in `MapFinanceEndpoints`
- `dotnet build` passes with zero warnings

---

### Task 12 — Frontend: Type Definitions

**Status:** New

**Description:**
Add budget type definitions to `src/types/`. Mirror the backend DTOs exactly. Use `BudgetPeriod` as a string literal union type (consistent with how `TransactionType` is handled in Sprint 2).

**Steps:**

1. Add to `frontend/src/types/finance.ts` (or create `frontend/src/types/budget.ts` — follow whatever pattern Sprint 2 established for transaction types):

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
- Types mirror backend DTOs exactly
- `BudgetPeriod` is a string literal union — not an enum (consistent with `verbatimModuleSyntax`)
- `BudgetWithSpending` extends `Budget` — no duplicate fields

---

### Task 13 — Frontend: budgetsApi Service Module

**Status:** New

**Description:**
Create the `budgetsApi` object in `src/api/budgets.ts`. All functions are fully typed. The Axios client from `src/api/client.ts` handles the auth token — no manual header management here.

**Steps:**

1. Create `frontend/src/api/budgets.ts`:
   ```typescript
   import type { BudgetWithSpending, CreateBudgetRequest, UpdateBudgetRequest } from '@/types/budget';
   import { apiClient } from '@/api/client';

   export const budgetsApi = {
     getAll(): Promise<BudgetWithSpending[]> {
       return apiClient.get<BudgetWithSpending[]>('/api/budgets').then(r => r.data);
     },

     getById(id: string): Promise<BudgetWithSpending> {
       return apiClient.get<BudgetWithSpending>(`/api/budgets/${id}`).then(r => r.data);
     },

     create(request: CreateBudgetRequest): Promise<BudgetWithSpending> {
       return apiClient.post<BudgetWithSpending>('/api/budgets', request).then(r => r.data);
     },

     update(id: string, request: UpdateBudgetRequest): Promise<BudgetWithSpending> {
       return apiClient.put<BudgetWithSpending>(`/api/budgets/${id}`, request).then(r => r.data);
     },

     delete(id: string): Promise<void> {
       return apiClient.delete(`/api/budgets/${id}`).then(() => undefined);
     },
   };
   ```

   > **Note:** Adjust the import path for `apiClient` to match whatever name `src/api/client.ts` exports (check Sprint 2's `transactionsApi` for the exact import pattern).

2. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- `budgetsApi` follows the same shape as `transactionsApi` and `categoriesApi`
- All functions typed with request/response types from `src/types/`
- No `any` types

---

### Task 14 — Frontend: Custom Hooks

**Status:** New

**Description:**
Create the TanStack Query hooks for budgets. All hooks follow the query key factory pattern. Mutations invalidate the `budgetKeys.lists()` key on success.

**Steps:**

1. Create `frontend/src/features/budgets/hooks/useBudgets.ts`:
   ```typescript
   import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
   import { budgetsApi } from '@/api/budgets';
   import type { CreateBudgetRequest, UpdateBudgetRequest } from '@/types/budget';

   export const budgetKeys = {
     all: ['budgets'] as const,
     lists: () => [...budgetKeys.all, 'list'] as const,
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
       mutationFn: (request: CreateBudgetRequest) => budgetsApi.create(request),
       onSuccess: () => {
         void queryClient.invalidateQueries({ queryKey: budgetKeys.lists() });
       },
     });
   }

   export function useUpdateBudget() {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: ({ id, request }: { id: string; request: UpdateBudgetRequest }) =>
         budgetsApi.update(id, request),
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

2. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- `budgetKeys` factory used consistently across all hooks
- `staleTime` is 2 minutes — shorter than transactions because spending data changes whenever a transaction is created
- No TanStack Query calls outside of these hook files

---

### Task 15 — Frontend: BudgetForm Component

**Status:** New

**Description:**
Create `BudgetForm` using React Hook Form + Zod. The form handles both create and update modes via an optional `defaultValues` prop. The category field is a `<select>` populated from `useCategories`.

**Steps:**

1. Create `frontend/src/features/budgets/components/BudgetForm.tsx`:
   ```typescript
   import { zodResolver } from '@hookform/resolvers/zod';
   import { useForm } from 'react-hook-form';
   import { z } from 'zod';
   import type { BudgetPeriod, BudgetWithSpending } from '@/types/budget';
   import { useCategories } from '@/features/categories/hooks/useCategories';

   const budgetSchema = z.object({
     categoryId: z.string().uuid('Please select a category.'),
     name: z.string().min(1, 'Budget name is required.').max(150, 'Name cannot exceed 150 characters.'),
     limitAmount: z.coerce.number().positive('Limit must be greater than zero.'),
     period: z.enum(['Daily', 'Weekly', 'Monthly', 'Yearly'] as const),
   });

   type BudgetFormData = z.infer<typeof budgetSchema>;

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

   export function BudgetForm({ defaultValues, onSubmit, isSubmitting, submitLabel = 'Save Budget' }: BudgetFormProps) {
     const { data: categories = [] } = useCategories();
     const { register, handleSubmit, formState: { errors } } = useForm<BudgetFormData>({
       resolver: zodResolver(budgetSchema),
       defaultValues,
     });

     return (
       <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
         <div>
           <label htmlFor="categoryId" className="block text-sm font-medium text-gray-700">Category</label>
           <select id="categoryId" {...register('categoryId')} className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm">
             <option value="">Select a category…</option>
             {categories.map(c => (
               <option key={c.id} value={c.id}>{c.name}</option>
             ))}
           </select>
           {errors.categoryId && <p className="mt-1 text-sm text-red-600">{errors.categoryId.message}</p>}
         </div>

         <div>
           <label htmlFor="name" className="block text-sm font-medium text-gray-700">Budget Name</label>
           <input id="name" type="text" {...register('name')} className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
           {errors.name && <p className="mt-1 text-sm text-red-600">{errors.name.message}</p>}
         </div>

         <div>
           <label htmlFor="limitAmount" className="block text-sm font-medium text-gray-700">Limit Amount</label>
           <input id="limitAmount" type="number" step="0.01" {...register('limitAmount')} className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
           {errors.limitAmount && <p className="mt-1 text-sm text-red-600">{errors.limitAmount.message}</p>}
         </div>

         <div>
           <label htmlFor="period" className="block text-sm font-medium text-gray-700">Period</label>
           <select id="period" {...register('period')} className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm">
             {PERIOD_OPTIONS.map(p => (
               <option key={p.value} value={p.value}>{p.label}</option>
             ))}
           </select>
           {errors.period && <p className="mt-1 text-sm text-red-600">{errors.period.message}</p>}
         </div>

         <button type="submit" disabled={isSubmitting} className="w-full rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
           {isSubmitting ? 'Saving…' : submitLabel}
         </button>
       </form>
     );
   }
   ```

   > **Note:** Adjust class names to match the design system in `docs/ai/ui-design-rules.md` — the Tailwind classes above are illustrative. Follow the button and input patterns already established in Sprint 2 components.

2. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- `BudgetForm` handles both create and update via `defaultValues` prop
- Every field displays an inline error message via `errors.field.message`
- Category dropdown uses `useCategories` hook — not a hardcoded list
- `isSubmitting` disables the submit button to prevent double-submit

---

### Task 16 — Frontend: BudgetCard and BudgetList Components

**Status:** New

**Description:**
Create `BudgetCard` and `BudgetList`. `BudgetCard` displays budget info and a progress bar showing spending vs limit. `BudgetList` renders the list and an empty state. Follow the card/list patterns established in Sprint 2 for transactions and categories.

**Steps:**

1. Create `frontend/src/features/budgets/components/BudgetCard.tsx`:
   - Display: budget name, category name, period badge
   - Progress bar: filled width = `Math.min(percentageUsed, 100)%`
   - Colour: green below 75%, amber 75–99%, red at 100%+
   - Labels: `$spentAmount / $limitAmount` and `percentageUsed%`
   - Over-budget indicator: show a warning label when `isOverBudget === true`
   - Edit and Delete action buttons (callbacks via props)

2. Create `frontend/src/features/budgets/components/BudgetList.tsx`:
   - Map `BudgetWithSpending[]` to `BudgetCard` components
   - Empty state: "No budgets yet. Create your first budget to start tracking spending."
   - Loading skeleton: show 3 placeholder cards while `isLoading` is true
   - Error state: display a user-facing error message when `error` is present (TanStack Query `error` state)

3. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- Progress bar width is capped at 100% via `Math.min` — never overflows visually
- Over-budget state is clearly visible
- Loading, error, and empty states are all handled
- No data fetching inside these components — data arrives via props from `BudgetsPage`

---

### Task 17 — Frontend: BudgetsPage and Router Wire-up

**Status:** New

**Description:**
Create `BudgetsPage` as the top-level page component. It owns the data fetching (via `useBudgets`), the create/edit modal state, and the delete confirmation flow. Wire the page into the router at `/budgets`.

**Steps:**

1. Create `frontend/src/features/budgets/pages/BudgetsPage.tsx`:
   - Use `useBudgets` for data fetching
   - Use `useCreateBudget`, `useUpdateBudget`, `useDeleteBudget` for mutations
   - Maintain local state for: modal open/closed, selected budget for editing
   - "Add Budget" button opens the create modal
   - Edit button on `BudgetCard` opens the update modal with `defaultValues` populated
   - Delete button triggers a confirm dialog before calling `useDeleteBudget.mutate`
   - Render `BudgetList` passing data, isLoading, error

2. Update `frontend/src/routes/index.tsx`:
   - Replace the placeholder div at `/budgets` with `<BudgetsPage />`
   - Add the import

3. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- `/budgets` route renders the real `BudgetsPage` (not the placeholder)
- Create, update, and delete flows work end-to-end
- Error state from `useBudgets` is rendered as a user-facing message
- `BudgetsPage` does not contain any Axios or fetch calls directly — only custom hook calls

---

## Success Criteria — Sprint Complete

- [ ] `dotnet build` passes with 0 errors and 0 warnings
- [ ] `npm run build` passes with 0 TypeScript errors
- [ ] `finances.budgets` table exists in the database with all expected columns
- [ ] `GET /api/budgets` returns all budgets for the authenticated user with spending data
- [ ] `POST /api/budgets` creates a budget; returns 409 if a budget for the category already exists
- [ ] `PUT /api/budgets/{id}` updates a budget; returns 404 if not owned by the user
- [ ] `DELETE /api/budgets/{id}` deletes a budget; returns 404 if not owned by the user
- [ ] Frontend `/budgets` page is functional end-to-end (create, view, update, delete)
- [ ] Progress bar displays correct spending percentage and over-budget state
- [ ] Pre-sprint cleanup items (duplicate DI, stale TODO) resolved

---

*Last updated: 02/06/2026*
