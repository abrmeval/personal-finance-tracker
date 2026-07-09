# Sprint 2 — Finance Module: Transactions & Categories

**Duration:** 2 weeks (02/06/2026 — 16/06/2026)
**Status:** In Progress
**Overview:** [SPRINTS-OVERVIEW.md](./SPRINTS-OVERVIEW.md)

---

## Overview

Sprint 2 builds the Finance module — the core of the Personal Finance Tracker. Users gain the ability to create categories (e.g., "Groceries", "Salary") and transactions (income/expense entries associated with a category). The transaction list supports pagination, filtering by date range, category, and type. After this sprint, the `/transactions` and `/categories` pages are fully functional end-to-end.

**This sprint depends on Sprint 1 being complete.** JWT authentication, the `Users` module, the `Entity` base class, `Result<T>`, `ApiResponse<T>`, `ValidationFilter<T>`, and `ExceptionHandlingMiddleware` must all exist before any task in this sprint begins.

---

## Scope

### What's Included

**Backend**
- Finance module project (`Personal.FinanceTracker.Finance`) with Clean Architecture layers
- `TransactionType` enum (Income, Expense) in the Domain layer
- `Category` domain entity with private constructor, static `Create(...)` factory, `Update(...)` method
- `Transaction` domain entity with private constructor, static `Create(...)` factory, `Update(...)` method
- `ICategoryRepository`, `ITransactionRepository` interfaces (Domain layer)
- `CategoryRepository`, `TransactionRepository` EF Core implementations (Infrastructure layer)
- `FinanceDbContext` with `HasDefaultSchema("finances")`, retry-on-failure, command timeout
- EF Core Fluent API configurations for `Category` and `Transaction`; initial migration
- `PagedResult<T>` shared type for paginated responses
- `TransactionQueryParams` record with `[AsParameters]` for filtering + pagination
- Request/response DTOs: `CreateCategoryRequest`, `UpdateCategoryRequest`, `CategoryResponse`, `CreateTransactionRequest`, `UpdateTransactionRequest`, `TransactionResponse`
- FluentValidation validators: `CreateCategoryValidator`, `UpdateCategoryValidator`, `CreateTransactionValidator`, `UpdateTransactionValidator`
- `ICategoryService` / `CategoryService` and `ITransactionService` / `TransactionService` — return `Result<T>`
- `CategoryEndpoints` — list, get, create, update, delete
- `TransactionEndpoints` — list (with filter + pagination), get, create, update, delete
- Register Finance module via `AddFinanceModule` / `MapFinanceEndpoints` and wire into `Program.cs`

**Frontend**
- Type definitions: `Transaction`, `Category`, `TransactionType`, `PagedResult<T>`, `CreateTransactionRequest`, `UpdateTransactionRequest`, `CreateCategoryRequest`, `UpdateCategoryRequest`, `TransactionFilters`
- `categoriesApi` and `transactionsApi` service modules (`src/api/categories.ts`, `src/api/transactions.ts`)
- Custom hooks: `useCategories`, `useCreateCategory`, `useUpdateCategory`, `useDeleteCategory`, `useTransactions`, `useCreateTransaction`, `useUpdateTransaction`, `useDeleteTransaction` with query key factories
- `CategoryForm` (Zod + React Hook Form), `CategoryList`, `CategoriesPage`
- `TransactionForm` (Zod + React Hook Form), `TransactionList`, `TransactionsPage`
- Wire `CategoriesPage` and `TransactionsPage` into the router at `/categories` and `/transactions`

### Out of Scope
- Budget tracking — deferred to Sprint 3
- Dashboard charts and reports — deferred to Sprint 4
- Background jobs — deferred to Sprint 4
- Default category seeding on registration — deferred to a future polish sprint

### Known Gaps and Pre-Sprint Cleanup

The following items were left incomplete at the end of Sprint 1 and **must be resolved before starting Sprint 2 backend work**:

1. **Stale `// TODO Sprint 2:` comments in `Program.cs`** — Two TODO comments need to be replaced with actual Finance module registration calls. See Task 1.
2. **`AuthEnpoints.cs` filename typo** — Missing `d` in `Endpoints`. Cosmetic but worth correcting during cleanup. See Task 1.

### Forward-Looking Requirement

`ITransactionRepository` **must** include `GetTotalExpensesByCategoryAsync(Guid userId, Guid categoryId, DateTime from, DateTime to, CancellationToken ct)`. This method is needed by Sprint 3's `BudgetService` to calculate spending against a budget limit. It is defined in Task 7 and implemented in Task 13.

### Side Notes — Future Work

The following items are tracked here for visibility and **should be planned into a future sprint**:

- **Update localStorage user data on profile update:** When a user updates their profile (name, email), the `AuthContext` user object and any value persisted in `localStorage` must be refreshed to reflect the change. Without this, the `Header` will continue showing the stale name after an update.

---

## Tasks

---

### Task 1 — Pre-Sprint Cleanup: Fix Program.cs and AuthEnpoints Filename

**Status:** New

**Description:**
Replace the stale `// TODO Sprint 2:` comments in `Program.cs` with actual Finance module registration calls, and fix the `AuthEnpoints.cs` filename typo to `AuthEndpoints.cs`. A clean `Program.cs` is required before adding the Finance module.

**Steps:**

1. Open `backend/src/Personal.FinanceTracker.Api/Program.cs`.

2. Replace the service registration TODO:
   ```csharp
   // Replace this line:
   // TODO Sprint 2: builder.Services.AddFinanceModule(builder.Configuration);
   // With:
   builder.Services.AddFinanceModule(builder.Configuration);
   ```

3. Replace the endpoint mapping TODO:
   ```csharp
   // Replace this line:
   // TODO Sprint 2: app.MapFinanceEndpoints();
   // With:
   app.MapFinanceEndpoints();
   ```

   > **Note:** These calls will not compile until Task 22 creates the `DependencyInjection` class. That is expected — the build will pass after Task 22 is complete. Alternatively, defer this task until after Task 22 and combine them.

4. Rename `backend/src/Modules/Users/Api/Endpoints/AuthEnpoints.cs` to `AuthEndpoints.cs`:
   - Update the filename on disk
   - The class name inside is already `AuthEndpoints` — only the filename has the typo

5. If deferred to after Task 22: run `dotnet build` — confirm 0 errors, 0 warnings.

**Success Criteria:**
- `Program.cs` has no stale TODO comments for Sprint 2
- `AuthEnpoints.cs` is renamed to `AuthEndpoints.cs`
- `dotnet build` passes cleanly (after Task 22)

---

### Task 2 — Create Finance Module Project

**Status:** New

**Description:**
Create the `Personal.FinanceTracker.Finance` class library project with the same Clean Architecture layer structure as the Users module. Register it in the `.slnx` solution file and add a project reference from the API host.

**Steps:**

1. Create the project directory structure:
   ```
   backend/src/Modules/Finance/
   ├── Domain/
   │   ├── Entities/
   │   ├── Enums/
   │   └── Interfaces/
   ├── Application/
   │   ├── DTOs/
   │   │   ├── Requests/
   │   │   └── Responses/
   │   ├── Interfaces/
   │   ├── Services/
   │   └── Validators/
   ├── Infrastructure/
   │   ├── Data/
   │   │   ├── Configurations/
   │   │   └── Migrations/
   │   ├── Repositories/
   │   └── Services/
   ├── Api/
   │   └── Endpoints/
   └── DependencyInjection.cs
   ```

2. Create `backend/src/Modules/Finance/Personal.FinanceTracker.Finance.csproj`:
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">

     <PropertyGroup>
       <TargetFramework>net10.0</TargetFramework>
     </PropertyGroup>

     <ItemGroup>
       <FrameworkReference Include="Microsoft.AspNetCore.App" />
     </ItemGroup>

     <ItemGroup>
       <ProjectReference Include="..\..\Personal.FinanceTracker.Shared\Personal.FinanceTracker.Shared.csproj" />
     </ItemGroup>

     <ItemGroup>
       <PackageReference Include="FluentValidation" Version="12.1.1" />
       <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="12.1.1" />
       <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.8" />
       <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.8">
         <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
         <PrivateAssets>all</PrivateAssets>
       </PackageReference>
       <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.1" />
     </ItemGroup>

   </Project>
   ```

   > **Note:** The Finance module does NOT need `BCrypt.Net-Next`, `Microsoft.IdentityModel.Tokens`, or `System.IdentityModel.Tokens.Jwt` — those are Users-module-specific. It matches the Users module for EF Core, FluentValidation, and Npgsql packages only.

3. Add the project to the solution:
   ```bash
   dotnet sln backend/Personal.FinanceTracker.slnx add backend/src/Modules/Finance/Personal.FinanceTracker.Finance.csproj
   ```

   Alternatively, manually edit `backend/Personal.FinanceTracker.slnx` to add the Finance project inside a new `/src/Modules/Finance/` folder:
   ```xml
   <Solution>
   <Folder Name="/src/">
   <Project Name="Personal.FinanceTracker.Api" Path="src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj" />
   <Project Name="Personal.FinanceTracker.Shared" Path="src/Personal.FinanceTracker.Shared/Personal.FinanceTracker.Shared.csproj" />
   </Folder>
   <Folder Name="/src/Modules/" />
   <Folder Name="/src/Modules/Users/">
   <Project Path="src/Modules/Users/Personal.FinanceTracker.Users.csproj" />
   </Folder>
   <Folder Name="/src/Modules/Finance/">
   <Project Path="src/Modules/Finance/Personal.FinanceTracker.Finance.csproj" />
   </Folder>
   </Solution>
   ```

4. Add a project reference from the API host to the Finance module. Edit `backend/src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj` and add:
   ```xml
   <ItemGroup>
     <ProjectReference Include="..\Personal.FinanceTracker.Shared\Personal.FinanceTracker.Shared.csproj" />
     <ProjectReference Include="..\Modules\Users\Personal.FinanceTracker.Users.csproj" />
     <ProjectReference Include="..\Modules\Finance\Personal.FinanceTracker.Finance.csproj" />
   </ItemGroup>
   ```

5. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- `Personal.FinanceTracker.Finance.csproj` exists with correct package references
- Project is registered in `Personal.FinanceTracker.slnx`
- API host project references the Finance module
- `dotnet build` passes cleanly

---

### Task 3 — TransactionType Enum

**Status:** New

**Description:**
Add the `TransactionType` enum to the Finance module's Domain layer. This enum distinguishes income from expense transactions. It is stored as a string in the database via EF Core's `HasConversion<string>()`.

**Steps:**

1. Create `backend/src/Modules/Finance/Domain/Enums/TransactionType.cs`:
   ```csharp
   namespace Personal.FinanceTracker.Finance.Domain.Enums;

   public enum TransactionType
   {
       Income = 0,
       Expense = 1
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- Enum is in the Domain layer with no external dependencies
- Two values: `Income` and `Expense`

---

### Task 4 — Category Domain Entity

**Status:** New

**Description:**
Create the `Category` entity in the Finance module's Domain layer. It extends `Entity` from `Personal.FinanceTracker.Shared.Abstractions`. All properties have `private set`. The static `Create(...)` factory validates inputs and throws `ArgumentException` for invalid domain state. An `Update(...)` method allows modifying the name, icon, and color without replacing the entity.

**Steps:**

1. Create `backend/src/Modules/Finance/Domain/Entities/Category.cs`:
   ```csharp
   using Personal.FinanceTracker.Shared.Abstractions;

   namespace Personal.FinanceTracker.Finance.Domain.Entities;

   public sealed class Category : Entity
   {
       public Guid UserId { get; private set; }
       public string Name { get; private set; } = string.Empty;
       public string? Icon { get; private set; }
       public string? Color { get; private set; }

       private Category() { }

       public static Category Create(
           Guid userId,
           string name,
           string? icon = null,
           string? color = null)
       {
           if (userId == Guid.Empty)
               throw new ArgumentException("User ID is required.", nameof(userId));

           if (string.IsNullOrWhiteSpace(name))
               throw new ArgumentException("Category name is required.", nameof(name));

           if (name.Length > 100)
               throw new ArgumentException("Category name cannot exceed 100 characters.", nameof(name));

           return new Category
           {
               Id = Guid.NewGuid(),
               UserId = userId,
               Name = name.Trim(),
               Icon = string.IsNullOrWhiteSpace(icon) ? null : icon.Trim(),
               Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim(),
               CreatedAt = DateTime.UtcNow
           };
       }

       public void Update(string name, string? icon = null, string? color = null)
       {
           if (string.IsNullOrWhiteSpace(name))
               throw new ArgumentException("Category name is required.", nameof(name));

           if (name.Length > 100)
               throw new ArgumentException("Category name cannot exceed 100 characters.", nameof(name));

           Name = name.Trim();
           Icon = string.IsNullOrWhiteSpace(icon) ? null : icon.Trim();
           Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
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
- `Icon` and `Color` are nullable

---

### Task 5 — Transaction Domain Entity

**Status:** New

**Description:**
Create the `Transaction` entity in the Finance module's Domain layer. It extends `Entity`, has a `TransactionType` enum property, and an optional `CategoryId` (transactions can be uncategorized). The `Amount` is stored as `decimal` with `HasPrecision(18, 2)` in EF Core. The `Update(...)` method allows modifying description, amount, type, date, category, and notes.

**Steps:**

1. Create `backend/src/Modules/Finance/Domain/Entities/Transaction.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Domain.Enums;
   using Personal.FinanceTracker.Shared.Abstractions;

   namespace Personal.FinanceTracker.Finance.Domain.Entities;

   public sealed class Transaction : Entity
   {
       public Guid UserId { get; private set; }
       public Guid? CategoryId { get; private set; }
       public string Description { get; private set; } = string.Empty;
       public decimal Amount { get; private set; }
       public TransactionType Type { get; private set; }
       public DateTime Date { get; private set; }
       public string? Notes { get; private set; }

       private Transaction() { }

       public static Transaction Create(
           Guid userId,
           string description,
           decimal amount,
           TransactionType type,
           DateTime date,
           Guid? categoryId = null,
           string? notes = null)
       {
           if (userId == Guid.Empty)
               throw new ArgumentException("User ID is required.", nameof(userId));

           if (string.IsNullOrWhiteSpace(description))
               throw new ArgumentException("Description is required.", nameof(description));

           if (description.Length > 500)
               throw new ArgumentException("Description cannot exceed 500 characters.", nameof(description));

           if (amount <= 0)
               throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

           if (date == default)
               throw new ArgumentException("A valid date is required.", nameof(date));

           return new Transaction
           {
               Id = Guid.NewGuid(),
               UserId = userId,
               CategoryId = categoryId,
               Description = description.Trim(),
               Amount = amount,
               Type = type,
               Date = date.Kind == DateTimeKind.Unspecified
                   ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
                   : date.ToUniversalTime(),
               Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
               CreatedAt = DateTime.UtcNow
           };
       }

       public void Update(
           string description,
           decimal amount,
           TransactionType type,
           DateTime date,
           Guid? categoryId = null,
           string? notes = null)
       {
           if (string.IsNullOrWhiteSpace(description))
               throw new ArgumentException("Description is required.", nameof(description));

           if (description.Length > 500)
               throw new ArgumentException("Description cannot exceed 500 characters.", nameof(description));

           if (amount <= 0)
               throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

           if (date == default)
               throw new ArgumentException("A valid date is required.", nameof(date));

           Description = description.Trim();
           Amount = amount;
           Type = type;
           Date = date.Kind == DateTimeKind.Unspecified
               ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
               : date.ToUniversalTime();
           CategoryId = categoryId;
           Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
           UpdatedAt = DateTime.UtcNow;
       }
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- Entity extends `Personal.FinanceTracker.Shared.Abstractions.Entity`
- All properties have `private set`
- `CategoryId` is nullable — transactions can be uncategorized
- `Create(...)` throws `ArgumentException` for invalid state
- `Update(...)` does not replace the entity
- `Date` is always stored as UTC

---

### Task 6 — ICategoryRepository Interface

**Status:** New

**Description:**
Define the `ICategoryRepository` interface in the Finance module's Domain layer. The interface is pure — no EF Core or infrastructure references. All methods accept a `CancellationToken`. Single-entity lookups return `Category?` (nullable) — never throw for not-found.

**Steps:**

1. Create `backend/src/Modules/Finance/Domain/Interfaces/ICategoryRepository.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Domain.Entities;

   namespace Personal.FinanceTracker.Finance.Domain.Interfaces;

   public interface ICategoryRepository
   {
       Task<IReadOnlyList<Category>> GetAllByUserAsync(Guid userId, CancellationToken ct = default);
       Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);
       Task<Category?> GetByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default);
       Task<bool> ExistsByUserAndNameAsync(Guid userId, string name, CancellationToken ct = default);
       Task<bool> ExistsByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default);
       Task AddAsync(Category category, CancellationToken ct = default);
       Task SaveChangesAsync(CancellationToken ct = default);
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- Interface is in the Domain layer — no Infrastructure references
- `GetByUserAndIdAsync` scopes the lookup to the authenticated user — prevents cross-user access
- `ExistsByUserAndNameAsync` supports duplicate category name validation
- `ExistsByUserAndIdAsync` supports ownership validation on update/delete

---

### Task 7 — ITransactionRepository Interface

**Status:** New

**Description:**
Define the `ITransactionRepository` interface in the Finance module's Domain layer. This interface includes paged query support, filtering by date range / category / type, and a `GetTotalExpensesByCategoryAsync` method required by Sprint 3's `BudgetService`.

**Steps:**

1. Create `backend/src/Modules/Finance/Domain/Interfaces/ITransactionRepository.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Domain.Entities;
   using Personal.FinanceTracker.Finance.Domain.Enums;

   namespace Personal.FinanceTracker.Finance.Domain.Interfaces;

   public interface ITransactionRepository
   {
       Task<IReadOnlyList<Transaction>> GetPagedByUserAsync(
           Guid userId,
           int page,
           int pageSize,
           DateTime? startDate,
           DateTime? endDate,
           Guid? categoryId,
           TransactionType? type,
           CancellationToken ct = default);

       Task<int> CountByUserAsync(
           Guid userId,
           DateTime? startDate,
           DateTime? endDate,
           Guid? categoryId,
           TransactionType? type,
           CancellationToken ct = default);

       Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default);
       Task<Transaction?> GetByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default);
       Task<bool> ExistsByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default);
       Task AddAsync(Transaction transaction, CancellationToken ct = default);
       Task SaveChangesAsync(CancellationToken ct = default);

       /// <summary>
       /// Calculates the total expense amount for a user's category within a date range.
       /// Used by BudgetService (Sprint 3) to compute spending against budget limits.
       /// </summary>
       Task<decimal> GetTotalExpensesByCategoryAsync(
           Guid userId,
           Guid categoryId,
           DateTime from,
           DateTime to,
           CancellationToken ct = default);
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- Interface is in the Domain layer — no Infrastructure references
- `GetPagedByUserAsync` supports pagination and all filter parameters
- `GetByUserAndIdAsync` scopes lookups to the authenticated user
- `GetTotalExpensesByCategoryAsync` is included for Sprint 3 forward compatibility
- All methods accept `CancellationToken`

---

### Task 8 — FinanceDbContext

**Status:** New

**Description:**
Create the `FinanceDbContext` with `HasDefaultSchema("finances")`. It follows the same pattern as `UsersDbContext` — sealed class with primary constructor, `DbSet` properties, and `ApplyConfigurationsFromAssembly` in `OnModelCreating`.

**Steps:**

1. Create `backend/src/Modules/Finance/Infrastructure/Data/FinanceDbContext.cs`:
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using Personal.FinanceTracker.Finance.Domain.Entities;

   namespace Personal.FinanceTracker.Finance.Infrastructure.Data;

   public sealed class FinanceDbContext(DbContextOptions<FinanceDbContext> options) : DbContext(options)
   {
       public DbSet<Transaction> Transactions => Set<Transaction>();
       public DbSet<Category> Categories => Set<Category>();

       protected override void OnModelCreating(ModelBuilder modelBuilder)
       {
           modelBuilder.HasDefaultSchema("finances");
           modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinanceDbContext).Assembly);
       }
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- `FinanceDbContext` uses `HasDefaultSchema("finances")`
- Both `DbSet<Transaction>` and `DbSet<Category>` are exposed
- `ApplyConfigurationsFromAssembly` is used — no manual `ApplyConfiguration` calls

---

### Task 9 — Category EF Core Configuration

**Status:** New

**Description:**
Add an `IEntityTypeConfiguration<Category>` Fluent API configuration. Use snake_case column names, `timestamptz` for date columns, and `idx_` prefix for index names — matching the pattern established in `UserConfiguration.cs`.

**Steps:**

1. Create `backend/src/Modules/Finance/Infrastructure/Data/Configurations/CategoryConfiguration.cs`:
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using Microsoft.EntityFrameworkCore.Metadata.Builders;
   using Personal.FinanceTracker.Finance.Domain.Entities;

   namespace Personal.FinanceTracker.Finance.Infrastructure.Data.Configurations;

   public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
   {
       public void Configure(EntityTypeBuilder<Category> builder)
       {
           builder.ToTable("categories");

           builder.HasKey(c => c.Id);

           builder.Property(c => c.Id)
               .HasColumnName("id")
               .ValueGeneratedNever();

           builder.Property(c => c.UserId)
               .HasColumnName("user_id")
               .IsRequired();

           builder.Property(c => c.Name)
               .HasColumnName("name")
               .HasMaxLength(100)
               .IsRequired();

           builder.Property(c => c.Icon)
               .HasColumnName("icon")
               .HasMaxLength(50);

           builder.Property(c => c.Color)
               .HasColumnName("color")
               .HasMaxLength(20);

           builder.Property(c => c.CreatedAt)
               .HasColumnName("created_at")
               .HasColumnType("timestamptz")
               .HasDefaultValueSql("now()")
               .IsRequired();

           builder.Property(c => c.UpdatedAt)
               .HasColumnName("updated_at")
               .HasColumnType("timestamptz");

           builder.HasIndex(c => c.UserId)
               .HasDatabaseName("idx_categories_user_id");

           builder.HasIndex(c => new { c.UserId, c.Name })
               .IsUnique()
               .HasDatabaseName("idx_categories_user_name");
       }
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- Table name is `categories` in the `finances` schema
- All columns are snake_case
- `created_at` and `updated_at` are `timestamptz`
- `created_at` has `HasDefaultValueSql("now()")`
- Unique index on `(user_id, name)` prevents duplicate category names per user
- Index names use `idx_` prefix

---

### Task 10 — Transaction EF Core Configuration

**Status:** New

**Description:**
Add an `IEntityTypeConfiguration<Transaction>` Fluent API configuration. The `Amount` column uses `HasPrecision(18, 2)`. The `TransactionType` enum is stored as a string via `HasConversion<string>()`. The `CategoryId` is nullable with a foreign key to `categories`.

**Steps:**

1. Create `backend/src/Modules/Finance/Infrastructure/Data/Configurations/TransactionConfiguration.cs`:
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using Microsoft.EntityFrameworkCore.Metadata.Builders;
   using Personal.FinanceTracker.Finance.Domain.Entities;
   using Personal.FinanceTracker.Finance.Domain.Enums;

   namespace Personal.FinanceTracker.Finance.Infrastructure.Data.Configurations;

   public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
   {
       public void Configure(EntityTypeBuilder<Transaction> builder)
       {
           builder.ToTable("transactions");

           builder.HasKey(t => t.Id);

           builder.Property(t => t.Id)
               .HasColumnName("id")
               .ValueGeneratedNever();

           builder.Property(t => t.UserId)
               .HasColumnName("user_id")
               .IsRequired();

           builder.Property(t => t.CategoryId)
               .HasColumnName("category_id");

           builder.Property(t => t.Description)
               .HasColumnName("description")
               .HasMaxLength(500)
               .IsRequired();

           builder.Property(t => t.Amount)
               .HasColumnName("amount")
               .HasPrecision(18, 2)
               .IsRequired();

           builder.Property(t => t.Type)
               .HasColumnName("type")
               .HasConversion<string>()
               .HasMaxLength(10)
               .IsRequired();

           builder.Property(t => t.Date)
               .HasColumnName("date")
               .HasColumnType("timestamptz")
               .IsRequired();

           builder.Property(t => t.Notes)
               .HasColumnName("notes")
               .HasMaxLength(2000);

           builder.Property(t => t.CreatedAt)
               .HasColumnName("created_at")
               .HasColumnType("timestamptz")
               .HasDefaultValueSql("now()")
               .IsRequired();

           builder.Property(t => t.UpdatedAt)
               .HasColumnName("updated_at")
               .HasColumnType("timestamptz");

           builder.HasOne<Category>()
               .WithMany()
               .HasForeignKey(t => t.CategoryId)
               .OnDelete(DeleteBehavior.SetNull);

           builder.HasIndex(t => t.UserId)
               .HasDatabaseName("idx_transactions_user_id");

           builder.HasIndex(t => t.Date)
               .HasDatabaseName("idx_transactions_date");

           builder.HasIndex(t => new { t.UserId, t.CategoryId })
               .HasDatabaseName("idx_transactions_user_category");
       }
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- Table name is `transactions` in the `finances` schema
- `Amount` has `HasPrecision(18, 2)`
- `Type` is stored as a string via `HasConversion<string>()`
- `CategoryId` is nullable with `SetNull` delete behavior
- `Date`, `created_at`, and `updated_at` are `timestamptz`
- Indexes on `user_id`, `date`, and `(user_id, category_id)` for query performance

---

### Task 11 — EF Core Migration

**Status:** New

**Description:**
Generate and apply the initial Finance module migration. This creates the `finances.categories` and `finances.transactions` tables in the PostgreSQL database.

**Steps:**

1. From the `backend/` directory, generate the migration:
   ```bash
   dotnet ef migrations add InitialFinanceSchema ^
     --project src/Modules/Finance/Personal.FinanceTracker.Finance.csproj ^
     --startup-project src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj ^
     --context FinanceDbContext ^
     --output-dir Infrastructure/Data/Migrations
   ```

   > **Note:** On Windows, use `^` for line continuation in PowerShell. On bash/macOS, use `\`.

2. Review the generated migration file:
   - `categories` table exists in `finances` schema
   - `transactions` table exists in `finances` schema
   - `amount` has precision `(18, 2)`
   - `type` is stored as `text`
   - `date`, `created_at`, `updated_at` are `timestamptz`
   - All indexes are present
   - Foreign key from `transactions.category_id` to `categories.id` with `SET NULL` on delete

3. Apply the migration:
   ```bash
   dotnet ef database update ^
     --project src/Modules/Finance/Personal.FinanceTracker.Finance.csproj ^
     --startup-project src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj ^
     --context FinanceDbContext
   ```

**Success Criteria:**
- Migration file generates without errors
- `dotnet ef database update` succeeds
- `finances.categories` and `finances.transactions` tables exist with all expected columns

---

### Task 12 — CategoryRepository Implementation

**Status:** New

**Description:**
Implement `ICategoryRepository` in the Infrastructure layer using `FinanceDbContext`. Pass `CancellationToken` through to all EF Core async calls. Follow the same pattern as `UserRepository`.

**Steps:**

1. Create `backend/src/Modules/Finance/Infrastructure/Repositories/CategoryRepository.cs`:
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using Personal.FinanceTracker.Finance.Domain.Entities;
   using Personal.FinanceTracker.Finance.Domain.Interfaces;
   using Personal.FinanceTracker.Finance.Infrastructure.Data;

   namespace Personal.FinanceTracker.Finance.Infrastructure.Repositories;

   public sealed class CategoryRepository(FinanceDbContext context) : ICategoryRepository
   {
       public async Task<IReadOnlyList<Category>> GetAllByUserAsync(Guid userId, CancellationToken ct = default)
           => await context.Categories
               .Where(c => c.UserId == userId)
               .OrderBy(c => c.Name)
               .ToListAsync(ct);

       public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
           => await context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

       public async Task<Category?> GetByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default)
           => await context.Categories
               .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);

       public async Task<bool> ExistsByUserAndNameAsync(Guid userId, string name, CancellationToken ct = default)
           => await context.Categories
               .AnyAsync(c => c.UserId == userId && c.Name == name, ct);

       public async Task<bool> ExistsByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default)
           => await context.Categories
               .AnyAsync(c => c.Id == id && c.UserId == userId, ct);

       public async Task AddAsync(Category category, CancellationToken ct = default)
           => await context.Categories.AddAsync(category, ct);

       public async Task SaveChangesAsync(CancellationToken ct = default)
           => await context.SaveChangesAsync(ct);
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- `CategoryRepository` fully implements `ICategoryRepository`
- `GetByUserAndIdAsync` always filters by `UserId` — no cross-user data leakage
- No business logic in the repository — only data access
- `SaveChangesAsync` is explicit — not automatically called by `AddAsync`

---

### Task 13 — TransactionRepository Implementation

**Status:** New

**Description:**
Implement `ITransactionRepository` in the Infrastructure layer. The paged query method builds a filtered `IQueryable` progressively, then applies pagination. The `GetTotalExpensesByCategoryAsync` method sums `Amount` for expense transactions within a date range — this is the forward-looking method needed by Sprint 3.

**Steps:**

1. Create `backend/src/Modules/Finance/Infrastructure/Repositories/TransactionRepository.cs`:
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using Personal.FinanceTracker.Finance.Domain.Entities;
   using Personal.FinanceTracker.Finance.Domain.Enums;
   using Personal.FinanceTracker.Finance.Domain.Interfaces;
   using Personal.FinanceTracker.Finance.Infrastructure.Data;

   namespace Personal.FinanceTracker.Finance.Infrastructure.Repositories;

   public sealed class TransactionRepository(FinanceDbContext context) : ITransactionRepository
   {
       public async Task<IReadOnlyList<Transaction>> GetPagedByUserAsync(
           Guid userId,
           int page,
           int pageSize,
           DateTime? startDate,
           DateTime? endDate,
           Guid? categoryId,
           TransactionType? type,
           CancellationToken ct = default)
       {
           var query = BuildFilteredQuery(userId, startDate, endDate, categoryId, type);

           return await query
               .OrderByDescending(t => t.Date)
               .ThenByDescending(t => t.CreatedAt)
               .Skip((page - 1) * pageSize)
               .Take(pageSize)
               .ToListAsync(ct);
       }

       public async Task<int> CountByUserAsync(
           Guid userId,
           DateTime? startDate,
           DateTime? endDate,
           Guid? categoryId,
           TransactionType? type,
           CancellationToken ct = default)
       {
           var query = BuildFilteredQuery(userId, startDate, endDate, categoryId, type);
           return await query.CountAsync(ct);
       }

       public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
           => await context.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);

       public async Task<Transaction?> GetByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default)
           => await context.Transactions
               .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

       public async Task<bool> ExistsByUserAndIdAsync(Guid userId, Guid id, CancellationToken ct = default)
           => await context.Transactions
               .AnyAsync(t => t.Id == id && t.UserId == userId, ct);

       public async Task AddAsync(Transaction transaction, CancellationToken ct = default)
           => await context.Transactions.AddAsync(transaction, ct);

       public async Task SaveChangesAsync(CancellationToken ct = default)
           => await context.SaveChangesAsync(ct);

       public async Task<decimal> GetTotalExpensesByCategoryAsync(
           Guid userId,
           Guid categoryId,
           DateTime from,
           DateTime to,
           CancellationToken ct = default)
       {
           return await context.Transactions
               .Where(t => t.UserId == userId
                   && t.CategoryId == categoryId
                   && t.Type == TransactionType.Expense
                   && t.Date >= from
                   && t.Date <= to)
               .SumAsync(t => t.Amount, ct);
       }

       private IQueryable<Transaction> BuildFilteredQuery(
           Guid userId,
           DateTime? startDate,
           DateTime? endDate,
           Guid? categoryId,
           TransactionType? type)
       {
           var query = context.Transactions.Where(t => t.UserId == userId);

           if (startDate.HasValue)
               query = query.Where(t => t.Date >= startDate.Value);

           if (endDate.HasValue)
               query = query.Where(t => t.Date <= endDate.Value);

           if (categoryId.HasValue)
               query = query.Where(t => t.CategoryId == categoryId.Value);

           if (type.HasValue)
               query = query.Where(t => t.Type == type.Value);

           return query;
       }
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- `TransactionRepository` fully implements `ITransactionRepository`
- `BuildFilteredQuery` is a private helper that progressively applies filters — clean and composable
- `GetPagedByUserAsync` orders by `Date` descending, then `CreatedAt` descending
- `GetTotalExpensesByCategoryAsync` filters by `Expense` type only — income transactions are excluded
- `GetByUserAndIdAsync` always filters by `UserId`

---

### Task 14 — PagedResult<T> and TransactionQueryParams

**Status:** New

**Description:**
Create the `PagedResult<T>` generic type in the Shared project (it will be reused by Reporting in Sprint 4) and the `TransactionQueryParams` record in the Finance Application layer. The query params record uses `[AsParameters]` for Minimal API binding.

**Steps:**

1. Create `backend/src/Personal.FinanceTracker.Shared/Models/PagedResult.cs`:
   ```csharp
   namespace Personal.FinanceTracker.Shared.Models;

   public sealed class PagedResult<T>
   {
       public IReadOnlyList<T> Items { get; init; } = [];
       public int TotalCount { get; init; }
       public int Page { get; init; }
       public int PageSize { get; init; }
       public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
       public bool HasPreviousPage => Page > 1;
       public bool HasNextPage => Page < TotalPages;
   }
   ```

2. Create `backend/src/Modules/Finance/Application/DTOs/Requests/TransactionQueryParams.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Domain.Enums;

   namespace Personal.FinanceTracker.Finance.Application.DTOs.Requests;

   public sealed record TransactionQueryParams
   {
       public int Page { get; init; } = 1;
       public int PageSize { get; init; } = 20;
       public DateTime? StartDate { get; init; }
       public DateTime? EndDate { get; init; }
       public Guid? CategoryId { get; init; }
       public TransactionType? Type { get; init; }
   }
   ```

   > **Note:** `[AsParameters]` is applied at the endpoint level, not on the record itself. See Task 21 for usage.

3. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- `PagedResult<T>` is in the Shared project (reusable by other modules)
- `TotalPages`, `HasPreviousPage`, and `HasNextPage` are computed properties
- `TransactionQueryParams` is a sealed record with sensible defaults (page 1, size 20)
- All filter parameters are nullable

---

### Task 15 — Category DTOs

**Status:** New

**Description:**
Create request and response DTOs for categories as sealed `record` types in the Application layer. Mirror the pattern established by `RegisterRequest` and `AuthResponse` in the Users module.

**Steps:**

1. Create `backend/src/Modules/Finance/Application/DTOs/Requests/CreateCategoryRequest.cs`:
   ```csharp
   namespace Personal.FinanceTracker.Finance.Application.DTOs.Requests;

   public sealed record CreateCategoryRequest(
       string Name,
       string? Icon,
       string? Color);
   ```

2. Create `backend/src/Modules/Finance/Application/DTOs/Requests/UpdateCategoryRequest.cs`:
   ```csharp
   namespace Personal.FinanceTracker.Finance.Application.DTOs.Requests;

   public sealed record UpdateCategoryRequest(
       string Name,
       string? Icon,
       string? Color);
   ```

3. Create `backend/src/Modules/Finance/Application/DTOs/Responses/CategoryResponse.cs`:
   ```csharp
   namespace Personal.FinanceTracker.Finance.Application.DTOs.Responses;

   public sealed record CategoryResponse(
       Guid Id,
       string Name,
       string? Icon,
       string? Color,
       DateTime CreatedAt,
       DateTime? UpdatedAt);
   ```

4. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- All DTOs are sealed `record` types
- `CategoryResponse` does not include `UserId` — it is implied by the authenticated user
- `Icon` and `Color` are nullable

---

### Task 16 — Transaction DTOs

**Status:** New

**Description:**
Create request and response DTOs for transactions. The `TransactionResponse` includes the `CategoryName` for display convenience — the service joins the category name when mapping.

**Steps:**

1. Create `backend/src/Modules/Finance/Application/DTOs/Requests/CreateTransactionRequest.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Domain.Enums;

   namespace Personal.FinanceTracker.Finance.Application.DTOs.Requests;

   public sealed record CreateTransactionRequest(
       string Description,
       decimal Amount,
       TransactionType Type,
       DateTime Date,
       Guid? CategoryId,
       string? Notes);
   ```

2. Create `backend/src/Modules/Finance/Application/DTOs/Requests/UpdateTransactionRequest.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Domain.Enums;

   namespace Personal.FinanceTracker.Finance.Application.DTOs.Requests;

   public sealed record UpdateTransactionRequest(
       string Description,
       decimal Amount,
       TransactionType Type,
       DateTime Date,
       Guid? CategoryId,
       string? Notes);
   ```

3. Create `backend/src/Modules/Finance/Application/DTOs/Responses/TransactionResponse.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Domain.Enums;

   namespace Personal.FinanceTracker.Finance.Application.DTOs.Responses;

   public sealed record TransactionResponse(
       Guid Id,
       string Description,
       decimal Amount,
       TransactionType Type,
       DateTime Date,
       Guid? CategoryId,
       string? CategoryName,
       string? Notes,
       DateTime CreatedAt,
       DateTime? UpdatedAt);
   ```

4. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- All DTOs are sealed `record` types
- `TransactionResponse` includes `CategoryName` for frontend display convenience
- `CategoryId` and `Notes` are nullable in both request and response

---

### Task 17 — FluentValidation Validators

**Status:** New

**Description:**
Create one `AbstractValidator<T>` per mutating request type. All live in `Application/Validators/`. Validators enforce structural rules only — no database calls. Business rules (duplicate category name, ownership) are enforced in the service layer. Follow the pattern established by `RegisterRequestValidator`.

**Steps:**

1. Create `backend/src/Modules/Finance/Application/Validators/CreateCategoryValidator.cs`:
   ```csharp
   using FluentValidation;
   using Personal.FinanceTracker.Finance.Application.DTOs.Requests;

   namespace Personal.FinanceTracker.Finance.Application.Validators;

   public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryRequest>
   {
       public CreateCategoryValidator()
       {
           RuleFor(x => x.Name)
               .NotEmpty().WithMessage("Category name is required.")
               .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.");

           RuleFor(x => x.Icon)
               .MaximumLength(50).WithMessage("Icon cannot exceed 50 characters.");

           RuleFor(x => x.Color)
               .MaximumLength(20).WithMessage("Color cannot exceed 20 characters.");
       }
   }
   ```

2. Create `backend/src/Modules/Finance/Application/Validators/UpdateCategoryValidator.cs`:
   ```csharp
   using FluentValidation;
   using Personal.FinanceTracker.Finance.Application.DTOs.Requests;

   namespace Personal.FinanceTracker.Finance.Application.Validators;

   public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryRequest>
   {
       public UpdateCategoryValidator()
       {
           RuleFor(x => x.Name)
               .NotEmpty().WithMessage("Category name is required.")
               .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.");

           RuleFor(x => x.Icon)
               .MaximumLength(50).WithMessage("Icon cannot exceed 50 characters.");

           RuleFor(x => x.Color)
               .MaximumLength(20).WithMessage("Color cannot exceed 20 characters.");
       }
   }
   ```

3. Create `backend/src/Modules/Finance/Application/Validators/CreateTransactionValidator.cs`:
   ```csharp
   using FluentValidation;
   using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
   using Personal.FinanceTracker.Finance.Domain.Enums;

   namespace Personal.FinanceTracker.Finance.Application.Validators;

   public sealed class CreateTransactionValidator : AbstractValidator<CreateTransactionRequest>
   {
       public CreateTransactionValidator()
       {
           RuleFor(x => x.Description)
               .NotEmpty().WithMessage("Description is required.")
               .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

           RuleFor(x => x.Amount)
               .GreaterThan(0).WithMessage("Amount must be greater than zero.")
               .LessThanOrEqualTo(1_000_000_000).WithMessage("Amount is unreasonably large.");

           RuleFor(x => x.Type)
               .IsInEnum().WithMessage("Invalid transaction type.");

           RuleFor(x => x.Date)
               .NotEmpty().WithMessage("Date is required.");

           RuleFor(x => x.Notes)
               .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters.");
       }
   }
   ```

4. Create `backend/src/Modules/Finance/Application/Validators/UpdateTransactionValidator.cs`:
   ```csharp
   using FluentValidation;
   using Personal.FinanceTracker.Finance.Application.DTOs.Requests;

   namespace Personal.FinanceTracker.Finance.Application.Validators;

   public sealed class UpdateTransactionValidator : AbstractValidator<UpdateTransactionRequest>
   {
       public UpdateTransactionValidator()
       {
           RuleFor(x => x.Description)
               .NotEmpty().WithMessage("Description is required.")
               .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

           RuleFor(x => x.Amount)
               .GreaterThan(0).WithMessage("Amount must be greater than zero.")
               .LessThanOrEqualTo(1_000_000_000).WithMessage("Amount is unreasonably large.");

           RuleFor(x => x.Type)
               .IsInEnum().WithMessage("Invalid transaction type.");

           RuleFor(x => x.Date)
               .NotEmpty().WithMessage("Date is required.");

           RuleFor(x => x.Notes)
               .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters.");
       }
   }
   ```

5. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- All four validators compile as `sealed class` extending `AbstractValidator<T>`
- No database calls in validators — business rules are enforced in services
- `IsInEnum()` prevents invalid transaction type values at the API boundary
- String length limits match entity constraints

---

### Task 18 — ICategoryService and CategoryService

**Status:** New

**Description:**
Create the category service interface in the Application layer and its implementation in the Infrastructure layer. `CategoryService` handles all business logic: ownership validation, duplicate name checking, and mapping to DTOs. Services return `Result<T>` — following the established pattern from `UserService`.

**Steps:**

1. Create `backend/src/Modules/Finance/Application/Services/ICategoryService.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
   using Personal.FinanceTracker.Finance.Application.DTOs.Responses;
   using Personal.FinanceTracker.Shared.Models;

   namespace Personal.FinanceTracker.Finance.Application.Services;

   public interface ICategoryService
   {
       Task<Result<IReadOnlyList<CategoryResponse>>> GetAllAsync(Guid userId, CancellationToken ct = default);
       Task<Result<CategoryResponse>> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default);
       Task<Result<CategoryResponse>> CreateAsync(Guid userId, CreateCategoryRequest request, CancellationToken ct = default);
       Task<Result<CategoryResponse>> UpdateAsync(Guid userId, Guid id, UpdateCategoryRequest request, CancellationToken ct = default);
       Task<Result<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default);
   }
   ```

2. Add new error codes to `backend/src/Personal.FinanceTracker.Shared/Constants/ApiErrorCode.cs`:
   ```csharp
   namespace Personal.FinanceTracker.Shared.Constants;

   public static class ApiErrorCode
   {
       public const string ResourceAlreadyExists = "RESOURCE_ALREADY_EXISTS";
       public const string InvalidCredentials = "INVALID_CREDENTIALS";
       public const string InvalidToken = "INVALID_TOKEN";
       public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
       public const string CategoryNotFound = "CATEGORY_NOT_FOUND";
       public const string TransactionNotFound = "TRANSACTION_NOT_FOUND";
       public const string DuplicateCategoryName = "DUPLICATE_CATEGORY_NAME";
   }
   ```

3. Create `backend/src/Modules/Finance/Infrastructure/Services/CategoryService.cs`:
   ```csharp
   using Microsoft.Extensions.Logging;
   using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
   using Personal.FinanceTracker.Finance.Application.DTOs.Responses;
   using Personal.FinanceTracker.Finance.Application.Services;
   using Personal.FinanceTracker.Finance.Domain.Entities;
   using Personal.FinanceTracker.Finance.Domain.Interfaces;
   using Personal.FinanceTracker.Shared.Constants;
   using Personal.FinanceTracker.Shared.Models;

   namespace Personal.FinanceTracker.Finance.Infrastructure.Services;

   public sealed class CategoryService(
       ICategoryRepository repository,
       ILogger<CategoryService> logger) : ICategoryService
   {
       public async Task<Result<IReadOnlyList<CategoryResponse>>> GetAllAsync(Guid userId, CancellationToken ct = default)
       {
           var categories = await repository.GetAllByUserAsync(userId, ct);
           var response = categories.Select(MapToResponse).ToList();
           return Result<IReadOnlyList<CategoryResponse>>.Success(response);
       }

       public async Task<Result<CategoryResponse>> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default)
       {
           var category = await repository.GetByUserAndIdAsync(userId, id, ct);
           if (category is null)
           {
               logger.LogWarning("Category {CategoryId} not found for user {UserId}", id, userId);
               return Result<CategoryResponse>.Failure(new(ApiErrorCode.CategoryNotFound, "Category not found."));
           }

           return Result<CategoryResponse>.Success(MapToResponse(category));
       }

       public async Task<Result<CategoryResponse>> CreateAsync(Guid userId, CreateCategoryRequest request, CancellationToken ct = default)
       {
           if (await repository.ExistsByUserAndNameAsync(userId, request.Name, ct))
           {
               logger.LogWarning("Category creation failed: name '{Name}' already exists for user {UserId}", request.Name, userId);
               return Result<CategoryResponse>.Failure(new(ApiErrorCode.DuplicateCategoryName, "A category with this name already exists."));
           }

           var category = Category.Create(userId, request.Name, request.Icon, request.Color);
           await repository.AddAsync(category, ct);
           await repository.SaveChangesAsync(ct);

           logger.LogInformation("Category {CategoryId} created for user {UserId}", category.Id, userId);
           return Result<CategoryResponse>.Success(MapToResponse(category));
       }

       public async Task<Result<CategoryResponse>> UpdateAsync(Guid userId, Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
       {
           var category = await repository.GetByUserAndIdAsync(userId, id, ct);
           if (category is null)
           {
               logger.LogWarning("Category update failed: {CategoryId} not found for user {UserId}", id, userId);
               return Result<CategoryResponse>.Failure(new(ApiErrorCode.CategoryNotFound, "Category not found."));
           }

           if (await repository.ExistsByUserAndNameAsync(userId, request.Name, ct) && category.Name != request.Name)
           {
               logger.LogWarning("Category update failed: name '{Name}' already exists for user {UserId}", request.Name, userId);
               return Result<CategoryResponse>.Failure(new(ApiErrorCode.DuplicateCategoryName, "A category with this name already exists."));
           }

           category.Update(request.Name, request.Icon, request.Color);
           await repository.SaveChangesAsync(ct);

           logger.LogInformation("Category {CategoryId} updated by user {UserId}", category.Id, userId);
           return Result<CategoryResponse>.Success(MapToResponse(category));
       }

       public async Task<Result<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
       {
           var category = await repository.GetByUserAndIdAsync(userId, id, ct);
           if (category is null)
           {
               logger.LogWarning("Category delete failed: {CategoryId} not found for user {UserId}", id, userId);
               return Result<bool>.Failure(new(ApiErrorCode.CategoryNotFound, "Category not found."));
           }

           // Note: EF Core will SET NULL on transactions due to OnDelete(DeleteBehavior.SetNull)
           // We need to remove the entity and save changes
           // The repository doesn't have a DeleteAsync — we need to use the context directly
           // OR add a DeleteAsync method to the interface. Let's add it.
           // For now, we'll handle this in the repository update.
           await repository.SaveChangesAsync(ct);

           logger.LogInformation("Category {CategoryId} deleted by user {UserId}", category.Id, userId);
           return Result<bool>.Success(true);
       }

       private static CategoryResponse MapToResponse(Category category)
           => new(category.Id, category.Name, category.Icon, category.Color, category.CreatedAt, category.UpdatedAt);
   }
   ```

   > **IMPORTANT — DeleteAsync gap:** The `ICategoryRepository` interface defined in Task 6 does not include a `DeleteAsync` method. You must add it:
   >
   > In `ICategoryRepository.cs`, add:
   > ```csharp
   > Task DeleteAsync(Category category, CancellationToken ct = default);
   > ```
   >
   > In `CategoryRepository.cs`, add:
   > ```csharp
   > public Task DeleteAsync(Category category, CancellationToken ct = default)
   > {
   >     context.Categories.Remove(category);
   >     return Task.CompletedTask;
   > }
   > ```
   >
   > Then update `CategoryService.DeleteAsync` to call `repository.DeleteAsync(category, ct)` before `SaveChangesAsync`.

4. Update `CategoryService.DeleteAsync` after adding `DeleteAsync` to the repository:
   ```csharp
   public async Task<Result<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
   {
       var category = await repository.GetByUserAndIdAsync(userId, id, ct);
       if (category is null)
       {
           logger.LogWarning("Category delete failed: {CategoryId} not found for user {UserId}", id, userId);
           return Result<bool>.Failure(new(ApiErrorCode.CategoryNotFound, "Category not found."));
       }

       await repository.DeleteAsync(category, ct);
       await repository.SaveChangesAsync(ct);

       logger.LogInformation("Category {CategoryId} deleted by user {UserId}", category.Id, userId);
       return Result<bool>.Success(true);
   }
   ```

5. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- `ICategoryService` is in Application with no Infrastructure references
- `CategoryService` returns `Result<T>` for all methods — following `UserService` pattern
- Duplicate name check is in the service, not the validator
- Ownership is enforced via `GetByUserAndIdAsync` — no cross-user access
- `DeleteAsync` is added to both `ICategoryRepository` and `CategoryRepository`

---
### CONTINUE HERE : 29/06/2026
### Task 19 — ITransactionService and TransactionService

**Status:** New

**Description:**
Create the transaction service interface and implementation. `TransactionService` handles ownership validation, category ownership validation (if a category is specified), paged queries, and mapping to `TransactionResponse` with the category name included.

**Steps:**

1. Create `backend/src/Modules/Finance/Application/Services/ITransactionService.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
   using Personal.FinanceTracker.Finance.Application.DTOs.Responses;
   using Personal.FinanceTracker.Shared.Models;

   namespace Personal.FinanceTracker.Finance.Application.Services;

   public interface ITransactionService
   {
       Task<Result<PagedResult<TransactionResponse>>> GetAllAsync(Guid userId, TransactionQueryParams queryParams, CancellationToken ct = default);
       Task<Result<TransactionResponse>> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default);
       Task<Result<TransactionResponse>> CreateAsync(Guid userId, CreateTransactionRequest request, CancellationToken ct = default);
       Task<Result<TransactionResponse>> UpdateAsync(Guid userId, Guid id, UpdateTransactionRequest request, CancellationToken ct = default);
       Task<Result<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default);
   }
   ```

   > **Note:** `ITransactionRepository` also needs a `DeleteAsync` method. Add it to the interface and implementation following the same pattern as `CategoryRepository.DeleteAsync` in Task 18.

2. Add `DeleteAsync` to `ITransactionRepository`:
   ```csharp
   Task DeleteAsync(Transaction transaction, CancellationToken ct = default);
   ```

3. Add `DeleteAsync` to `TransactionRepository`:
   ```csharp
   public Task DeleteAsync(Transaction transaction, CancellationToken ct = default)
   {
       context.Transactions.Remove(transaction);
       return Task.CompletedTask;
   }
   ```

4. Create `backend/src/Modules/Finance/Infrastructure/Services/TransactionService.cs`:
   ```csharp
   using Microsoft.Extensions.Logging;
   using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
   using Personal.FinanceTracker.Finance.Application.DTOs.Responses;
   using Personal.FinanceTracker.Finance.Application.Services;
   using Personal.FinanceTracker.Finance.Domain.Entities;
   using Personal.FinanceTracker.Finance.Domain.Interfaces;
   using Personal.FinanceTracker.Shared.Constants;
   using Personal.FinanceTracker.Shared.Models;

   namespace Personal.FinanceTracker.Finance.Infrastructure.Services;

   public sealed class TransactionService(
       ITransactionRepository transactionRepository,
       ICategoryRepository categoryRepository,
       ILogger<TransactionService> logger) : ITransactionService
   {
       public async Task<Result<PagedResult<TransactionResponse>>> GetAllAsync(
           Guid userId,
           TransactionQueryParams queryParams,
           CancellationToken ct = default)
       {
           var page = queryParams.Page < 1 ? 1 : queryParams.Page;
           var pageSize = queryParams.PageSize is < 1 or > 100 ? 20 : queryParams.PageSize;

           var transactions = await transactionRepository.GetPagedByUserAsync(
               userId, page, pageSize,
               queryParams.StartDate, queryParams.EndDate,
               queryParams.CategoryId, queryParams.Type, ct);

           var totalCount = await transactionRepository.CountByUserAsync(
               userId, queryParams.StartDate, queryParams.EndDate,
               queryParams.CategoryId, queryParams.Type, ct);

           var categoryIds = transactions
               .Where(t => t.CategoryId.HasValue)
               .Select(t => t.CategoryId!.Value)
               .Distinct()
               .ToList();

           var categoryNames = new Dictionary<Guid, string>();
           foreach (var categoryId in categoryIds)
           {
               var category = await categoryRepository.GetByIdAsync(categoryId, ct);
               if (category is not null)
                   categoryNames[categoryId] = category.Name;
           }

           var items = transactions
               .Select(t => MapToResponse(t, t.CategoryId.HasValue ? categoryNames.GetValueOrDefault(t.CategoryId.Value) : null))
               .ToList();

           var result = new PagedResult<TransactionResponse>
           {
               Items = items,
               TotalCount = totalCount,
               Page = page,
               PageSize = pageSize
           };

           return Result<PagedResult<TransactionResponse>>.Success(result);
       }

       public async Task<Result<TransactionResponse>> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default)
       {
           var transaction = await transactionRepository.GetByUserAndIdAsync(userId, id, ct);
           if (transaction is null)
           {
               logger.LogWarning("Transaction {TransactionId} not found for user {UserId}", id, userId);
               return Result<TransactionResponse>.Failure(new(ApiErrorCode.TransactionNotFound, "Transaction not found."));
           }

           string? categoryName = null;
           if (transaction.CategoryId.HasValue)
           {
               var category = await categoryRepository.GetByIdAsync(transaction.CategoryId.Value, ct);
               categoryName = category?.Name;
           }

           return Result<TransactionResponse>.Success(MapToResponse(transaction, categoryName));
       }

       public async Task<Result<TransactionResponse>> CreateAsync(Guid userId, CreateTransactionRequest request, CancellationToken ct = default)
       {
           if (request.CategoryId.HasValue)
           {
               var categoryExists = await categoryRepository.ExistsByUserAndIdAsync(userId, request.CategoryId.Value, ct);
               if (!categoryExists)
               {
                   logger.LogWarning("Transaction creation failed: category {CategoryId} not found for user {UserId}", request.CategoryId, userId);
                   return Result<TransactionResponse>.Failure(new(ApiErrorCode.CategoryNotFound, "The specified category was not found."));
               }
           }

           var transaction = Transaction.Create(
               userId, request.Description, request.Amount, request.Type,
               request.Date, request.CategoryId, request.Notes);

           await transactionRepository.AddAsync(transaction, ct);
           await transactionRepository.SaveChangesAsync(ct);

           logger.LogInformation("Transaction {TransactionId} created for user {UserId}", transaction.Id, userId);

           string? categoryName = null;
           if (transaction.CategoryId.HasValue)
           {
               var category = await categoryRepository.GetByIdAsync(transaction.CategoryId.Value, ct);
               categoryName = category?.Name;
           }

           return Result<TransactionResponse>.Success(MapToResponse(transaction, categoryName));
       }

       public async Task<Result<TransactionResponse>> UpdateAsync(Guid userId, Guid id, UpdateTransactionRequest request, CancellationToken ct = default)
       {
           var transaction = await transactionRepository.GetByUserAndIdAsync(userId, id, ct);
           if (transaction is null)
           {
               logger.LogWarning("Transaction update failed: {TransactionId} not found for user {UserId}", id, userId);
               return Result<TransactionResponse>.Failure(new(ApiErrorCode.TransactionNotFound, "Transaction not found."));
           }

           if (request.CategoryId.HasValue)
           {
               var categoryExists = await categoryRepository.ExistsByUserAndIdAsync(userId, request.CategoryId.Value, ct);
               if (!categoryExists)
               {
                   logger.LogWarning("Transaction update failed: category {CategoryId} not found for user {UserId}", request.CategoryId, userId);
                   return Result<TransactionResponse>.Failure(new(ApiErrorCode.CategoryNotFound, "The specified category was not found."));
               }
           }

           transaction.Update(
               request.Description, request.Amount, request.Type,
               request.Date, request.CategoryId, request.Notes);

           await transactionRepository.SaveChangesAsync(ct);

           logger.LogInformation("Transaction {TransactionId} updated by user {UserId}", transaction.Id, userId);

           string? categoryName = null;
           if (transaction.CategoryId.HasValue)
           {
               var category = await categoryRepository.GetByIdAsync(transaction.CategoryId.Value, ct);
               categoryName = category?.Name;
           }

           return Result<TransactionResponse>.Success(MapToResponse(transaction, categoryName));
       }

       public async Task<Result<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
       {
           var transaction = await transactionRepository.GetByUserAndIdAsync(userId, id, ct);
           if (transaction is null)
           {
               logger.LogWarning("Transaction delete failed: {TransactionId} not found for user {UserId}", id, userId);
               return Result<bool>.Failure(new(ApiErrorCode.TransactionNotFound, "Transaction not found."));
           }

           await transactionRepository.DeleteAsync(transaction, ct);
           await transactionRepository.SaveChangesAsync(ct);

           logger.LogInformation("Transaction {TransactionId} deleted by user {UserId}", transaction.Id, userId);
           return Result<bool>.Success(true);
       }

       private static TransactionResponse MapToResponse(Transaction transaction, string? categoryName)
           => new(
               transaction.Id,
               transaction.Description,
               transaction.Amount,
               transaction.Type,
               transaction.Date,
               transaction.CategoryId,
               categoryName,
               transaction.Notes,
               transaction.CreatedAt,
               transaction.UpdatedAt);
   }
   ```

5. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- `ITransactionService` is in Application with no Infrastructure references
- `TransactionService` returns `Result<T>` for all methods
- Category ownership is validated when `CategoryId` is provided on create/update
- `GetAllAsync` returns `PagedResult<TransactionResponse>` with category names resolved
- Page and pageSize are clamped to safe ranges (1-100)
- `DeleteAsync` is added to both `ITransactionRepository` and `TransactionRepository`

---

### Task 20 — CategoryEndpoints Minimal API

**Status:** New

**Description:**
Create the `CategoryEndpoints` static class in the Api layer. All endpoints are scoped to the authenticated user via `ClaimsPrincipalExtensions.GetUserId()`. The group requires authorization. Apply `ValidationFilter<T>` to create and update endpoints. All responses are wrapped in `ApiResponse<T>` — following the pattern established in `AuthEnpoints.cs`.

**Steps:**

1. Create `backend/src/Modules/Finance/Api/Endpoints/CategoryEndpoints.cs`:
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
   using Personal.FinanceTracker.Shared.Models;

   namespace Personal.FinanceTracker.Finance.Api.Endpoints;

   public static class CategoryEndpoints
   {
       public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
       {
           var group = app.MapGroup("/api/categories")
               .WithTags("Categories")
               .RequireAuthorization();

           group.MapGet("/", GetAllAsync)
               .WithName("GetCategories")
               .WithDescription("Get all categories for the authenticated user.");

           group.MapGet("/{id:guid}", GetByIdAsync)
               .WithName("GetCategoryById")
               .WithDescription("Get a single category by ID.");

           group.MapPost("/", CreateAsync)
               .WithName("CreateCategory")
               .WithDescription("Create a new category.")
               .AddEndpointFilter<ValidationFilter<CreateCategoryRequest>>();

           group.MapPut("/{id:guid}", UpdateAsync)
               .WithName("UpdateCategory")
               .WithDescription("Update an existing category.")
               .AddEndpointFilter<ValidationFilter<UpdateCategoryRequest>>();

           group.MapDelete("/{id:guid}", DeleteAsync)
               .WithName("DeleteCategory")
               .WithDescription("Delete a category. Transactions referencing it will have their category set to null.");

           return app;
       }

       private static async Task<Ok<ApiResponse<IReadOnlyList<CategoryResponse>>>> GetAllAsync(
           ClaimsPrincipal user,
           ICategoryService categoryService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var result = await categoryService.GetAllAsync(userId, ct);

           return TypedResults.Ok(new ApiResponse<IReadOnlyList<CategoryResponse>>
           {
               IsOk = true,
               Data = result.Value,
               StatusCode = StatusCodes.Status200OK,
               CodeText = "OK"
           });
       }

       private static async Task<Results<Ok<ApiResponse<CategoryResponse>>, NotFound<ApiResponse<CategoryResponse>>>> GetByIdAsync(
           Guid id,
           ClaimsPrincipal user,
           ICategoryService categoryService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var result = await categoryService.GetByIdAsync(userId, id, ct);

           if (result.IsFailure)
               return TypedResults.NotFound(new ApiResponse<CategoryResponse>
               {
                   IsOk = false,
                   Error = new ApiError
                   {
                       Title = "Category Not Found",
                       Status = StatusCodes.Status404NotFound,
                       Detail = result.Error?.Description,
                   },
                   StatusCode = StatusCodes.Status404NotFound,
                   CodeText = "NOT_FOUND"
               });

           return TypedResults.Ok(new ApiResponse<CategoryResponse>
           {
               IsOk = true,
               Data = result.Value,
               StatusCode = StatusCodes.Status200OK,
               CodeText = "OK"
           });
       }

       private static async Task<Results<Created<ApiResponse<CategoryResponse>>, Conflict<ApiResponse<CategoryResponse>>>> CreateAsync(
           CreateCategoryRequest request,
           ClaimsPrincipal user,
           ICategoryService categoryService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var result = await categoryService.CreateAsync(userId, request, ct);

           if (result.IsFailure)
               return TypedResults.Conflict(new ApiResponse<CategoryResponse>
               {
                   IsOk = false,
                   Error = new ApiError
                   {
                       Title = "Category Creation Failed",
                       Status = StatusCodes.Status409Conflict,
                       Detail = result.Error?.Description,
                   },
                   StatusCode = StatusCodes.Status409Conflict,
                   CodeText = "CONFLICT"
               });

           return TypedResults.Created($"/api/categories/{result.Value!.Id}", new ApiResponse<CategoryResponse>
           {
               IsOk = true,
               Data = result.Value,
               StatusCode = StatusCodes.Status201Created,
               CodeText = "CREATED"
           });
       }

       private static async Task<Results<Ok<ApiResponse<CategoryResponse>>, NotFound<ApiResponse<CategoryResponse>>>> UpdateAsync(
           Guid id,
           UpdateCategoryRequest request,
           ClaimsPrincipal user,
           ICategoryService categoryService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var result = await categoryService.UpdateAsync(userId, id, request, ct);

           if (result.IsFailure)
               return TypedResults.NotFound(new ApiResponse<CategoryResponse>
               {
                   IsOk = false,
                   Error = new ApiError
                   {
                       Title = "Category Update Failed",
                       Status = StatusCodes.Status404NotFound,
                       Detail = result.Error?.Description,
                   },
                   StatusCode = StatusCodes.Status404NotFound,
                   CodeText = "NOT_FOUND"
               });

           return TypedResults.Ok(new ApiResponse<CategoryResponse>
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
           ICategoryService categoryService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var result = await categoryService.DeleteAsync(userId, id, ct);

           if (result.IsFailure)
               return TypedResults.NotFound(new ApiResponse<object>
               {
                   IsOk = false,
                   Error = new ApiError
                   {
                       Title = "Category Not Found",
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
- All endpoints use `TypedResults` (not `Results`)
- `RequireAuthorization()` is applied at group level
- `ValidationFilter<T>` is applied to create and update endpoints
- All responses are wrapped in `ApiResponse<T>` with `IsOk`, `Data`, `StatusCode`, `CodeText`
- No business logic in endpoint handlers — all delegated to `ICategoryService`
- `GetUserId()` called on every handler

---

### Task 21 — TransactionEndpoints Minimal API

**Status:** New

**Description:**
Create the `TransactionEndpoints` static class. The list endpoint uses `[AsParameters] TransactionQueryParams` for filter + pagination binding. All endpoints are authorized and wrapped in `ApiResponse<T>`.

**Steps:**

1. Create `backend/src/Modules/Finance/Api/Endpoints/TransactionEndpoints.cs`:
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
   using Personal.FinanceTracker.Shared.Models;

   namespace Personal.FinanceTracker.Finance.Api.Endpoints;

   public static class TransactionEndpoints
   {
       public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
       {
           var group = app.MapGroup("/api/transactions")
               .WithTags("Transactions")
               .RequireAuthorization();

           group.MapGet("/", GetAllAsync)
               .WithName("GetTransactions")
               .WithDescription("Get a paginated, filtered list of transactions for the authenticated user.");

           group.MapGet("/{id:guid}", GetByIdAsync)
               .WithName("GetTransactionById")
               .WithDescription("Get a single transaction by ID.");

           group.MapPost("/", CreateAsync)
               .WithName("CreateTransaction")
               .WithDescription("Create a new transaction.")
               .AddEndpointFilter<ValidationFilter<CreateTransactionRequest>>();

           group.MapPut("/{id:guid}", UpdateAsync)
               .WithName("UpdateTransaction")
               .WithDescription("Update an existing transaction.")
               .AddEndpointFilter<ValidationFilter<UpdateTransactionRequest>>();

           group.MapDelete("/{id:guid}", DeleteAsync)
               .WithName("DeleteTransaction")
               .WithDescription("Delete a transaction.");

           return app;
       }

       private static async Task<Ok<ApiResponse<PagedResult<TransactionResponse>>>> GetAllAsync(
           ClaimsPrincipal user,
           [AsParameters] TransactionQueryParams queryParams,
           ITransactionService transactionService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var result = await transactionService.GetAllAsync(userId, queryParams, ct);

           return TypedResults.Ok(new ApiResponse<PagedResult<TransactionResponse>>
           {
               IsOk = true,
               Data = result.Value,
               StatusCode = StatusCodes.Status200OK,
               CodeText = "OK"
           });
       }

       private static async Task<Results<Ok<ApiResponse<TransactionResponse>>, NotFound<ApiResponse<TransactionResponse>>>> GetByIdAsync(
           Guid id,
           ClaimsPrincipal user,
           ITransactionService transactionService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var result = await transactionService.GetByIdAsync(userId, id, ct);

           if (result.IsFailure)
               return TypedResults.NotFound(new ApiResponse<TransactionResponse>
               {
                   IsOk = false,
                   Error = new ApiError
                   {
                       Title = "Transaction Not Found",
                       Status = StatusCodes.Status404NotFound,
                       Detail = result.Error?.Description,
                   },
                   StatusCode = StatusCodes.Status404NotFound,
                   CodeText = "NOT_FOUND"
               });

           return TypedResults.Ok(new ApiResponse<TransactionResponse>
           {
               IsOk = true,
               Data = result.Value,
               StatusCode = StatusCodes.Status200OK,
               CodeText = "OK"
           });
       }

       private static async Task<Results<Created<ApiResponse<TransactionResponse>>, BadRequest<ApiResponse<TransactionResponse>>>> CreateAsync(
           CreateTransactionRequest request,
           ClaimsPrincipal user,
           ITransactionService transactionService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var result = await transactionService.CreateAsync(userId, request, ct);

           if (result.IsFailure)
               return TypedResults.BadRequest(new ApiResponse<TransactionResponse>
               {
                   IsOk = false,
                   Error = new ApiError
                   {
                       Title = "Transaction Creation Failed",
                       Status = StatusCodes.Status400BadRequest,
                       Detail = result.Error?.Description,
                   },
                   StatusCode = StatusCodes.Status400BadRequest,
                   CodeText = "BAD_REQUEST"
               });

           return TypedResults.Created($"/api/transactions/{result.Value!.Id}", new ApiResponse<TransactionResponse>
           {
               IsOk = true,
               Data = result.Value,
               StatusCode = StatusCodes.Status201Created,
               CodeText = "CREATED"
           });
       }

       private static async Task<Results<Ok<ApiResponse<TransactionResponse>>, NotFound<ApiResponse<TransactionResponse>>>> UpdateAsync(
           Guid id,
           UpdateTransactionRequest request,
           ClaimsPrincipal user,
           ITransactionService transactionService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var result = await transactionService.UpdateAsync(userId, id, request, ct);

           if (result.IsFailure)
               return TypedResults.NotFound(new ApiResponse<TransactionResponse>
               {
                   IsOk = false,
                   Error = new ApiError
                   {
                       Title = "Transaction Update Failed",
                       Status = StatusCodes.Status404NotFound,
                       Detail = result.Error?.Description,
                   },
                   StatusCode = StatusCodes.Status404NotFound,
                   CodeText = "NOT_FOUND"
               });

           return TypedResults.Ok(new ApiResponse<TransactionResponse>
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
           ITransactionService transactionService,
           CancellationToken ct)
       {
           var userId = user.GetUserId();
           var result = await transactionService.DeleteAsync(userId, id, ct);

           if (result.IsFailure)
               return TypedResults.NotFound(new ApiResponse<object>
               {
                   IsOk = false,
                   Error = new ApiError
                   {
                       Title = "Transaction Not Found",
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
- `[AsParameters]` is used on the list endpoint for query binding
- All endpoints use `TypedResults` and wrap responses in `ApiResponse<T>`
- `RequireAuthorization()` is applied at group level
- `ValidationFilter<T>` is applied to create and update endpoints
- `GetUserId()` called on every handler

---

### Task 22 — Register Finance Module in DependencyInjection and Program.cs

**Status:** New

**Description:**
Create the `DependencyInjection` static class for the Finance module (matching the Users module pattern) and wire it into `Program.cs`. This registers the `FinanceDbContext`, all repositories, services, validators, and maps all endpoints.

**Steps:**

1. Create `backend/src/Modules/Finance/DependencyInjection.cs`:
   ```csharp
   using FluentValidation;
   using Microsoft.AspNetCore.Routing;
   using Microsoft.EntityFrameworkCore;
   using Microsoft.Extensions.Configuration;
   using Microsoft.Extensions.DependencyInjection;
   using Personal.FinanceTracker.Finance.Api.Endpoints;
   using Personal.FinanceTracker.Finance.Application.Validators;
   using Personal.FinanceTracker.Finance.Domain.Interfaces;
   using Personal.FinanceTracker.Finance.Infrastructure.Data;
   using Personal.FinanceTracker.Finance.Infrastructure.Repositories;
   using Personal.FinanceTracker.Finance.Infrastructure.Services;
   using Personal.FinanceTracker.Finance.Application.Services;

   namespace Personal.FinanceTracker.Finance;

   public static class DependencyInjection
   {
       public static IServiceCollection AddFinanceModule(
           this IServiceCollection services,
           IConfiguration configuration)
       {
           services.AddDbContext<FinanceDbContext>(options =>
               options.UseNpgsql(
                   configuration.GetConnectionString("DefaultConnection"),
                   npgsqlOptions =>
                   {
                       npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "finances");
                       npgsqlOptions.EnableRetryOnFailure(
                           maxRetryCount: 3,
                           maxRetryDelay: TimeSpan.FromSeconds(10),
                           errorCodesToAdd: null);
                       npgsqlOptions.CommandTimeout(30);
                   }));

           services.AddScoped<ICategoryRepository, CategoryRepository>();
           services.AddScoped<ITransactionRepository, TransactionRepository>();

           services.AddScoped<ICategoryService, CategoryService>();
           services.AddScoped<ITransactionService, TransactionService>();

           services.AddValidatorsFromAssemblyContaining<CreateCategoryValidator>();

           return services;
       }

       public static IEndpointRouteBuilder MapFinanceEndpoints(this IEndpointRouteBuilder app)
       {
           app.MapCategoryEndpoints();
           app.MapTransactionEndpoints();
           return app;
       }
   }
   ```

2. Update `backend/src/Personal.FinanceTracker.Api/Program.cs` — replace the TODO comments (from Task 1):
   ```csharp
   // Replace:
   // TODO Sprint 2: builder.Services.AddFinanceModule(builder.Configuration);
   // With:
   builder.Services.AddFinanceModule(builder.Configuration);
   ```
   And:
   ```csharp
   // Replace:
   // TODO Sprint 2: app.MapFinanceEndpoints();
   // With:
   app.MapFinanceEndpoints();
   ```

3. Add the `using` directive for the Finance module at the top of `Program.cs`:
   ```csharp
   using Personal.FinanceTracker.Finance;
   ```

4. Run `dotnet build` — confirm 0 errors, 0 warnings.

**Success Criteria:**
- `DependencyInjection` class follows the same pattern as `Users.DependencyInjection`
- `FinanceDbContext` is registered with retry-on-failure and migrations history table in `finances` schema
- All repositories and services registered as `Scoped`
- Validators registered via `AddValidatorsFromAssemblyContaining<T>()`
- `Program.cs` has no stale TODO comments
- `dotnet build` passes with zero warnings

---

### Task 23 — Frontend: Type Definitions

**Status:** New

**Description:**
Add finance type definitions to `src/types/`. Mirror the backend DTOs exactly. Use `TransactionType` as a string literal union type (consistent with `verbatimModuleSyntax` — no `enum` keyword). Create `PagedResult<T>` in `src/types/http.ts` since it mirrors the shared backend type.

**Steps:**

1. Add `PagedResult<T>` to `frontend/src/types/http.ts` (append to existing file):
   ```typescript
   export interface PagedResult<T> {
     items: T[];
     totalCount: number;
     page: number;
     pageSize: number;
     totalPages: number;
     hasPreviousPage: boolean;
     hasNextPage: boolean;
   }
   ```

2. Create `frontend/src/types/finance.ts`:
   ```typescript
   export type TransactionType = 'Income' | 'Expense';

   export interface Category {
     id: string;
     name: string;
     icon: string | null;
     color: string | null;
     createdAt: string;
     updatedAt: string | null;
   }

   export interface Transaction {
     id: string;
     description: string;
     amount: number;
     type: TransactionType;
     date: string;
     categoryId: string | null;
     categoryName: string | null;
     notes: string | null;
     createdAt: string;
     updatedAt: string | null;
   }

   export interface CreateCategoryRequest {
     name: string;
     icon: string | null;
     color: string | null;
   }

   export interface UpdateCategoryRequest {
     name: string;
     icon: string | null;
     color: string | null;
   }

   export interface CreateTransactionRequest {
     description: string;
     amount: number;
     type: TransactionType;
     date: string;
     categoryId: string | null;
     notes: string | null;
   }

   export interface UpdateTransactionRequest {
     description: string;
     amount: number;
     type: TransactionType;
     date: string;
     categoryId: string | null;
     notes: string | null;
   }

   export interface TransactionFilters {
     page: number;
     pageSize: number;
     startDate?: string;
     endDate?: string;
     categoryId?: string;
     type?: TransactionType;
   }
   ```

3. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- Types mirror backend DTOs exactly
- `TransactionType` is a string literal union — not an enum
- `PagedResult<T>` is in `http.ts` (mirrors Shared project placement)
- `Transaction` includes `categoryName` for display convenience
- All nullable fields are `T | null`

---

### Task 24 — Frontend: API Service Modules

**Status:** New

**Description:**
Create `categoriesApi` and `transactionsApi` objects following the `authApi` pattern. All functions return `Promise<ApiResponse<T>>`. The `apiClient` from `src/api/client.ts` handles auth tokens automatically.

**Steps:**

1. Create `frontend/src/api/categories.ts`:
   ```typescript
   import { apiClient } from '@/api/client';
   import type { ApiResponse, PagedResult } from '@/types/http';
   import type { Category, CreateCategoryRequest, UpdateCategoryRequest } from '@/types/finance';

   export const categoriesApi = {
     getAll: (): Promise<ApiResponse<Category[]>> =>
       apiClient.get<Category[]>('/categories'),

     getById: (id: string): Promise<ApiResponse<Category>> =>
       apiClient.get<Category>(`/categories/${id}`),

     create: (data: CreateCategoryRequest): Promise<ApiResponse<Category>> =>
       apiClient.post<Category>('/categories', data),

     update: (id: string, data: UpdateCategoryRequest): Promise<ApiResponse<Category>> =>
       apiClient.put<Category>(`/categories/${id}`, data),

     delete: (id: string): Promise<ApiResponse<void>> =>
       apiClient.delete<void>(`/categories/${id}`),
   };
   ```

2. Create `frontend/src/api/transactions.ts`:
   ```typescript
   import { apiClient } from '@/api/client';
   import type { ApiResponse, PagedResult } from '@/types/http';
   import type { Transaction, CreateTransactionRequest, UpdateTransactionRequest, TransactionFilters } from '@/types/finance';

   function buildQueryString(filters: TransactionFilters): string {
     const params = new URLSearchParams();
     params.set('page', String(filters.page));
     params.set('pageSize', String(filters.pageSize));

     if (filters.startDate) params.set('startDate', filters.startDate);
     if (filters.endDate) params.set('endDate', filters.endDate);
     if (filters.categoryId) params.set('categoryId', filters.categoryId);
     if (filters.type) params.set('type', filters.type);

     return params.toString();
   }

   export const transactionsApi = {
     getAll: (filters: TransactionFilters): Promise<ApiResponse<PagedResult<Transaction>>> =>
       apiClient.get<PagedResult<Transaction>>(`/transactions?${buildQueryString(filters)}`),

     getById: (id: string): Promise<ApiResponse<Transaction>> =>
       apiClient.get<Transaction>(`/transactions/${id}`),

     create: (data: CreateTransactionRequest): Promise<ApiResponse<Transaction>> =>
       apiClient.post<Transaction>('/transactions', data),

     update: (id: string, data: UpdateTransactionRequest): Promise<ApiResponse<Transaction>> =>
       apiClient.put<Transaction>(`/transactions/${id}`, data),

     delete: (id: string): Promise<ApiResponse<void>> =>
       apiClient.delete<void>(`/transactions/${id}`),
   };
   ```

3. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- `categoriesApi` and `transactionsApi` follow the same shape as `authApi`
- All functions typed with request/response types from `src/types/`
- `buildQueryString` is a private helper that only includes non-undefined filters
- No `any` types

---

### Task 25 — Frontend: Custom Hooks

**Status:** New

**Description:**
Create TanStack Query hooks for categories and transactions. All hooks follow the query key factory pattern. Mutations invalidate the appropriate list keys on success. Co-locate hooks in feature folders.

**Steps:**

1. Create `frontend/src/features/categories/hooks/useCategories.ts`:
   ```typescript
   import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
   import { categoriesApi } from '@/api/categories';
   import type { CreateCategoryRequest, UpdateCategoryRequest } from '@/types/finance';

   export const categoryKeys = {
     all: ['categories'] as const,
     lists: () => [...categoryKeys.all, 'list'] as const,
     details: () => [...categoryKeys.all, 'detail'] as const,
     detail: (id: string) => [...categoryKeys.all, 'detail', id] as const,
   };

   export function useCategories() {
     return useQuery({
       queryKey: categoryKeys.lists(),
       queryFn: () => categoriesApi.getAll(),
       staleTime: 1000 * 60 * 5,
     });
   }

   export function useCreateCategory() {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: (data: CreateCategoryRequest) => categoriesApi.create(data),
       onSuccess: () => {
         void queryClient.invalidateQueries({ queryKey: categoryKeys.lists() });
       },
     });
   }

   export function useUpdateCategory() {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: ({ id, data }: { id: string; data: UpdateCategoryRequest }) =>
         categoriesApi.update(id, data),
       onSuccess: () => {
         void queryClient.invalidateQueries({ queryKey: categoryKeys.lists() });
       },
     });
   }

   export function useDeleteCategory() {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: (id: string) => categoriesApi.delete(id),
       onSuccess: () => {
         void queryClient.invalidateQueries({ queryKey: categoryKeys.lists() });
       },
     });
   }
   ```

2. Create `frontend/src/features/transactions/hooks/useTransactions.ts`:
   ```typescript
   import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
   import { transactionsApi } from '@/api/transactions';
   import type { CreateTransactionRequest, UpdateTransactionRequest, TransactionFilters } from '@/types/finance';

   export const transactionKeys = {
     all: ['transactions'] as const,
     lists: () => [...transactionKeys.all, 'list'] as const,
     list: (filters: TransactionFilters) => [...transactionKeys.lists(), filters] as const,
     details: () => [...transactionKeys.all, 'detail'] as const,
     detail: (id: string) => [...transactionKeys.all, 'detail', id] as const,
   };

   export function useTransactions(filters: TransactionFilters) {
     return useQuery({
       queryKey: transactionKeys.list(filters),
       queryFn: () => transactionsApi.getAll(filters),
       staleTime: 1000 * 60 * 5,
     });
   }

   export function useCreateTransaction() {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: (data: CreateTransactionRequest) => transactionsApi.create(data),
       onSuccess: () => {
         void queryClient.invalidateQueries({ queryKey: transactionKeys.lists() });
       },
     });
   }

   export function useUpdateTransaction() {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: ({ id, data }: { id: string; data: UpdateTransactionRequest }) =>
         transactionsApi.update(id, data),
       onSuccess: () => {
         void queryClient.invalidateQueries({ queryKey: transactionKeys.lists() });
       },
     });
   }

   export function useDeleteTransaction() {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: (id: string) => transactionsApi.delete(id),
       onSuccess: () => {
         void queryClient.invalidateQueries({ queryKey: transactionKeys.lists() });
       },
     });
   }
   ```

3. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- `categoryKeys` and `transactionKeys` factories used consistently across all hooks
- `transactionKeys.list(filters)` includes the filters in the key — refetches when filters change
- `staleTime` is 5 minutes for both lists (matching the global default in `main.tsx`)
- Mutations invalidate list keys on success
- No TanStack Query calls outside of these hook files

---

### Task 26 — Frontend: Category Components and Page

**Status:** New

**Description:**
Create `CategoryForm`, `CategoryList`, and `CategoriesPage`. The form uses Zod + React Hook Form. The page owns the data fetching (via `useCategories`), the create/edit modal state, and the delete confirmation flow. Follow the Tailwind styling patterns established in `LoginPage`.

**Steps:**

1. Create `frontend/src/features/categories/schemas.ts`:
   ```typescript
   import { z } from 'zod';

   export const categorySchema = z.object({
     name: z
       .string()
       .min(1, 'Category name is required.')
       .max(100, 'Category name cannot exceed 100 characters.'),
     icon: z
       .string()
       .max(50, 'Icon cannot exceed 50 characters.')
       .optional()
       .or(z.literal('')),
     color: z
       .string()
       .max(20, 'Color cannot exceed 20 characters.')
       .optional()
       .or(z.literal('')),
   });

   export type CategoryFormData = z.infer<typeof categorySchema>;
   ```

2. Create `frontend/src/features/categories/components/CategoryForm.tsx`:
   ```typescript
   import { zodResolver } from '@hookform/resolvers/zod';
   import { useForm } from 'react-hook-form';
   import { categorySchema } from '@/features/categories/schemas';
   import type { CategoryFormData } from '@/features/categories/schemas';
   import type { Category } from '@/types/finance';

   interface CategoryFormProps {
     defaultValues?: Partial<CategoryFormData>;
     onSubmit: (data: CategoryFormData) => void;
     isSubmitting: boolean;
     submitLabel?: string;
   }

   export function CategoryForm({
     defaultValues,
     onSubmit,
     isSubmitting,
     submitLabel = 'Save Category',
   }: CategoryFormProps) {
     const {
       register,
       handleSubmit,
       formState: { errors },
     } = useForm<CategoryFormData>({
       resolver: zodResolver(categorySchema),
       defaultValues: {
         name: '',
         icon: '',
         color: '',
         ...defaultValues,
       },
     });

     return (
       <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
         <div>
           <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
             Category Name
           </label>
           <input
             id="name"
             type="text"
             {...register('name')}
             className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
             placeholder="e.g., Groceries"
           />
           {errors.name && (
             <p className="mt-1 text-xs text-red-600">{errors.name.message}</p>
           )}
         </div>

         <div>
           <label htmlFor="icon" className="block text-sm font-medium text-gray-700 mb-1">
             Icon (optional)
           </label>
           <input
             id="icon"
             type="text"
             {...register('icon')}
             className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
             placeholder="e.g., shopping-cart"
           />
           {errors.icon && (
             <p className="mt-1 text-xs text-red-600">{errors.icon.message}</p>
           )}
         </div>

         <div>
           <label htmlFor="color" className="block text-sm font-medium text-gray-700 mb-1">
             Color (optional)
           </label>
           <input
             id="color"
             type="text"
             {...register('color')}
             className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
             placeholder="e.g., #FF5733"
           />
           {errors.color && (
             <p className="mt-1 text-xs text-red-600">{errors.color.message}</p>
           )}
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

3. Create `frontend/src/features/categories/components/CategoryList.tsx`:
   ```typescript
   import { Pencil, Trash2 } from 'lucide-react';
   import type { Category } from '@/types/finance';

   interface CategoryListProps {
     categories: Category[];
     isLoading: boolean;
     error: Error | null;
     onEdit: (category: Category) => void;
     onDelete: (category: Category) => void;
   }

   export function CategoryList({ categories, isLoading, error, onEdit, onDelete }: CategoryListProps) {
     if (isLoading) {
       return (
         <div className="space-y-2">
           {[1, 2, 3].map(i => (
             <div key={i} className="h-16 rounded-lg bg-gray-100 animate-pulse" />
           ))}
         </div>
       );
     }

     if (error) {
       return (
         <div className="rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
           Failed to load categories. Please try again.
         </div>
       );
     }

     if (categories.length === 0) {
       return (
         <div className="text-center py-12 text-gray-500 text-sm">
           No categories yet. Create your first category to start organizing transactions.
         </div>
       );
     }

     return (
       <div className="space-y-2">
         {categories.map(category => (
           <div
             key={category.id}
             className="flex items-center justify-between rounded-lg border border-gray-200 bg-white px-4 py-3"
           >
             <div className="flex items-center gap-3">
               {category.color && (
                 <span
                   className="inline-block h-3 w-3 rounded-full"
                   style={{ backgroundColor: category.color }}
                 />
               )}
               <div>
                 <p className="text-sm font-medium text-gray-900">{category.name}</p>
                 {category.icon && (
                   <p className="text-xs text-gray-500">{category.icon}</p>
                 )}
               </div>
             </div>
             <div className="flex items-center gap-2">
               <button
                 onClick={() => onEdit(category)}
                 className="rounded-md p-2 text-gray-400 hover:text-indigo-600 hover:bg-gray-50 transition-colors"
                 aria-label={`Edit ${category.name}`}
               >
                 <Pencil className="h-4 w-4" />
               </button>
               <button
                 onClick={() => onDelete(category)}
                 className="rounded-md p-2 text-gray-400 hover:text-red-600 hover:bg-gray-50 transition-colors"
                 aria-label={`Delete ${category.name}`}
               >
                 <Trash2 className="h-4 w-4" />
               </button>
             </div>
           </div>
         ))}
       </div>
     );
   }
   ```

4. Create `frontend/src/features/categories/pages/CategoriesPage.tsx`:
   ```typescript
   import { useState } from 'react';
   import { Plus, X } from 'lucide-react';
   import { useCategories, useCreateCategory, useUpdateCategory, useDeleteCategory } from '@/features/categories/hooks/useCategories';
   import { CategoryForm } from '@/features/categories/components/CategoryForm';
   import { CategoryList } from '@/features/categories/components/CategoryList';
   import type { Category } from '@/types/finance';
   import type { CategoryFormData } from '@/features/categories/schemas';
   import { setDocumentTitle } from '@/utils/documentTitle';

   export function CategoriesPage() {
     setDocumentTitle('Categories');
     const { data: response, isLoading, error } = useCategories();
     const createMutation = useCreateCategory();
     const updateMutation = useUpdateCategory();
     const deleteMutation = useDeleteCategory();

     const [isModalOpen, setIsModalOpen] = useState(false);
     const [editingCategory, setEditingCategory] = useState<Category | null>(null);
     const [deleteTarget, setDeleteTarget] = useState<Category | null>(null);

     const categories = response?.data ?? [];

     function handleOpenCreate() {
       setEditingCategory(null);
       setIsModalOpen(true);
     }

     function handleOpenEdit(category: Category) {
       setEditingCategory(category);
       setIsModalOpen(true);
     }

     function handleCloseModal() {
       setIsModalOpen(false);
       setEditingCategory(null);
     }

     async function handleSubmit(data: CategoryFormData) {
       if (editingCategory) {
         await updateMutation.mutateAsync({
           id: editingCategory.id,
           data: {
             name: data.name,
             icon: data.icon ?? null,
             color: data.color ?? null,
           },
         });
       } else {
         await createMutation.mutateAsync({
           name: data.name,
           icon: data.icon ?? null,
           color: data.color ?? null,
         });
       }
       handleCloseModal();
     }

     async function handleConfirmDelete() {
       if (!deleteTarget) return;
       await deleteMutation.mutateAsync(deleteTarget.id);
       setDeleteTarget(null);
     }

     const isSubmitting = createMutation.isPending || updateMutation.isPending;

     return (
       <div className="space-y-6">
         <div className="flex items-center justify-between">
           <h1 className="text-2xl font-bold text-gray-900">Categories</h1>
           <button
             onClick={handleOpenCreate}
             className="inline-flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 transition-colors"
           >
             <Plus className="h-4 w-4" />
             Add Category
           </button>
         </div>

         <CategoryList
           categories={categories}
           isLoading={isLoading}
           error={error}
           onEdit={handleOpenEdit}
           onDelete={setDeleteTarget}
         />

         {isModalOpen && (
           <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
             <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl">
               <div className="mb-4 flex items-center justify-between">
                 <h2 className="text-lg font-semibold text-gray-900">
                   {editingCategory ? 'Edit Category' : 'New Category'}
                 </h2>
                 <button
                   onClick={handleCloseModal}
                   className="rounded-md p-1 text-gray-400 hover:text-gray-600"
                   aria-label="Close"
                 >
                   <X className="h-5 w-5" />
                 </button>
               </div>
               <CategoryForm
                 defaultValues={
                   editingCategory
                     ? {
                         name: editingCategory.name,
                         icon: editingCategory.icon ?? '',
                         color: editingCategory.color ?? '',
                       }
                     : undefined
                 }
                 onSubmit={handleSubmit}
                 isSubmitting={isSubmitting}
                 submitLabel={editingCategory ? 'Update Category' : 'Create Category'}
               />
             </div>
           </div>
         )}

         {deleteTarget && (
           <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
             <div className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-xl">
               <h2 className="text-lg font-semibold text-gray-900">Delete Category</h2>
               <p className="mt-2 text-sm text-gray-600">
                 Are you sure you want to delete "{deleteTarget.name}"? Transactions referencing
                 this category will become uncategorized.
               </p>
               <div className="mt-6 flex gap-3">
                 <button
                   onClick={() => setDeleteTarget(null)}
                   className="flex-1 rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors"
                 >
                   Cancel
                 </button>
                 <button
                   onClick={handleConfirmDelete}
                   disabled={deleteMutation.isPending}
                   className="flex-1 rounded-lg bg-red-600 px-4 py-2 text-sm font-semibold text-white hover:bg-red-700 disabled:opacity-50 transition-colors"
                 >
                   {deleteMutation.isPending ? 'Deleting…' : 'Delete'}
                 </button>
               </div>
             </div>
           </div>
         )}
       </div>
     );
   }
   ```

5. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- `CategoryForm` uses Zod + React Hook Form with inline error messages
- `CategoryList` handles loading, error, and empty states
- `CategoriesPage` owns data fetching via `useCategories` — no direct API calls
- Create and edit flows use the same `CategoryForm` with `defaultValues`
- Delete flow shows a confirmation dialog before calling the mutation
- All components use Tailwind CSS following the design system from `LoginPage`
- Lucide React icons used throughout

---

### Task 27 — Frontend: Transaction Components and Page

**Status:** New

**Description:**
Create `TransactionForm`, `TransactionList`, and `TransactionsPage`. The form includes a category selector populated from `useCategories`. The page owns pagination state, filter state, data fetching, and CRUD modal flows.

**Steps:**

1. Create `frontend/src/features/transactions/schemas.ts`:
   ```typescript
   import { z } from 'zod';

   export const transactionSchema = z.object({
     description: z
       .string()
       .min(1, 'Description is required.')
       .max(500, 'Description cannot exceed 500 characters.'),
     amount: z.coerce.number().positive('Amount must be greater than zero.'),
     type: z.enum(['Income', 'Expense'] as const),
     date: z.string().min(1, 'Date is required.'),
     categoryId: z.string().optional().or(z.literal('')),
     notes: z
       .string()
       .max(2000, 'Notes cannot exceed 2000 characters.')
       .optional()
       .or(z.literal('')),
   });

   export type TransactionFormData = z.infer<typeof transactionSchema>;
   ```

2. Create `frontend/src/features/transactions/components/TransactionForm.tsx`:
   ```typescript
   import { zodResolver } from '@hookform/resolvers/zod';
   import { useForm } from 'react-hook-form';
   import { transactionSchema } from '@/features/transactions/schemas';
   import type { TransactionFormData } from '@/features/transactions/schemas';
   import type { TransactionType } from '@/types/finance';
   import { useCategories } from '@/features/categories/hooks/useCategories';

   interface TransactionFormProps {
     defaultValues?: Partial<TransactionFormData>;
     onSubmit: (data: TransactionFormData) => void;
     isSubmitting: boolean;
     submitLabel?: string;
   }

   const TYPE_OPTIONS: { value: TransactionType; label: string }[] = [
     { value: 'Income', label: 'Income' },
     { value: 'Expense', label: 'Expense' },
   ];

   export function TransactionForm({
     defaultValues,
     onSubmit,
     isSubmitting,
     submitLabel = 'Save Transaction',
   }: TransactionFormProps) {
     const { data: categoriesResponse } = useCategories();
     const categories = categoriesResponse?.data ?? [];

     const {
       register,
       handleSubmit,
       formState: { errors },
     } = useForm<TransactionFormData>({
       resolver: zodResolver(transactionSchema),
       defaultValues: {
         description: '',
         amount: 0,
         type: 'Expense',
         date: new Date().toISOString().split('T')[0],
         categoryId: '',
         notes: '',
         ...defaultValues,
       },
     });

     return (
       <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
         <div>
           <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
             Description
           </label>
           <input
             id="description"
             type="text"
             {...register('description')}
             className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
             placeholder="e.g., Grocery shopping"
           />
           {errors.description && (
             <p className="mt-1 text-xs text-red-600">{errors.description.message}</p>
           )}
         </div>

         <div className="grid grid-cols-2 gap-4">
           <div>
             <label htmlFor="amount" className="block text-sm font-medium text-gray-700 mb-1">
               Amount
             </label>
             <input
               id="amount"
               type="number"
               step="0.01"
               {...register('amount')}
               className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
               placeholder="0.00"
             />
             {errors.amount && (
               <p className="mt-1 text-xs text-red-600">{errors.amount.message}</p>
             )}
           </div>

           <div>
             <label htmlFor="type" className="block text-sm font-medium text-gray-700 mb-1">
               Type
             </label>
             <select
               id="type"
               {...register('type')}
               className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
             >
               {TYPE_OPTIONS.map(opt => (
                 <option key={opt.value} value={opt.value}>{opt.label}</option>
               ))}
             </select>
             {errors.type && (
               <p className="mt-1 text-xs text-red-600">{errors.type.message}</p>
             )}
           </div>
         </div>

         <div className="grid grid-cols-2 gap-4">
           <div>
             <label htmlFor="date" className="block text-sm font-medium text-gray-700 mb-1">
               Date
             </label>
             <input
               id="date"
               type="date"
               {...register('date')}
               className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
             />
             {errors.date && (
               <p className="mt-1 text-xs text-red-600">{errors.date.message}</p>
             )}
           </div>

           <div>
             <label htmlFor="categoryId" className="block text-sm font-medium text-gray-700 mb-1">
               Category
             </label>
             <select
               id="categoryId"
               {...register('categoryId')}
               className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
             >
               <option value="">Uncategorized</option>
               {categories.map(c => (
                 <option key={c.id} value={c.id}>{c.name}</option>
               ))}
             </select>
           </div>
         </div>

         <div>
           <label htmlFor="notes" className="block text-sm font-medium text-gray-700 mb-1">
             Notes (optional)
           </label>
           <textarea
             id="notes"
             rows={3}
             {...register('notes')}
             className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
             placeholder="Additional notes…"
           />
           {errors.notes && (
             <p className="mt-1 text-xs text-red-600">{errors.notes.message}</p>
           )}
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

3. Create `frontend/src/features/transactions/components/TransactionList.tsx`:
   ```typescript
   import { Pencil, Trash2, ArrowDownCircle, ArrowUpCircle } from 'lucide-react';
   import type { Transaction } from '@/types/finance';

   interface TransactionListProps {
     transactions: Transaction[];
     isLoading: boolean;
     error: Error | null;
     onEdit: (transaction: Transaction) => void;
     onDelete: (transaction: Transaction) => void;
   }

   function formatCurrency(amount: number): string {
     return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);
   }

   function formatDate(dateString: string): string {
     return new Date(dateString).toLocaleDateString('en-US', {
       year: 'numeric',
       month: 'short',
       day: 'numeric',
     });
   }

   export function TransactionList({ transactions, isLoading, error, onEdit, onDelete }: TransactionListProps) {
     if (isLoading) {
       return (
         <div className="space-y-2">
           {[1, 2, 3, 4, 5].map(i => (
             <div key={i} className="h-20 rounded-lg bg-gray-100 animate-pulse" />
           ))}
         </div>
       );
     }

     if (error) {
       return (
         <div className="rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
           Failed to load transactions. Please try again.
         </div>
       );
     }

     if (transactions.length === 0) {
       return (
         <div className="text-center py-12 text-gray-500 text-sm">
           No transactions found. Create your first transaction to start tracking your finances.
         </div>
       );
     }

     return (
       <div className="space-y-2">
         {transactions.map(transaction => (
           <div
             key={transaction.id}
             className="flex items-center justify-between rounded-lg border border-gray-200 bg-white px-4 py-3"
           >
             <div className="flex items-center gap-3 min-w-0 flex-1">
               {transaction.type === 'Income' ? (
                 <ArrowUpCircle className="h-5 w-5 flex-shrink-0 text-green-600" />
               ) : (
                 <ArrowDownCircle className="h-5 w-5 flex-shrink-0 text-red-600" />
               )}
               <div className="min-w-0">
                 <p className="truncate text-sm font-medium text-gray-900">
                   {transaction.description}
                 </p>
                 <p className="text-xs text-gray-500">
                   {formatDate(transaction.date)}
                   {transaction.categoryName && ` · ${transaction.categoryName}`}
                 </p>
               </div>
             </div>
             <div className="flex items-center gap-3 flex-shrink-0">
               <span
                 className={`text-sm font-semibold ${
                   transaction.type === 'Income' ? 'text-green-600' : 'text-red-600'
                 }`}
               >
                 {transaction.type === 'Income' ? '+' : '-'}
                 {formatCurrency(transaction.amount)}
               </span>
               <button
                 onClick={() => onEdit(transaction)}
                 className="rounded-md p-2 text-gray-400 hover:text-indigo-600 hover:bg-gray-50 transition-colors"
                 aria-label={`Edit ${transaction.description}`}
               >
                 <Pencil className="h-4 w-4" />
               </button>
               <button
                 onClick={() => onDelete(transaction)}
                 className="rounded-md p-2 text-gray-400 hover:text-red-600 hover:bg-gray-50 transition-colors"
                 aria-label={`Delete ${transaction.description}`}
               >
                 <Trash2 className="h-4 w-4" />
               </button>
             </div>
           </div>
         ))}
       </div>
     );
   }
   ```

4. Create `frontend/src/features/transactions/pages/TransactionsPage.tsx`:
   ```typescript
   import { useState } from 'react';
   import { Plus, X, ChevronLeft, ChevronRight } from 'lucide-react';
   import {
     useTransactions,
     useCreateTransaction,
     useUpdateTransaction,
     useDeleteTransaction,
   } from '@/features/transactions/hooks/useTransactions';
   import { TransactionForm } from '@/features/transactions/components/TransactionForm';
   import { TransactionList } from '@/features/transactions/components/TransactionList';
   import type { Transaction, TransactionFilters } from '@/types/finance';
   import type { TransactionFormData } from '@/features/transactions/schemas';
   import { setDocumentTitle } from '@/utils/documentTitle';

   const DEFAULT_FILTERS: TransactionFilters = {
     page: 1,
     pageSize: 20,
   };

   export function TransactionsPage() {
     setDocumentTitle('Transactions');
     const [filters, setFilters] = useState<TransactionFilters>(DEFAULT_FILTERS);
     const { data: response, isLoading, error } = useTransactions(filters);
     const createMutation = useCreateTransaction();
     const updateMutation = useUpdateTransaction();
     const deleteMutation = useDeleteTransaction();

     const [isModalOpen, setIsModalOpen] = useState(false);
     const [editingTransaction, setEditingTransaction] = useState<Transaction | null>(null);
     const [deleteTarget, setDeleteTarget] = useState<Transaction | null>(null);

     const pagedData = response?.data;
     const transactions = pagedData?.items ?? [];
     const totalPages = pagedData?.totalPages ?? 0;
     const currentPage = pagedData?.page ?? 1;

     function handleOpenCreate() {
       setEditingTransaction(null);
       setIsModalOpen(true);
     }

     function handleOpenEdit(transaction: Transaction) {
       setEditingTransaction(transaction);
       setIsModalOpen(true);
     }

     function handleCloseModal() {
       setIsModalOpen(false);
       setEditingTransaction(null);
     }

     async function handleSubmit(data: TransactionFormData) {
       const payload = {
         description: data.description,
         amount: data.amount,
         type: data.type,
         date: data.date,
         categoryId: data.categoryId || null,
         notes: data.notes || null,
       };

       if (editingTransaction) {
         await updateMutation.mutateAsync({ id: editingTransaction.id, data: payload });
       } else {
         await createMutation.mutateAsync(payload);
       }
       handleCloseModal();
     }

     async function handleConfirmDelete() {
       if (!deleteTarget) return;
       await deleteMutation.mutateAsync(deleteTarget.id);
       setDeleteTarget(null);
     }

     function handlePageChange(newPage: number) {
       setFilters(prev => ({ ...prev, page: newPage }));
     }

     const isSubmitting = createMutation.isPending || updateMutation.isPending;

     return (
       <div className="space-y-6">
         <div className="flex items-center justify-between">
           <h1 className="text-2xl font-bold text-gray-900">Transactions</h1>
           <button
             onClick={handleOpenCreate}
             className="inline-flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 transition-colors"
           >
             <Plus className="h-4 w-4" />
             Add Transaction
           </button>
         </div>

         <TransactionList
           transactions={transactions}
           isLoading={isLoading}
           error={error}
           onEdit={handleOpenEdit}
           onDelete={setDeleteTarget}
         />

         {totalPages > 1 && (
           <div className="flex items-center justify-center gap-4">
             <button
               onClick={() => handlePageChange(currentPage - 1)}
               disabled={currentPage <= 1}
               className="rounded-md p-2 text-gray-400 hover:text-indigo-600 disabled:opacity-30 disabled:cursor-not-allowed"
               aria-label="Previous page"
             >
               <ChevronLeft className="h-5 w-5" />
             </button>
             <span className="text-sm text-gray-600">
               Page {currentPage} of {totalPages}
             </span>
             <button
               onClick={() => handlePageChange(currentPage + 1)}
               disabled={currentPage >= totalPages}
               className="rounded-md p-2 text-gray-400 hover:text-indigo-600 disabled:opacity-30 disabled:cursor-not-allowed"
               aria-label="Next page"
             >
               <ChevronRight className="h-5 w-5" />
             </button>
           </div>
         )}

         {isModalOpen && (
           <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
             <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl max-h-[90dvh] overflow-y-auto">
               <div className="mb-4 flex items-center justify-between">
                 <h2 className="text-lg font-semibold text-gray-900">
                   {editingTransaction ? 'Edit Transaction' : 'New Transaction'}
                 </h2>
                 <button
                   onClick={handleCloseModal}
                   className="rounded-md p-1 text-gray-400 hover:text-gray-600"
                   aria-label="Close"
                 >
                   <X className="h-5 w-5" />
                 </button>
               </div>
               <TransactionForm
                 defaultValues={
                   editingTransaction
                     ? {
                         description: editingTransaction.description,
                         amount: editingTransaction.amount,
                         type: editingTransaction.type,
                         date: editingTransaction.date.split('T')[0],
                         categoryId: editingTransaction.categoryId ?? '',
                         notes: editingTransaction.notes ?? '',
                       }
                     : undefined
                 }
                 onSubmit={handleSubmit}
                 isSubmitting={isSubmitting}
                 submitLabel={editingTransaction ? 'Update Transaction' : 'Create Transaction'}
               />
             </div>
           </div>
         )}

         {deleteTarget && (
           <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
             <div className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-xl">
               <h2 className="text-lg font-semibold text-gray-900">Delete Transaction</h2>
               <p className="mt-2 text-sm text-gray-600">
                 Are you sure you want to delete "{deleteTarget.description}"?
               </p>
               <div className="mt-6 flex gap-3">
                 <button
                   onClick={() => setDeleteTarget(null)}
                   className="flex-1 rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors"
                 >
                   Cancel
                 </button>
                 <button
                   onClick={handleConfirmDelete}
                   disabled={deleteMutation.isPending}
                   className="flex-1 rounded-lg bg-red-600 px-4 py-2 text-sm font-semibold text-white hover:bg-red-700 disabled:opacity-50 transition-colors"
                 >
                   {deleteMutation.isPending ? 'Deleting…' : 'Delete'}
                 </button>
               </div>
             </div>
           </div>
         )}
       </div>
     );
   }
   ```

5. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- `TransactionForm` uses Zod + React Hook Form with a category dropdown populated from `useCategories`
- `TransactionList` displays income/expense icons, formatted currency, date, and category name
- `TransactionsPage` owns pagination state and passes filters to `useTransactions`
- Create and edit flows use the same `TransactionForm` with `defaultValues`
- Delete flow shows a confirmation dialog
- Pagination controls appear when `totalPages > 1`
- All components use Tailwind CSS and Lucide React icons

---

### Task 28 — Frontend: Router Wire-up

**Status:** New

**Description:**
Replace the placeholder `<div>` elements for `/transactions` and `/categories` routes in `src/routes/index.tsx` with the real page components.

**Steps:**

1. Update `frontend/src/routes/index.tsx` — add imports and replace placeholders:
   ```typescript
   import { createBrowserRouter } from "react-router-dom";
   import { MainLayout } from "@/components/layout/MainLayout";
   import { ProtectedRoute } from "@/features/auth/ProtectedRoute";
   import { NotFoundPage } from "@/pages/NotFoundPage";
   import { LoginPage } from "@/features/auth/LoginPage";
   import { RegisterPage } from "@/features/auth/RegisterPage";
   import { CategoriesPage } from "@/features/categories/pages/CategoriesPage";
   import { TransactionsPage } from "@/features/transactions/pages/TransactionsPage";

   const router = createBrowserRouter([
     {
       path: "/login",
       element: <LoginPage />,
     },
     {
       path: "/register",
       element: <RegisterPage />,
     },
     {
       element: <ProtectedRoute />,
       children: [
         {
           element: <MainLayout />,
           children: [
             {
               index: true,
               element: (
                 <div className="text-gray-500">
                   Dashboard — coming in Sprint 4
                 </div>
               ),
             },
             {
               path: "transactions",
               element: <TransactionsPage />,
             },
             {
               path: "categories",
               element: <CategoriesPage />,
             },
             {
               path: "budgets",
               element: (
                 <div className="text-gray-500">Budgets — coming in Sprint 3</div>
               ),
             },
             {
               path: "reports",
               element: (
                 <div className="text-gray-500">Reports — coming in Sprint 4</div>
               ),
             },
             { path: "*", element: <NotFoundPage /> },
           ],
         },
       ],
     },
   ]);
   export default router;
   ```

2. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- `/transactions` route renders the real `TransactionsPage` (not the placeholder)
- `/categories` route renders the real `CategoriesPage` (not the placeholder)
- Dashboard, Budgets, and Reports routes remain as placeholders
- `npm run build` passes with zero TypeScript errors

---

## Success Criteria — Sprint Complete

- [ ] `dotnet build` passes with 0 errors and 0 warnings
- [ ] `npm run build` passes with 0 TypeScript errors
- [ ] `finances.categories` and `finances.transactions` tables exist in the database
- [ ] `GET /api/categories` returns all categories for the authenticated user
- [ ] `POST /api/categories` creates a category; returns 409 if the name already exists
- [ ] `PUT /api/categories/{id}` updates a category; returns 404 if not owned by the user
- [ ] `DELETE /api/categories/{id}` deletes a category; transactions referencing it become uncategorized
- [ ] `GET /api/transactions?page=1&pageSize=20` returns a paginated list of transactions
- [ ] `GET /api/transactions?type=Expense&categoryId={guid}` returns filtered transactions
- [ ] `POST /api/transactions` creates a transaction; returns 400 if the category does not belong to the user
- [ ] `PUT /api/transactions/{id}` updates a transaction; returns 404 if not owned by the user
- [ ] `DELETE /api/transactions/{id}` deletes a transaction; returns 404 if not owned by the user
- [ ] `ITransactionRepository.GetTotalExpensesByCategoryAsync` is implemented for Sprint 3 forward compatibility
- [ ] Frontend `/categories` page is functional end-to-end (create, view, update, delete)
- [ ] Frontend `/transactions` page is functional end-to-end (create, view, update, delete, pagination)
- [ ] All endpoints return `ApiResponse<T>` envelope with `IsOk`, `Data`, `StatusCode`, `CodeText`
- [ ] All services return `Result<T>` — no raw exceptions for expected business failures
- [ ] No stale `// TODO Sprint 2:` comments in `Program.cs`
- [ ] `AuthEnpoints.cs` filename typo corrected to `AuthEndpoints.cs`

---

*Last updated: 22/06/2026*
