# Sprint 1 — Users Module / Authentication

**Duration:** 19/05/2026 — 30/05/2026
**Status:** Done
**Overview:** [SPRINTS-OVERVIEW.md](./SPRINTS-OVERVIEW.md)

---

## Overview

Sprint 1 builds the Users module end-to-end: domain entity, repository, JWT token service, application service, Minimal API endpoints, EF Core migrations, and the full frontend auth flow (login, register, protected routes, token refresh). After this sprint, users can register, log in, and all subsequent feature pages are protected behind authentication.

**This sprint is a blocker for Sprints 2–4.** No Finance or Reporting module work should begin until every task here is Done.

---

## Scope

### What's Included
- Create the `Personal.FinanceTracker.Users` module project with Clean Architecture folder structure
- Register the Users project in the `.slnx` solution and wire project references
- Install required NuGet packages into the Users project
- `User` and `RefreshToken` domain entities with private constructors and static factory methods
- `IUserRepository` interface (Application layer) and `UserRepository` EF Core implementation (Infrastructure layer)
- `UsersDbContext` with `users` schema, Fluent API entity configurations, snake_case columns
- EF Core migration `InitialUsersSchema`
- `ITokenService` / `TokenService` — JWT access token generation, refresh token generation, principal extraction
- `IUserService` / `UserService` — register, login, refresh token, revoke token
- Request/response DTOs as `record` types with FluentValidation validators
- `AuthEndpoints` Minimal API endpoints: register, login, refresh, revoke
- `ClaimsPrincipalExtensions` in the Shared project
- `UsersModule.cs` registration — `AddUsersModule` + `MapUsersEndpoints`; wire into `Program.cs`
- Frontend: `src/types/auth.ts`, `src/api/client.ts` (fetch-based wrapper), `src/api/auth.ts`
- Frontend: `AuthContext`, `useAuth` hook
- Frontend: `LoginPage`, `RegisterPage` (React Hook Form + Zod)
- Frontend: `ProtectedRoute` component
- Frontend: Update router to add public auth routes and protect the main layout
- Frontend: Update `Header` to show logged-in user name and a logout button

### Out of Scope
- Finance module entities, transactions, categories, budgets
- Reporting module
- Email verification or password reset flows
- Role-based authorization (all authenticated users share the same access level this sprint)

### Known Gaps
- A running PostgreSQL instance is required for migrations. Use Docker:
  ```bash
  docker run -d --name finance-db -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=finance_tracker_dev -p 5432:5432 postgres:16
  ```
- `appsettings.Development.json` has placeholder JWT values (`<jwt_secret_key>` etc.) — replace these with real values in a local `appsettings.Local.json` (already gitignored) before running migrations or starting the API.

---

## Tasks

---

### Task 1 — Create the Users Module Project

**Status:** Done

**Description:**
Create the `Personal.FinanceTracker.Users` class library project inside `backend/src/Modules/Users/`, register it in the solution, and add the required project references: Users → Shared, Api → Users.

**Steps:**

1. Create the project:
   ```bash
   cd backend
   dotnet new classlib -n Personal.FinanceTracker.Users -o src/Modules/Users --framework net10.0
   ```

2. Remove the default stub:
   ```bash
   del src/Modules/Users/Class1.cs
   ```

3. Replace the generated `.csproj` content with the minimal version (framework comes from `Directory.Build.props`):

   **`backend/src/Modules/Users/Personal.FinanceTracker.Users.csproj`:**
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

   </Project>
   ```

4. Create the Clean Architecture folder structure:
   ```bash
   mkdir src/Modules/Users/Domain/Entities
   mkdir src/Modules/Users/Domain/Interfaces
   mkdir src/Modules/Users/Application/DTOs/Requests
   mkdir src/Modules/Users/Application/DTOs/Responses
   mkdir src/Modules/Users/Application/Services
   mkdir src/Modules/Users/Application/Validators
   mkdir src/Modules/Users/Infrastructure/Data
   mkdir src/Modules/Users/Infrastructure/Data/Configurations
   mkdir src/Modules/Users/Infrastructure/Data/Migrations
   mkdir src/Modules/Users/Infrastructure/Repositories
   mkdir src/Modules/Users/Api/Endpoints
   ```

5. Register the project in the solution:
   ```bash
   dotnet sln Personal.FinanceTracker.slnx add src/Modules/Users/Personal.FinanceTracker.Users.csproj
   ```

6. Add the reference from Api to Users:
   ```bash
   dotnet add src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj reference src/Modules/Users/Personal.FinanceTracker.Users.csproj
   ```

7. Run `dotnet build Personal.FinanceTracker.slnx` and confirm 0 errors.

**Success Criteria:**
- `dotnet sln list` shows three projects: Api, Shared, Users
- `dotnet build` passes with 0 errors and 0 warnings

---

### Task 2 — Install NuGet Packages into the Users Project

**Status:** Done

**Description:**
Install all NuGet packages required by the Users module. The `FrameworkReference` covers ASP.NET Core packages; only third-party and EF Core packages need explicit references.

**Steps:**

1. From the `backend/` directory:
   ```bash
   dotnet add src/Modules/Users package BCrypt.Net-Next
   dotnet add src/Modules/Users package Microsoft.EntityFrameworkCore
   dotnet add src/Modules/Users package Npgsql.EntityFrameworkCore.PostgreSQL
   dotnet add src/Modules/Users package Microsoft.EntityFrameworkCore.Design
   dotnet add src/Modules/Users package FluentValidation
   dotnet add src/Modules/Users package Microsoft.IdentityModel.Tokens
   dotnet add src/Modules/Users package System.IdentityModel.Tokens.Jwt
   ```

2. Open `src/Modules/Users/Personal.FinanceTracker.Users.csproj` and ensure `Microsoft.EntityFrameworkCore.Design` has the correct asset metadata:
   ```xml
   <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="...">
     <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
     <PrivateAssets>all</PrivateAssets>
   </PackageReference>
   ```

3. Run `dotnet build Personal.FinanceTracker.slnx` — confirm 0 errors.

**Success Criteria:**
- All packages restore without errors
- `dotnet build` passes cleanly

---

### Task 3 — User Domain Entity

**Status:** Done

**Description:**
Create the `User` entity in the Domain layer. It extends `Entity` from `Personal.FinanceTracker.Shared.Abstractions`, uses a private constructor, a static `Create` factory method, and an `UpdatePassword` method. All properties have `private set`.

**Steps:**

1. Create `backend/src/Modules/Users/Domain/Entities/User.cs`:
   ```csharp
   using Personal.FinanceTracker.Shared.Abstractions;

   namespace Personal.FinanceTracker.Users.Domain.Entities;

   public sealed class User : Entity
   {
       public string Email { get; private set; } = string.Empty;
       public string PasswordHash { get; private set; } = string.Empty;
       public string FirstName { get; private set; } = string.Empty;
       public string LastName { get; private set; } = string.Empty;

       private User() { }

       public static User Create(
           string email,
           string passwordHash,
           string firstName,
           string lastName)
       {
           if (string.IsNullOrWhiteSpace(email))
               throw new ArgumentException("Email is required.", nameof(email));

           if (string.IsNullOrWhiteSpace(passwordHash))
               throw new ArgumentException("Password hash is required.", nameof(passwordHash));

           if (string.IsNullOrWhiteSpace(firstName))
               throw new ArgumentException("First name is required.", nameof(firstName));

           if (string.IsNullOrWhiteSpace(lastName))
               throw new ArgumentException("Last name is required.", nameof(lastName));

           return new User
           {
               Id = Guid.NewGuid(),
               Email = email.ToLowerInvariant().Trim(),
               PasswordHash = passwordHash,
               FirstName = firstName.Trim(),
               LastName = lastName.Trim(),
               CreatedAt = DateTime.UtcNow
           };
       }

       public void UpdatePassword(string newPasswordHash)
       {
           if (string.IsNullOrWhiteSpace(newPasswordHash))
               throw new ArgumentException("Password hash is required.", nameof(newPasswordHash));

           PasswordHash = newPasswordHash;
           UpdatedAt = DateTime.UtcNow;
       }
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- `User.cs` compiles with no errors or warnings
- Entity extends `Personal.FinanceTracker.Shared.Abstractions.Entity`

---

### Task 4 — RefreshToken Domain Entity

**Status:** Done

**Description:**
Create the `RefreshToken` entity. It does not extend `Entity` (it has no `UpdatedAt` semantics and revocation is a one-way operation). It uses a private constructor, a static `Create` factory, and a `Revoke()` method.

**Steps:**

1. Create `backend/src/Modules/Users/Domain/Entities/RefreshToken.cs`:
   ```csharp
   namespace Personal.FinanceTracker.Users.Domain.Entities;

   public sealed class RefreshToken
   {
       public Guid Id { get; private set; }
       public string Token { get; private set; } = string.Empty;
       public Guid UserId { get; private set; }
       public DateTime ExpiresAt { get; private set; }
       public DateTime CreatedAt { get; private set; }
       public bool IsRevoked { get; private set; }
       public DateTime? RevokedAt { get; private set; }

       public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
       public bool IsActive => !IsRevoked && !IsExpired;

       private RefreshToken() { }

       public static RefreshToken Create(Guid userId, string token, DateTime expiresAt)
       {
           if (string.IsNullOrWhiteSpace(token))
               throw new ArgumentException("Token is required.", nameof(token));

           return new RefreshToken
           {
               Id = Guid.NewGuid(),
               Token = token,
               UserId = userId,
               ExpiresAt = expiresAt,
               CreatedAt = DateTime.UtcNow,
               IsRevoked = false
           };
       }

       public void Revoke()
       {
           IsRevoked = true;
           RevokedAt = DateTime.UtcNow;
       }
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- `RefreshToken.cs` compiles cleanly
- `IsActive` computed property returns correct value based on `IsRevoked` and `IsExpired`

---

### Task 5 — IUserRepository Interface

**Status:** Done

> **Implementation note:** A generic `IRepository<T>` base interface was added to `Domain/Interfaces/IRepository.cs`. `IUserRepository` extends it. This was not in the original spec but aligns with the architecture.

**Description:**
Define the repository interface in the Application layer. The domain defines the contract; Infrastructure implements it. All methods accept a `CancellationToken`.

**Steps:**

1. Create `backend/src/Modules/Users/Domain/Interfaces/IUserRepository.cs`:
   ```csharp
   using Personal.FinanceTracker.Users.Domain.Entities;

   namespace Personal.FinanceTracker.Users.Domain.Interfaces;

   public interface IUserRepository
   {
       Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
       Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
       Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
       Task AddAsync(User user, CancellationToken ct = default);
       Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken ct = default);
       Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken ct = default);
       Task SaveChangesAsync(CancellationToken ct = default);
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- Interface compiles with all required method signatures
- No implementation details (no EF Core references) in the interface

---

### Task 6 — UsersDbContext and Entity Configurations

**Status:** Done

> **Implementation note:** Index names use `idx_` prefix in the actual migration rather than the `ix_` prefix shown in this spec. Functionally equivalent.

**Description:**
Create `UsersDbContext` with the `users` schema, and `IEntityTypeConfiguration<T>` classes for both entities using Fluent API only — no Data Annotations on entities. All column names use snake_case.

**Steps:**

1. Create `backend/src/Modules/Users/Infrastructure/Data/UsersDbContext.cs`:
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using Personal.FinanceTracker.Users.Domain.Entities;
   using Personal.FinanceTracker.Users.Infrastructure.Data.Configurations;

   namespace Personal.FinanceTracker.Users.Infrastructure.Data;

   public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
   {
       public DbSet<User> Users => Set<User>();
       public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

       protected override void OnModelCreating(ModelBuilder modelBuilder)
       {
           modelBuilder.HasDefaultSchema("users");
           modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
       }
   }
   ```

2. Create `backend/src/Modules/Users/Infrastructure/Data/Configurations/UserConfiguration.cs`:
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using Microsoft.EntityFrameworkCore.Metadata.Builders;
   using Personal.FinanceTracker.Users.Domain.Entities;

   namespace Personal.FinanceTracker.Users.Infrastructure.Data.Configurations;

   public sealed class UserConfiguration : IEntityTypeConfiguration<User>
   {
       public void Configure(EntityTypeBuilder<User> builder)
       {
           builder.ToTable("users");

           builder.HasKey(u => u.Id);

           builder.Property(u => u.Id)
               .HasColumnName("id")
               .ValueGeneratedNever();

           builder.Property(u => u.Email)
               .HasColumnName("email")
               .HasMaxLength(256)
               .IsRequired();

           builder.Property(u => u.PasswordHash)
               .HasColumnName("password_hash")
               .HasMaxLength(512)
               .IsRequired();

           builder.Property(u => u.FirstName)
               .HasColumnName("first_name")
               .HasMaxLength(100)
               .IsRequired();

           builder.Property(u => u.LastName)
               .HasColumnName("last_name")
               .HasMaxLength(100)
               .IsRequired();

           builder.Property(u => u.CreatedAt)
               .HasColumnName("created_at")
               .HasDefaultValueSql("now()")
               .IsRequired();

           builder.Property(u => u.UpdatedAt)
               .HasColumnName("updated_at");

           builder.HasIndex(u => u.Email)
               .IsUnique()
               .HasDatabaseName("ix_users_email");

           builder.HasMany<RefreshToken>()
               .WithOne()
               .HasForeignKey(rt => rt.UserId)
               .OnDelete(DeleteBehavior.Cascade);
       }
   }
   ```

3. Create `backend/src/Modules/Users/Infrastructure/Data/Configurations/RefreshTokenConfiguration.cs`:
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using Microsoft.EntityFrameworkCore.Metadata.Builders;
   using Personal.FinanceTracker.Users.Domain.Entities;

   namespace Personal.FinanceTracker.Users.Infrastructure.Data.Configurations;

   public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
   {
       public void Configure(EntityTypeBuilder<RefreshToken> builder)
       {
           builder.ToTable("refresh_tokens");

           builder.HasKey(rt => rt.Id);

           builder.Property(rt => rt.Id)
               .HasColumnName("id")
               .ValueGeneratedNever();

           builder.Property(rt => rt.Token)
               .HasColumnName("token")
               .HasMaxLength(512)
               .IsRequired();

           builder.Property(rt => rt.UserId)
               .HasColumnName("user_id")
               .IsRequired();

           builder.Property(rt => rt.ExpiresAt)
               .HasColumnName("expires_at")
               .IsRequired();

           builder.Property(rt => rt.CreatedAt)
               .HasColumnName("created_at")
               .HasDefaultValueSql("now()")
               .IsRequired();

           builder.Property(rt => rt.IsRevoked)
               .HasColumnName("is_revoked")
               .HasDefaultValue(false)
               .IsRequired();

           builder.Property(rt => rt.RevokedAt)
               .HasColumnName("revoked_at");

           builder.Ignore(rt => rt.IsExpired);
           builder.Ignore(rt => rt.IsActive);

           builder.HasIndex(rt => rt.Token)
               .IsUnique()
               .HasDatabaseName("ix_refresh_tokens_token");

           builder.HasIndex(rt => rt.UserId)
               .HasDatabaseName("ix_refresh_tokens_user_id");
       }
   }
   ```

4. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- `UsersDbContext` compiles with `users` schema set
- Both entity configurations compile with no Data Annotations on the entities

---

### Task 7 — UserRepository Implementation

**Status:** Done

**Description:**
Implement `IUserRepository` in the Infrastructure layer using `UsersDbContext`. Pass `CancellationToken` through to all EF Core async calls.

**Steps:**

1. Create `backend/src/Modules/Users/Infrastructure/Repositories/UserRepository.cs`:
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using Personal.FinanceTracker.Users.Domain.Entities;
   using Personal.FinanceTracker.Users.Domain.Interfaces;
   using Personal.FinanceTracker.Users.Infrastructure.Data;

   namespace Personal.FinanceTracker.Users.Infrastructure.Repositories;

   public sealed class UserRepository(UsersDbContext context) : IUserRepository
   {
       public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
           => await context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

       public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
           => await context.Users
               .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant().Trim(), ct);

       public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
           => await context.Users
               .AnyAsync(u => u.Email == email.ToLowerInvariant().Trim(), ct);

       public async Task AddAsync(User user, CancellationToken ct = default)
           => await context.Users.AddAsync(user, ct);

       public async Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken ct = default)
           => await context.RefreshTokens.AddAsync(refreshToken, ct);

       public async Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken ct = default)
           => await context.RefreshTokens
               .FirstOrDefaultAsync(rt => rt.Token == token, ct);

       public async Task SaveChangesAsync(CancellationToken ct = default)
           => await context.SaveChangesAsync(ct);
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- `UserRepository` compiles and fully implements `IUserRepository`
- No EF Core references leak into Domain or Application layers

---

### Task 8 — EF Core Migration: InitialUsersSchema

**Status:** Done

> **Implementation note:** Step 6 (remove the temporary `AddDbContext` call from `Program.cs`) was NOT completed. The temporary registration on lines 13–14 of `Program.cs` remains alongside the `AddUsersModule` call on line 65. This creates a duplicate `UsersDbContext` DI registration. This should be cleaned up before Sprint 2.

**Description:**
Create and verify the initial EF Core migration for the Users module. The migration creates the `users` schema, `users.users` table, and `users.refresh_tokens` table.

**Steps:**

1. Ensure the connection string in `appsettings.Local.json` (or `appsettings.Development.json`) points to a running PostgreSQL instance:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=finance_tracker_dev;Username=postgres;Password=postgres"
     }
   }
   ```

2. Register the `UsersDbContext` temporarily in `Program.cs` before running migrations (this will be replaced by `AddUsersModule` in Task 15 — for now add it directly so EF tooling can discover the context):
   ```csharp
   // Temporary — will be replaced by AddUsersModule in Task 15
   builder.Services.AddDbContext<Personal.FinanceTracker.Users.Infrastructure.Data.UsersDbContext>(options =>
       options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
   ```

3. From the `backend/` directory, run:
   ```bash
   dotnet ef migrations add InitialUsersSchema \
     --project src/Modules/Users/Personal.FinanceTracker.Users.csproj \
     --startup-project src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj \
     --context UsersDbContext \
     --output-dir Infrastructure/Data/Migrations
   ```

4. Review the generated migration file to confirm:
   - Schema `users` is created with `CREATE SCHEMA`
   - Table `users.users` has all expected columns with correct types
   - Table `users.refresh_tokens` has all expected columns
   - Unique indexes are present on `email` and `token`

5. Apply the migration:
   ```bash
   dotnet ef database update \
     --project src/Modules/Users/Personal.FinanceTracker.Users.csproj \
     --startup-project src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj \
     --context UsersDbContext
   ```

6. Remove the temporary `AddDbContext` call from `Program.cs` added in step 2 — it will be properly registered via `AddUsersModule` in Task 15.

**Success Criteria:**
- Migration file `InitialUsersSchema.cs` generated in `Infrastructure/Data/Migrations/`
- `dotnet ef database update` runs without errors
- Tables `users.users` and `users.refresh_tokens` exist in the database

---

### Task 9 — DTOs (Request and Response Records)

**Status:** Done

**Description:**
Create all request and response DTOs as `record` types. They live in the Application layer. Response types are immutable value objects — no setters.

**Steps:**

1. Create `backend/src/Modules/Users/Application/DTOs/Requests/RegisterRequest.cs`:
   ```csharp
   namespace Personal.FinanceTracker.Users.Application.DTOs.Requests;

   public sealed record RegisterRequest(
       string Email,
       string Password,
       string FirstName,
       string LastName);
   ```

2. Create `backend/src/Modules/Users/Application/DTOs/Requests/LoginRequest.cs`:
   ```csharp
   namespace Personal.FinanceTracker.Users.Application.DTOs.Requests;

   public sealed record LoginRequest(
       string Email,
       string Password);
   ```

3. Create `backend/src/Modules/Users/Application/DTOs/Requests/RefreshTokenRequest.cs`:
   ```csharp
   namespace Personal.FinanceTracker.Users.Application.DTOs.Requests;

   public sealed record RefreshTokenRequest(string RefreshToken);
   ```

4. Create `backend/src/Modules/Users/Application/DTOs/Responses/UserResponse.cs`:
   ```csharp
   namespace Personal.FinanceTracker.Users.Application.DTOs.Responses;

   public sealed record UserResponse(
       Guid Id,
       string Email,
       string FirstName,
       string LastName);
   ```

5. Create `backend/src/Modules/Users/Application/DTOs/Responses/AuthResponse.cs`:
   ```csharp
   namespace Personal.FinanceTracker.Users.Application.DTOs.Responses;

   public sealed record AuthResponse(
       string AccessToken,
       string RefreshToken,
       int ExpiresIn,
       UserResponse User);
   ```

6. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- All five DTO files compile as `sealed record` types
- No mutable setters — all properties are positional parameters (init-only)

---

### Task 10 — FluentValidation Validators

**Status:** Done

> **Implementation note:** `RegisterRequestValidator` includes additional rules beyond this spec: a special character requirement (`.Matches("[.,&()-*]")`) and character set constraints on name fields. `LoginRequestValidator` adds `MaximumLength(256)` on the email field. These are valid enhancements.

**Description:**
Create one `AbstractValidator<T>` per mutating request. Validators live in `Application/Validators/` and are registered via `AddValidatorsFromAssemblyContaining<T>()` in the module registration.

**Steps:**

1. Create `backend/src/Modules/Users/Application/Validators/RegisterRequestValidator.cs`:
   ```csharp
   using FluentValidation;
   using Personal.FinanceTracker.Users.Application.DTOs.Requests;

   namespace Personal.FinanceTracker.Users.Application.Validators;

   public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
   {
       public RegisterRequestValidator()
       {
           RuleFor(x => x.Email)
               .NotEmpty().WithMessage("Email is required.")
               .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.")
               .EmailAddress().WithMessage("Email must be a valid email address.");

           RuleFor(x => x.Password)
               .NotEmpty().WithMessage("Password is required.")
               .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
               .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.")
               .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
               .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
               .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

           RuleFor(x => x.FirstName)
               .NotEmpty().WithMessage("First name is required.")
               .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

           RuleFor(x => x.LastName)
               .NotEmpty().WithMessage("Last name is required.")
               .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");
       }
   }
   ```

2. Create `backend/src/Modules/Users/Application/Validators/LoginRequestValidator.cs`:
   ```csharp
   using FluentValidation;
   using Personal.FinanceTracker.Users.Application.DTOs.Requests;

   namespace Personal.FinanceTracker.Users.Application.Validators;

   public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
   {
       public LoginRequestValidator()
       {
           RuleFor(x => x.Email)
               .NotEmpty().WithMessage("Email is required.")
               .EmailAddress().WithMessage("Email must be a valid email address.");

           RuleFor(x => x.Password)
               .NotEmpty().WithMessage("Password is required.");
       }
   }
   ```

3. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- Both validators compile and have the correct base type `AbstractValidator<T>`
- No duplicate email check in validators — that business rule belongs in `UserService`

---

### Task 11 — ITokenService and TokenService

**Status:** Done

> **Implementation note:** This spec placed `ITokenService` in `Application/Services/`. The actual implementation places it in `Application/Interfaces/` — consistent with how `IUserService` and `IJwtSettings` are placed. `TokenService` uses `IOptions<JwtSettings>` instead of reading `IConfiguration` string keys directly (cleaner, strongly-typed). An additional `IJwtSettings` interface and `JwtSettings` class were introduced — see Task 15 notes.
>
> Both `ITokenService` and `TokenService` now live at:
> - `Application/Interfaces/ITokenService.cs`
> - `Infrastructure/Services/TokenService.cs`

**Description:**
Create the token service interface in the Application layer and its implementation in Infrastructure. `TokenService` reads JWT configuration from `IConfiguration`, generates access tokens (signed JWTs), generates refresh tokens (cryptographically random), and can extract a `ClaimsPrincipal` from an expired access token for the refresh flow.

**Steps:**

1. Create `backend/src/Modules/Users/Application/Services/ITokenService.cs`:
   ```csharp
   using System.Security.Claims;
   using Personal.FinanceTracker.Users.Domain.Entities;

   namespace Personal.FinanceTracker.Users.Application.Services;

   public interface ITokenService
   {
       string GenerateAccessToken(User user);
       string GenerateRefreshToken();
       ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
   }
   ```

2. Create `backend/src/Modules/Users/Infrastructure/TokenService.cs`:
   ```csharp
   using System.IdentityModel.Tokens.Jwt;
   using System.Security.Claims;
   using System.Security.Cryptography;
   using System.Text;
   using Microsoft.Extensions.Configuration;
   using Microsoft.IdentityModel.Tokens;
   using Personal.FinanceTracker.Users.Application.Services;
   using Personal.FinanceTracker.Users.Domain.Entities;

   namespace Personal.FinanceTracker.Users.Infrastructure;

   public sealed class TokenService(IConfiguration configuration) : ITokenService
   {
       public string GenerateAccessToken(User user)
       {
           var jwtConfig = configuration.GetSection("Jwt");
           var secretKey = jwtConfig["SecretKey"]!;
           var issuer = jwtConfig["Issuer"]!;
           var audience = jwtConfig["Audience"]!;
           var expiryMinutes = int.Parse(jwtConfig["ExpiryMinutes"] ?? "60");

           var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
           var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

           var claims = new[]
           {
               new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
               new Claim(ClaimTypes.Email, user.Email),
               new Claim(ClaimTypes.GivenName, user.FirstName),
               new Claim(ClaimTypes.Surname, user.LastName),
               new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
           };

           var token = new JwtSecurityToken(
               issuer: issuer,
               audience: audience,
               claims: claims,
               expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
               signingCredentials: credentials);

           return new JwtSecurityTokenHandler().WriteToken(token);
       }

       public string GenerateRefreshToken()
       {
           var bytes = RandomNumberGenerator.GetBytes(64);
           return Convert.ToBase64String(bytes);
       }

       public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
       {
           var jwtConfig = configuration.GetSection("Jwt");
           var secretKey = jwtConfig["SecretKey"]!;

           var validationParameters = new TokenValidationParameters
           {
               ValidateIssuerSigningKey = true,
               IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
               ValidateIssuer = false,
               ValidateAudience = false,
               ValidateLifetime = false  // expired tokens are valid here — we only verify the signature
           };

           var handler = new JwtSecurityTokenHandler();
           var principal = handler.ValidateToken(token, validationParameters, out var securityToken);

           if (securityToken is not JwtSecurityToken jwt ||
               !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
           {
               throw new UnauthorizedAccessException("Invalid token.");
           }

           return principal;
       }
   }
   ```

3. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- `ITokenService` is defined in Application (no Infrastructure references)
- `TokenService` is in Infrastructure and implements `ITokenService`
- `GetPrincipalFromExpiredToken` does not validate lifetime — intentional for the refresh flow

---

### Task 12 — IUserService and UserService

**Status:** Done

> **Intentional deviation:** This spec defines `IUserService` with nullable return types (`AuthResponse?`, `bool`). The actual implementation uses `Result<AuthResponse>` and `Result<bool>` throughout. This is a deliberate, better design — `Result<T>` makes success/failure explicit without null checks and carries a typed error code + description. See `DESIGN_PATTERNS.md` — Result Pattern.
>
> `UserService` lives in `Infrastructure/Services/` (not `Application/Services/`) because it depends on `BCrypt.Net.BCrypt`, an external library. Its contract `IUserService` remains in `Application/Interfaces/`. This is consistent with the `ITokenService`/`TokenService` placement and is the correct pattern — see `DESIGN_PATTERNS.md` — Service Placement Rule.

**Description:**
Create the user service interface in Application and its implementation. All business logic (duplicate email check, BCrypt hashing, token rotation) lives here. Endpoints remain thin. Services return `null` or `bool` for expected not-found/failure cases — they throw only for programming errors or truly invalid state.

**Steps:**

1. Create `backend/src/Modules/Users/Application/Services/IUserService.cs`:
   ```csharp
   using Personal.FinanceTracker.Users.Application.DTOs.Requests;
   using Personal.FinanceTracker.Users.Application.DTOs.Responses;

   namespace Personal.FinanceTracker.Users.Application.Services;

   public interface IUserService
   {
       Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
       Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
       Task<AuthResponse?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
       Task<bool> RevokeTokenAsync(Guid userId, string refreshToken, CancellationToken ct = default);
   }
   ```

2. Create `backend/src/Modules/Users/Application/Services/UserService.cs`:
   ```csharp
   using Microsoft.Extensions.Configuration;
   using Microsoft.Extensions.Logging;
   using Personal.FinanceTracker.Users.Application.DTOs.Requests;
   using Personal.FinanceTracker.Users.Application.DTOs.Responses;
   using Personal.FinanceTracker.Users.Domain.Entities;
   using Personal.FinanceTracker.Users.Domain.Interfaces;

   namespace Personal.FinanceTracker.Users.Application.Services;

   public sealed class UserService(
       IUserRepository repository,
       ITokenService tokenService,
       IConfiguration configuration,
       ILogger<UserService> logger) : IUserService
   {
       public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
       {
           if (await repository.EmailExistsAsync(request.Email, ct))
           {
               logger.LogWarning("Registration attempted with existing email {Email}", request.Email);
               return null;
           }

           var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
           var user = User.Create(request.Email, passwordHash, request.FirstName, request.LastName);

           await repository.AddAsync(user, ct);

           var refreshTokenValue = tokenService.GenerateRefreshToken();
           var refreshTokenExpiry = GetRefreshTokenExpiry();
           var refreshToken = RefreshToken.Create(user.Id, refreshTokenValue, refreshTokenExpiry);

           await repository.AddRefreshTokenAsync(refreshToken, ct);
           await repository.SaveChangesAsync(ct);

           logger.LogInformation("User {UserId} registered successfully", user.Id);

           return BuildAuthResponse(user, refreshTokenValue);
       }

       public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
       {
           var user = await repository.GetByEmailAsync(request.Email, ct);

           if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
           {
               logger.LogWarning("Failed login attempt for email {Email}", request.Email);
               return null;
           }

           var refreshTokenValue = tokenService.GenerateRefreshToken();
           var refreshTokenExpiry = GetRefreshTokenExpiry();
           var refreshToken = RefreshToken.Create(user.Id, refreshTokenValue, refreshTokenExpiry);

           await repository.AddRefreshTokenAsync(refreshToken, ct);
           await repository.SaveChangesAsync(ct);

           logger.LogInformation("User {UserId} logged in successfully", user.Id);

           return BuildAuthResponse(user, refreshTokenValue);
       }

       public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
       {
           var storedToken = await repository.GetRefreshTokenAsync(refreshToken, ct);

           if (storedToken is null || !storedToken.IsActive)
           {
               logger.LogWarning("Invalid or expired refresh token used");
               return null;
           }

           var user = await repository.GetByIdAsync(storedToken.UserId, ct);

           if (user is null)
               return null;

           storedToken.Revoke();

           var newRefreshTokenValue = tokenService.GenerateRefreshToken();
           var newRefreshToken = RefreshToken.Create(user.Id, newRefreshTokenValue, GetRefreshTokenExpiry());

           await repository.AddRefreshTokenAsync(newRefreshToken, ct);
           await repository.SaveChangesAsync(ct);

           return BuildAuthResponse(user, newRefreshTokenValue);
       }

       public async Task<bool> RevokeTokenAsync(Guid userId, string refreshToken, CancellationToken ct = default)
       {
           var storedToken = await repository.GetRefreshTokenAsync(refreshToken, ct);

           if (storedToken is null || storedToken.UserId != userId || !storedToken.IsActive)
               return false;

           storedToken.Revoke();
           await repository.SaveChangesAsync(ct);

           return true;
       }

       private AuthResponse BuildAuthResponse(User user, string refreshTokenValue)
       {
           var accessToken = tokenService.GenerateAccessToken(user);
           var expiryMinutes = int.Parse(configuration["Jwt:ExpiryMinutes"] ?? "60");

           return new AuthResponse(
               AccessToken: accessToken,
               RefreshToken: refreshTokenValue,
               ExpiresIn: expiryMinutes * 60,
               User: new UserResponse(user.Id, user.Email, user.FirstName, user.LastName));
       }

       private DateTime GetRefreshTokenExpiry()
       {
           var days = int.Parse(configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");
           return DateTime.UtcNow.AddDays(days);
       }
   }
   ```

3. Add `RefreshTokenExpiryDays` to `appsettings.Development.json`:
   ```json
   {
     "Jwt": {
       "SecretKey": "<jwt_secret_key>",
       "Issuer": "<jwt_issuer>",
       "Audience": "<jwt_audience>",
       "ExpiryMinutes": 60,
       "RefreshTokenExpiryDays": 7
     }
   }
   ```

4. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- `RegisterAsync` returns `null` (not throws) when email already exists
- `LoginAsync` returns `null` on invalid credentials — never reveals whether email exists
- `RefreshTokenAsync` rotates the refresh token (old one revoked, new one issued)
- No EF Core or BCrypt references in the interface

---

### Task 13 — ClaimsPrincipalExtensions in Shared

**Status:** Done

**Description:**
Add `ClaimsPrincipalExtensions` to the Shared project so all modules can extract typed values from the authenticated user's claims. These will be used by Auth endpoints to get `UserId` and by future Finance/Reporting endpoints.

**Steps:**

1. Create `backend/src/Personal.FinanceTracker.Shared/Extensions/ClaimsPrincipalExtensions.cs`:
   ```csharp
   using System.Security.Claims;

   namespace Personal.FinanceTracker.Shared.Extensions;

   public static class ClaimsPrincipalExtensions
   {
       public static Guid GetUserId(this ClaimsPrincipal user)
       {
           var claim = user.FindFirst(ClaimTypes.NameIdentifier)
               ?? throw new UnauthorizedAccessException("User ID claim not found.");

           return Guid.Parse(claim.Value);
       }

       public static string GetEmail(this ClaimsPrincipal user)
       {
           return user.FindFirst(ClaimTypes.Email)?.Value
               ?? throw new UnauthorizedAccessException("Email claim not found.");
       }
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- Extension methods are in the Shared project (available to all modules)
- Both methods throw `UnauthorizedAccessException` on missing claims — this is caught by `ExceptionHandlingMiddleware` and returned as 401

---

### Task 14 — AuthEndpoints Minimal API

**Status:** Done

> **Intentional deviation:** Endpoints return `ApiResponse<AuthResponse>` envelopes rather than bare `AuthResponse` / `UnauthorizedHttpResult` as shown in this spec. This is consistent with the project-wide `ApiResponse<T>` pattern. `HttpContext` is injected to populate `Error.Instance` with the request path. The file has a typo in its name: `AuthEnpoints.cs` (missing 'd') — cosmetic, does not affect functionality.

**Description:**
Create the `AuthEndpoints` static class in the Api layer. Endpoints are thin — they delegate entirely to `IUserService`. Use `TypedResults` for full OpenAPI type inference. Apply `ValidationFilter<T>` to mutating endpoints.

**Steps:**

1. Create `backend/src/Modules/Users/Api/Endpoints/AuthEndpoints.cs`:
   ```csharp
   using System.Security.Claims;
   using Microsoft.AspNetCore.Builder;
   using Microsoft.AspNetCore.Http;
   using Microsoft.AspNetCore.Http.HttpResults;
   using Microsoft.AspNetCore.Routing;
   using Personal.FinanceTracker.Shared.Extensions;
   using Personal.FinanceTracker.Shared.Filters;
   using Personal.FinanceTracker.Users.Application.DTOs.Requests;
   using Personal.FinanceTracker.Users.Application.DTOs.Responses;
   using Personal.FinanceTracker.Users.Application.Services;

   namespace Personal.FinanceTracker.Users.Api.Endpoints;

   public static class AuthEndpoints
   {
       public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
       {
           var group = app.MapGroup("/api/auth")
               .WithTags("Authentication");

           group.MapPost("/register", RegisterAsync)
               .WithName("Register")
               .WithDescription("Create a new user account and return tokens.")
               .AddEndpointFilter<ValidationFilter<RegisterRequest>>();

           group.MapPost("/login", LoginAsync)
               .WithName("Login")
               .WithDescription("Authenticate with email and password and return tokens.")
               .AddEndpointFilter<ValidationFilter<LoginRequest>>();

           group.MapPost("/refresh", RefreshAsync)
               .WithName("RefreshToken")
               .WithDescription("Exchange a valid refresh token for a new token pair.");

           group.MapPost("/revoke", RevokeAsync)
               .WithName("RevokeToken")
               .WithDescription("Revoke the current refresh token.")
               .RequireAuthorization();

           return app;
       }

       private static async Task<Results<Ok<AuthResponse>, Conflict<string>>> RegisterAsync(
           RegisterRequest request,
           IUserService userService,
           CancellationToken ct)
       {
           var result = await userService.RegisterAsync(request, ct);

           if (result is null)
               return TypedResults.Conflict("An account with this email address already exists.");

           return TypedResults.Ok(result);
       }

       private static async Task<Results<Ok<AuthResponse>, UnauthorizedHttpResult>> LoginAsync(
           LoginRequest request,
           IUserService userService,
           CancellationToken ct)
       {
           var result = await userService.LoginAsync(request, ct);

           if (result is null)
               return TypedResults.Unauthorized();

           return TypedResults.Ok(result);
       }

       private static async Task<Results<Ok<AuthResponse>, UnauthorizedHttpResult>> RefreshAsync(
           RefreshTokenRequest request,
           IUserService userService,
           CancellationToken ct)
       {
           var result = await userService.RefreshTokenAsync(request.RefreshToken, ct);

           if (result is null)
               return TypedResults.Unauthorized();

           return TypedResults.Ok(result);
       }

       private static async Task<Results<NoContent, UnauthorizedHttpResult>> RevokeAsync(
           RefreshTokenRequest request,
           IUserService userService,
           ClaimsPrincipal claimsPrincipal,
           CancellationToken ct)
       {
           var userId = claimsPrincipal.GetUserId();
           var success = await userService.RevokeTokenAsync(userId, request.RefreshToken, ct);

           if (!success)
               return TypedResults.Unauthorized();

           return TypedResults.NoContent();
       }
   }
   ```

2. Run `dotnet build` — confirm 0 errors.

**Success Criteria:**
- All four endpoints compile using `TypedResults`
- `ValidationFilter<T>` applied to register and login
- `RevokeAsync` calls `RequireAuthorization()` at the group level
- No business logic in any endpoint handler — all delegated to `IUserService`

---

### Task 15 — UsersModule Registration and Wire into Program.cs

**Status:** Done

> **Implementation notes:**
> - The module registration class is named `DependencyInjection` (not `UsersModule` as in the spec). The extension methods are `AddUsersModule` and `MapUsersEndpoints` as specified.
> - Two additions beyond the spec: `IJwtSettings` interface in `Application/Interfaces/` and `JwtSettings` concrete class in `Infrastructure/Configuration/`, registered as a singleton via `IOptions<JwtSettings>`. This decouples services from `IConfiguration` string indexers.
> - The stale TODO comment (`// TODO Sprint 1: builder.Services.AddUsersModule(...)`) was not removed from `Program.cs`. Clean this up before Sprint 2.
> - The temporary `AddDbContext<UsersDbContext>` registration from Task 8 Step 2 was not removed. See Task 8 note above.

**Description:**
Create the module registration class `UsersModule.cs` at the root of the Users project. This class is the single entry point for DI registration and endpoint mapping. Then replace the TODO Sprint 1 comments in `Program.cs` with the actual calls.

**Steps:**

1. Create `backend/src/Modules/Users/UsersModule.cs`:
   ```csharp
   using FluentValidation;
   using Microsoft.EntityFrameworkCore;
   using Microsoft.Extensions.Configuration;
   using Microsoft.Extensions.DependencyInjection;
   using Personal.FinanceTracker.Users.Api.Endpoints;
   using Personal.FinanceTracker.Users.Application.Services;
   using Personal.FinanceTracker.Users.Application.Validators;
   using Personal.FinanceTracker.Users.Domain.Interfaces;
   using Personal.FinanceTracker.Users.Infrastructure;
   using Personal.FinanceTracker.Users.Infrastructure.Data;
   using Personal.FinanceTracker.Users.Infrastructure.Repositories;

   namespace Personal.FinanceTracker.Users;

   public static class UsersModule
   {
       public static IServiceCollection AddUsersModule(
           this IServiceCollection services,
           IConfiguration configuration)
       {
           // Database
           services.AddDbContext<UsersDbContext>(options =>
               options.UseNpgsql(
                   configuration.GetConnectionString("DefaultConnection"),
                   npgsqlOptions =>
                   {
                       npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "users");
                       npgsqlOptions.EnableRetryOnFailure(
                           maxRetryCount: 3,
                           maxRetryDelay: TimeSpan.FromSeconds(10),
                           errorCodesToAdd: null);
                       npgsqlOptions.CommandTimeout(30);
                   }));

           // Repositories
           services.AddScoped<IUserRepository, UserRepository>();

           // Services
           services.AddScoped<ITokenService, TokenService>();
           services.AddScoped<IUserService, UserService>();

           // Validators
           services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

           return services;
       }

       public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
       {
           app.MapAuthEndpoints();
           return app;
       }
   }
   ```

2. Update `backend/src/Personal.FinanceTracker.Api/Program.cs` — replace the TODO Sprint 1 comments:

   **Before:**
   ```csharp
   // TODO Sprint 1: builder.Services.AddUsersModule(builder.Configuration);
   ```
   **After:**
   ```csharp
   builder.Services.AddUsersModule(builder.Configuration);
   ```

   **Before:**
   ```csharp
   // TODO Sprint 1: app.MapUsersEndpoints();
   ```
   **After:**
   ```csharp
   app.MapUsersEndpoints();
   ```

3. Add the required `using` to `Program.cs`:
   ```csharp
   using Personal.FinanceTracker.Users;
   ```

4. Run `dotnet build Personal.FinanceTracker.slnx` — confirm 0 errors, 0 warnings.

5. Run the API and verify:
   - `GET http://localhost:5194/swagger` loads with the `Authentication` tag and four endpoints visible
   - `GET http://localhost:5194/health/live` returns 200

**Success Criteria:**
- `dotnet build` passes with 0 errors and 0 warnings
- Swagger UI shows all four auth endpoints under the `Authentication` tag
- Module is self-contained — no auth logic in `Program.cs`

---

### Task 16 — Frontend: Auth Type Definitions

**Status:** Done

> **Extra additions:** `src/types/http.ts` was added (not in spec) — centralises `AppStatusCode` enum, `ApiError` class, and `ApiResponse<T>` interface. These are used throughout the client.

**Description:**
Create the TypeScript type definitions that mirror the backend DTOs exactly. All types live in `src/types/auth.ts`.

**Steps:**

1. Create `frontend/src/types/auth.ts`:
   ```typescript
   export interface UserResponse {
     id: string;
     email: string;
     firstName: string;
     lastName: string;
   }

   export interface AuthResponse {
     accessToken: string;
     refreshToken: string;
     expiresIn: number;
     user: UserResponse;
   }

   export interface LoginRequest {
     email: string;
     password: string;
   }

   export interface RegisterRequest {
     email: string;
     password: string;
     firstName: string;
     lastName: string;
   }

   export interface RefreshTokenRequest {
     refreshToken: string;
   }
   ```

2. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- All types mirror backend DTO property names (camelCase — JSON serialisation default)
- No `any` types

---

### Task 17 — Frontend: Fetch-based API Client

**Status:** Done

> **Implementation note:** The actual `client.ts` is more robust than the spec's simplified version. It includes a richer `ApiError` class (with `title`, `context`, `instance`, `status` fields), a `parseResponse` function, and `ClientLogger` integration for structured error logging. All API functions return `ApiResponse<T>` envelopes — not raw `T` — matching the backend response shape. The `return await` vs `return` distinction in `try-catch` is critical — see `client.ts` comments.

**Description:**
Create `src/api/client.ts` — a lightweight, typed wrapper around the native `fetch` API. It attaches the `Authorization: Bearer` header from `localStorage` on every request, handles 401 responses by attempting a token refresh (with a queue for concurrent 401s), and redirects to `/login` on refresh failure. No external HTTP library is used.

**Steps:**

1. Create `frontend/src/api/client.ts`:
   ```typescript
   import type { AuthResponse } from '@/types/auth';

   const BASE_URL = import.meta.env.VITE_API_URL ?? '/api';

   export class ApiError extends Error {
     constructor(
       public readonly status: number,
       message: string,
     ) {
       super(message);
       this.name = 'ApiError';
     }
   }

   let isRefreshing = false;
   let refreshPromise: Promise<string> | null = null;

   async function refreshAccessToken(): Promise<string> {
     const storedRefreshToken = localStorage.getItem('refreshToken');

     if (!storedRefreshToken) {
       throw new ApiError(401, 'No refresh token available.');
     }

     const response = await fetch(`${BASE_URL}/auth/refresh`, {
       method: 'POST',
       headers: { 'Content-Type': 'application/json' },
       body: JSON.stringify({ refreshToken: storedRefreshToken }),
     });

     if (!response.ok) {
       throw new ApiError(response.status, 'Token refresh failed.');
     }

     const data = (await response.json()) as AuthResponse;
     localStorage.setItem('accessToken', data.accessToken);
     localStorage.setItem('refreshToken', data.refreshToken);
     return data.accessToken;
   }

   async function getValidAccessToken(): Promise<string | null> {
     const token = localStorage.getItem('accessToken');
     return token;
   }

   async function request<T>(
     path: string,
     options: RequestInit = {},
     retry = true,
   ): Promise<T> {
     const token = await getValidAccessToken();

     const headers: Record<string, string> = {
       'Content-Type': 'application/json',
       ...(options.headers as Record<string, string>),
     };

     if (token) {
       headers['Authorization'] = `Bearer ${token}`;
     }

     const response = await fetch(`${BASE_URL}${path}`, { ...options, headers });

     if (response.status === 401 && retry) {
       // Deduplicate concurrent refresh calls
       if (!isRefreshing) {
         isRefreshing = true;
         refreshPromise = refreshAccessToken().finally(() => {
           isRefreshing = false;
           refreshPromise = null;
         });
       }

       try {
         const newToken = await refreshPromise!;
         headers['Authorization'] = `Bearer ${newToken}`;
         const retried = await fetch(`${BASE_URL}${path}`, { ...options, headers });

         if (!retried.ok) {
           throw new ApiError(retried.status, await retried.text());
         }

         return retried.status === 204 ? (undefined as T) : ((await retried.json()) as T);
       } catch {
         localStorage.removeItem('accessToken');
         localStorage.removeItem('refreshToken');
         localStorage.removeItem('user');
         window.location.href = '/login';
         throw new ApiError(401, 'Session expired.');
       }
     }

     if (!response.ok) {
       throw new ApiError(response.status, await response.text());
     }

     return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
   }

   export const apiClient = {
     get: <T>(path: string) => request<T>(path, { method: 'GET' }),
     post: <T>(path: string, body?: unknown) =>
       request<T>(path, { method: 'POST', body: JSON.stringify(body) }),
     put: <T>(path: string, body?: unknown) =>
       request<T>(path, { method: 'PUT', body: JSON.stringify(body) }),
     patch: <T>(path: string, body?: unknown) =>
       request<T>(path, { method: 'PATCH', body: JSON.stringify(body) }),
     delete: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
   };
   ```

2. Create `frontend/.env.example`:
   ```
   VITE_API_URL=/api
   ```

3. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- No `axios` import anywhere in `client.ts` — uses the native `fetch` API only
- `apiClient` exports typed `get`, `post`, `put`, `patch`, `delete` helpers
- Concurrent 401 responses share a single refresh call via `refreshPromise` deduplication
- On refresh failure, tokens and user are cleared from `localStorage` and the user is redirected to `/login`
- `ApiError` carries the HTTP status code for consumers to branch on

---

### Task 18 — Frontend: Auth API Module

**Status:** Done

> **Implementation note:** `authApi` functions return `ApiResponse<AuthResponse>` envelopes (not bare `AuthResponse`) matching the actual backend response shape. The spec showed bare return types which were based on the original nullable service design.

**Description:**
Create `src/api/auth.ts` — the `authApi` object with fully typed functions for all four auth endpoints. Uses `apiClient` from `@/api/client` exclusively — no direct `fetch` calls.

**Steps:**

1. Create `frontend/src/api/auth.ts`:
   ```typescript
   import type { AuthResponse, LoginRequest, RegisterRequest, RefreshTokenRequest } from '@/types/auth';
   import { apiClient } from '@/api/client';

   export const authApi = {
     login: (data: LoginRequest): Promise<AuthResponse> =>
       apiClient.post<AuthResponse>('/auth/login', data),

     register: (data: RegisterRequest): Promise<AuthResponse> =>
       apiClient.post<AuthResponse>('/auth/register', data),

     refresh: (data: RefreshTokenRequest): Promise<AuthResponse> =>
       apiClient.post<AuthResponse>('/auth/refresh', data),

     revoke: (data: RefreshTokenRequest): Promise<void> =>
       apiClient.post<void>('/auth/revoke', data),
   };
   ```

2. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- All four functions typed with request/response types from `src/types/auth.ts`
- Uses `apiClient` from `@/api/client` — no direct `fetch` or `axios` imports
- No `any` types

---

### Task 19 — Frontend: AuthContext and AuthProvider

**Status:** Done

> **Implementation note:** The context is split across two files — `src/components/auth/authContext.ts` (context object + type) and `src/components/auth/AuthProvider.tsx` (provider component) — rather than a single `src/context/AuthContext.tsx` as in the spec. The import path for `useAuth` uses the `components/auth/` location. Functionally equivalent.

**Description:**
Create `src/context/AuthContext.tsx`. The provider reads tokens from `localStorage` on mount (so auth state survives a page refresh), stores the decoded user, and exposes `login`, `register`, `logout`, `isAuthenticated`, and `isLoading`.

**Steps:**

1. Create `frontend/src/context/AuthContext.tsx`:
   ```tsx
   import { createContext, useEffect, useState } from 'react';
   import type { ReactNode } from 'react';
   import type { UserResponse, LoginRequest, RegisterRequest } from '@/types/auth';
   import { authApi } from '@/api/auth';

   interface AuthContextValue {
     user: UserResponse | null;
     isAuthenticated: boolean;
     isLoading: boolean;
     login: (data: LoginRequest) => Promise<void>;
     register: (data: RegisterRequest) => Promise<void>;
     logout: () => Promise<void>;
   }

   export const AuthContext = createContext<AuthContextValue | null>(null);

   interface AuthProviderProps {
     children: ReactNode;
   }

   export function AuthProvider({ children }: AuthProviderProps) {
     const [user, setUser] = useState<UserResponse | null>(null);
     const [isLoading, setIsLoading] = useState(true);

     useEffect(() => {
       const stored = localStorage.getItem('user');
       if (stored) {
         try {
           setUser(JSON.parse(stored) as UserResponse);
         } catch {
           localStorage.removeItem('user');
         }
       }
       setIsLoading(false);
     }, []);

     async function login(data: LoginRequest) {
       const response = await authApi.login(data);
       localStorage.setItem('accessToken', response.accessToken);
       localStorage.setItem('refreshToken', response.refreshToken);
       localStorage.setItem('user', JSON.stringify(response.user));
       setUser(response.user);
     }

     async function register(data: RegisterRequest) {
       const response = await authApi.register(data);
       localStorage.setItem('accessToken', response.accessToken);
       localStorage.setItem('refreshToken', response.refreshToken);
       localStorage.setItem('user', JSON.stringify(response.user));
       setUser(response.user);
     }

     async function logout() {
       const refreshToken = localStorage.getItem('refreshToken');
       if (refreshToken) {
         try {
           await authApi.revoke({ refreshToken });
         } catch {
           // best-effort revocation — clear locally regardless
         }
       }
       localStorage.removeItem('accessToken');
       localStorage.removeItem('refreshToken');
       localStorage.removeItem('user');
       setUser(null);
     }

     return (
       <AuthContext.Provider
         value={{
           user,
           isAuthenticated: user !== null,
           isLoading,
           login,
           register,
           logout,
         }}
       >
         {children}
       </AuthContext.Provider>
     );
   }
   ```

2. Wire `AuthProvider` into `frontend/src/main.tsx` — wrap the app inside `AuthProvider` (inside `QueryClientProvider`):
   ```tsx
   import { StrictMode } from 'react';
   import { createRoot } from 'react-dom/client';
   import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
   import { AuthProvider } from '@/context/AuthContext.tsx';
   import { App } from './App.tsx';
   import './index.css';

   const queryClient = new QueryClient({
     defaultOptions: {
       queries: {
         staleTime: 1000 * 60 * 5,
         retry: 1,
       },
     },
   });

   createRoot(document.getElementById('root')!).render(
     <StrictMode>
       <QueryClientProvider client={queryClient}>
         <AuthProvider>
           <App />
         </AuthProvider>
       </QueryClientProvider>
     </StrictMode>,
   );
   ```

3. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- Auth state survives a hard browser refresh (loaded from `localStorage` on mount)
- `isLoading` is `true` until the initial localStorage read completes
- `logout` attempts token revocation before clearing local state

---

### Task 20 — Frontend: useAuth Hook

**Status:** Done

**Description:**
Create `src/hooks/useAuth.ts` — a simple hook that consumes `AuthContext` and throws a descriptive error if used outside the provider.

**Steps:**

1. Create `frontend/src/hooks/useAuth.ts`:
   ```typescript
   import { useContext } from 'react';
   import { AuthContext } from '@/context/AuthContext.tsx';

   export function useAuth() {
     const context = useContext(AuthContext);

     if (!context) {
       throw new Error('useAuth must be used within an AuthProvider.');
     }

     return context;
   }
   ```

2. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- Hook throws a clear error when used outside `AuthProvider`
- Returns the full `AuthContextValue` type — no `any`

---

### Task 21 — Frontend: Auth Zod Schemas

**Status:** Done

**Description:**
Create the Zod schemas for the login and register forms. Co-locate them in `src/features/auth/schemas.ts`. Infer the form data types from the schemas.

**Steps:**

1. Create `frontend/src/features/auth/schemas.ts`:
   ```typescript
   import { z } from 'zod';

   export const loginSchema = z.object({
     email: z
       .string()
       .min(1, 'Email is required.')
       .email('Must be a valid email address.'),
     password: z
       .string()
       .min(1, 'Password is required.'),
   });

   export const registerSchema = z.object({
     email: z
       .string()
       .min(1, 'Email is required.')
       .email('Must be a valid email address.'),
     password: z
       .string()
       .min(8, 'Password must be at least 8 characters.')
       .max(100, 'Password cannot exceed 100 characters.')
       .regex(/[A-Z]/, 'Password must contain at least one uppercase letter.')
       .regex(/[a-z]/, 'Password must contain at least one lowercase letter.')
       .regex(/[0-9]/, 'Password must contain at least one digit.'),
     confirmPassword: z
       .string()
       .min(1, 'Please confirm your password.'),
     firstName: z
       .string()
       .min(1, 'First name is required.')
       .max(100, 'First name cannot exceed 100 characters.'),
     lastName: z
       .string()
       .min(1, 'Last name is required.')
       .max(100, 'Last name cannot exceed 100 characters.'),
   }).refine((data) => data.password === data.confirmPassword, {
     message: 'Passwords do not match.',
     path: ['confirmPassword'],
   });

   export type LoginFormData = z.infer<typeof loginSchema>;
   export type RegisterFormData = z.infer<typeof registerSchema>;
   ```

2. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- `loginSchema` and `registerSchema` export matching inferred types
- `confirmPassword` is validated with `.refine()` — not sent to the API

---

### Task 22 — Frontend: LoginPage

**Status:** Done

**Description:**
Create `src/features/auth/LoginPage.tsx`. Uses React Hook Form + Zod, shows inline field errors, shows a loading state during submission, displays a server-level error if credentials are invalid, and redirects to `/` if the user is already authenticated.

**Steps:**

1. Create `frontend/src/features/auth/LoginPage.tsx`:
   ```tsx
   import { useEffect, useState } from 'react';
   import { Link, useNavigate } from 'react-router-dom';
   import { useForm } from 'react-hook-form';
   import { zodResolver } from '@hookform/resolvers/zod';
   import { useAuth } from '@/hooks/useAuth';
   import { loginSchema } from '@/features/auth/schemas';
   import type { LoginFormData } from '@/features/auth/schemas';

   export function LoginPage() {
     const { login, isAuthenticated } = useAuth();
     const navigate = useNavigate();
     const [serverError, setServerError] = useState<string | null>(null);

     useEffect(() => {
       if (isAuthenticated) navigate('/', { replace: true });
     }, [isAuthenticated, navigate]);

     const {
       register,
       handleSubmit,
       formState: { errors, isSubmitting },
     } = useForm<LoginFormData>({
       resolver: zodResolver(loginSchema),
     });

     async function onSubmit(data: LoginFormData) {
       setServerError(null);
       try {
         await login(data);
         navigate('/', { replace: true });
       } catch {
         setServerError('Invalid email or password. Please try again.');
       }
     }

     return (
       <div className="min-h-[100dvh] flex items-center justify-center bg-gray-50 px-4">
         <div className="w-full max-w-md bg-white rounded-2xl shadow-sm border border-gray-200 p-8">
           <h1 className="text-2xl font-bold text-gray-900 mb-2">Welcome back</h1>
           <p className="text-sm text-gray-500 mb-6">Sign in to your account</p>

           {serverError && (
             <div className="mb-4 rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
               {serverError}
             </div>
           )}

           <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
             <div>
               <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                 Email
               </label>
               <input
                 id="email"
                 type="email"
                 autoComplete="email"
                 {...register('email')}
                 className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
                 placeholder="you@example.com"
               />
               {errors.email && (
                 <p className="mt-1 text-xs text-red-600">{errors.email.message}</p>
               )}
             </div>

             <div>
               <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
                 Password
               </label>
               <input
                 id="password"
                 type="password"
                 autoComplete="current-password"
                 {...register('password')}
                 className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
                 placeholder="••••••••"
               />
               {errors.password && (
                 <p className="mt-1 text-xs text-red-600">{errors.password.message}</p>
               )}
             </div>

             <button
               type="submit"
               disabled={isSubmitting}
               className="w-full rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
             >
               {isSubmitting ? 'Signing in…' : 'Sign in'}
             </button>
           </form>

           <p className="mt-6 text-center text-sm text-gray-500">
             Don&apos;t have an account?{' '}
             <Link to="/register" className="font-medium text-indigo-600 hover:text-indigo-500">
               Create one
             </Link>
           </p>
         </div>
       </div>
     );
   }
   ```

2. Run `npm run lint` — confirm 0 errors.

**Success Criteria:**
- Inline validation errors appear below each field on submit
- Server error banner appears for invalid credentials
- Submit button shows `Signing in…` and is disabled during submission
- Already-authenticated users are immediately redirected to `/`

---

### Task 23 — Frontend: RegisterPage

**Status:** Done

**Description:**
Create `src/features/auth/RegisterPage.tsx`. Same pattern as `LoginPage` — React Hook Form + Zod, inline errors, loading state, server error handling, redirect if already authenticated. Includes the `confirmPassword` field (validated client-side only, not sent to the API).

**Steps:**

1. Create `frontend/src/features/auth/RegisterPage.tsx`:
   ```tsx
   import { useEffect, useState } from 'react';
   import { Link, useNavigate } from 'react-router-dom';
   import { useForm } from 'react-hook-form';
   import { zodResolver } from '@hookform/resolvers/zod';
   import { useAuth } from '@/hooks/useAuth';
   import { registerSchema } from '@/features/auth/schemas';
   import type { RegisterFormData } from '@/features/auth/schemas';

   export function RegisterPage() {
     const { register: registerUser, isAuthenticated } = useAuth();
     const navigate = useNavigate();
     const [serverError, setServerError] = useState<string | null>(null);

     useEffect(() => {
       if (isAuthenticated) navigate('/', { replace: true });
     }, [isAuthenticated, navigate]);

     const {
       register,
       handleSubmit,
       formState: { errors, isSubmitting },
     } = useForm<RegisterFormData>({
       resolver: zodResolver(registerSchema),
     });

     async function onSubmit(data: RegisterFormData) {
       setServerError(null);
       try {
         await registerUser({
           email: data.email,
           password: data.password,
           firstName: data.firstName,
           lastName: data.lastName,
         });
         navigate('/', { replace: true });
       } catch {
         setServerError('This email address is already registered. Please sign in instead.');
       }
     }

     return (
       <div className="min-h-[100dvh] flex items-center justify-center bg-gray-50 px-4 py-8">
         <div className="w-full max-w-md bg-white rounded-2xl shadow-sm border border-gray-200 p-8">
           <h1 className="text-2xl font-bold text-gray-900 mb-2">Create an account</h1>
           <p className="text-sm text-gray-500 mb-6">Start tracking your finances today</p>

           {serverError && (
             <div className="mb-4 rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
               {serverError}
             </div>
           )}

           <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
             <div className="grid grid-cols-2 gap-4">
               <div>
                 <label htmlFor="firstName" className="block text-sm font-medium text-gray-700 mb-1">
                   First name
                 </label>
                 <input
                   id="firstName"
                   type="text"
                   autoComplete="given-name"
                   {...register('firstName')}
                   className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
                   placeholder="Jane"
                 />
                 {errors.firstName && (
                   <p className="mt-1 text-xs text-red-600">{errors.firstName.message}</p>
                 )}
               </div>

               <div>
                 <label htmlFor="lastName" className="block text-sm font-medium text-gray-700 mb-1">
                   Last name
                 </label>
                 <input
                   id="lastName"
                   type="text"
                   autoComplete="family-name"
                   {...register('lastName')}
                   className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
                   placeholder="Doe"
                 />
                 {errors.lastName && (
                   <p className="mt-1 text-xs text-red-600">{errors.lastName.message}</p>
                 )}
               </div>
             </div>

             <div>
               <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                 Email
               </label>
               <input
                 id="email"
                 type="email"
                 autoComplete="email"
                 {...register('email')}
                 className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
                 placeholder="you@example.com"
               />
               {errors.email && (
                 <p className="mt-1 text-xs text-red-600">{errors.email.message}</p>
               )}
             </div>

             <div>
               <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
                 Password
               </label>
               <input
                 id="password"
                 type="password"
                 autoComplete="new-password"
                 {...register('password')}
                 className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
                 placeholder="Min. 8 characters"
               />
               {errors.password && (
                 <p className="mt-1 text-xs text-red-600">{errors.password.message}</p>
               )}
             </div>

             <div>
               <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700 mb-1">
                 Confirm password
               </label>
               <input
                 id="confirmPassword"
                 type="password"
                 autoComplete="new-password"
                 {...register('confirmPassword')}
                 className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
                 placeholder="••••••••"
               />
               {errors.confirmPassword && (
                 <p className="mt-1 text-xs text-red-600">{errors.confirmPassword.message}</p>
               )}
             </div>

             <button
               type="submit"
               disabled={isSubmitting}
               className="w-full rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
             >
               {isSubmitting ? 'Creating account…' : 'Create account'}
             </button>
           </form>

           <p className="mt-6 text-center text-sm text-gray-500">
             Already have an account?{' '}
             <Link to="/login" className="font-medium text-indigo-600 hover:text-indigo-500">
               Sign in
             </Link>
           </p>
         </div>
       </div>
     );
   }
   ```

2. Run `npm run lint` — confirm 0 errors.

**Success Criteria:**
- `confirmPassword` validated client-side only — not included in the `registerUser` call
- All five fields display inline errors
- Server error shown when API returns a conflict

---

### Task 24 — Frontend: ProtectedRoute Component

**Status:** Done

**Description:**
Create `src/components/layout/ProtectedRoute.tsx`. While `isLoading` is true (initial localStorage read), render nothing to prevent a flash of the login redirect. Once loaded, redirect to `/login` if not authenticated, or render the child outlet.

**Steps:**

1. Create `frontend/src/components/layout/ProtectedRoute.tsx`:
   ```tsx
   import { Navigate, Outlet } from 'react-router-dom';
   import { useAuth } from '@/hooks/useAuth';

   export function ProtectedRoute() {
     const { isAuthenticated, isLoading } = useAuth();

     if (isLoading) return null;

     if (!isAuthenticated) return <Navigate to="/login" replace />;

     return <Outlet />;
   }
   ```

2. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- No flash of redirect during initial auth state hydration
- Unauthenticated users are redirected to `/login` with `replace` (no back-button loop)

---

### Task 25 — Frontend: Update Router

**Status:** Done

**Description:**
Update `src/routes/index.tsx` to add the public `/login` and `/register` routes, and wrap the existing main layout route with `ProtectedRoute`.

**Steps:**

1. Replace `frontend/src/routes/index.tsx`:
   ```tsx
   import { createBrowserRouter } from 'react-router-dom';
   import { MainLayout } from '@/components/layout/MainLayout.tsx';
   import { ProtectedRoute } from '@/components/layout/ProtectedRoute.tsx';
   import { NotFoundPage } from '@/pages/NotFoundPage.tsx';
   import { LoginPage } from '@/features/auth/LoginPage.tsx';
   import { RegisterPage } from '@/features/auth/RegisterPage.tsx';

   const router = createBrowserRouter([
     {
       path: '/login',
       element: <LoginPage />,
     },
     {
       path: '/register',
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
               element: <div className="text-gray-500">Dashboard — coming in Sprint 4</div>,
             },
             {
               path: 'transactions',
               element: <div className="text-gray-500">Transactions — coming in Sprint 2</div>,
             },
             {
               path: 'categories',
               element: <div className="text-gray-500">Categories — coming in Sprint 2</div>,
             },
             {
               path: 'budgets',
               element: <div className="text-gray-500">Budgets — coming in Sprint 3</div>,
             },
             {
               path: 'reports',
               element: <div className="text-gray-500">Reports — coming in Sprint 4</div>,
             },
             { path: '*', element: <NotFoundPage /> },
           ],
         },
       ],
     },
   ]);

   export default router;
   ```

2. Run `npm run build` — confirm 0 TypeScript errors.

**Success Criteria:**
- `/login` and `/register` are accessible without authentication
- All other routes redirect to `/login` when not authenticated
- `ProtectedRoute` wraps `MainLayout` — not individual routes

---

### Task 26 — Frontend: Update Header with User Info and Logout

**Status:** Done

**Description:**
Update `src/components/layout/Header.tsx` to show the logged-in user's first name and a logout button. Uses `useAuth` for the user object and the `logout` function.

**Steps:**

1. Replace `frontend/src/components/layout/Header.tsx`:
   ```tsx
   import { LogOut, User } from 'lucide-react';
   import { useAuth } from '@/hooks/useAuth';

   export function Header() {
     const { user, logout } = useAuth();

     async function handleLogout() {
       await logout();
     }

     return (
       <header className="h-14 bg-white border-b border-gray-200 flex items-center justify-between px-6">
         <h2 className="text-sm font-medium text-gray-500">Personal Finance Tracker</h2>

         {user && (
           <div className="flex items-center gap-3">
             <div className="flex items-center gap-2 text-sm text-gray-700">
               <User className="w-4 h-4 text-gray-400" aria-hidden="true" />
               <span>{user.firstName} {user.lastName}</span>
             </div>
             <button
               onClick={handleLogout}
               aria-label="Sign out"
               className="flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm text-gray-600 hover:bg-gray-100 hover:text-gray-900 transition-colors"
             >
               <LogOut className="w-4 h-4" aria-hidden="true" />
               <span>Sign out</span>
             </button>
           </div>
         )}
       </header>
     );
   }
   ```

2. Run `npm run build` and `npm run lint` — confirm 0 errors.

**Success Criteria:**
- Logged-in user's full name visible in the header
- Sign out button calls `logout()` which revokes the refresh token, clears localStorage, and the `ProtectedRoute` then redirects to `/login`
- Icons use Lucide React — no other icon library

---

## Aditional steps added by Me:
- Make use of the ioptions páttern
- Create an abstraction to return a respopnse to the client in the same formart for all endpoints
- Create an abstraction for services response 
- Create an abstraction for repositories response
- Verify project structure
- show validation errros form server
- Modify ApiError class as  the same format of ProblemDetails object from server
- Modify middleware to return same format response

# Progress:
- Until Task 14 Done

## Definition of Done

This sprint is complete when:

- [ ] `dotnet build Personal.FinanceTracker.slnx` passes with 0 errors and 0 warnings
- [ ] `dotnet run --project backend/src/Personal.FinanceTracker.Api` starts successfully
- [ ] `GET /health/live` returns HTTP 200
- [ ] Swagger UI shows the `Authentication` group with four endpoints: register, login, refresh, revoke
- [ ] `POST /api/auth/register` creates a user and returns `{ accessToken, refreshToken, expiresIn, user }`
- [ ] `POST /api/auth/register` with a duplicate email returns HTTP 409 Conflict
- [ ] `POST /api/auth/login` returns tokens for valid credentials
- [ ] `POST /api/auth/login` returns HTTP 401 for invalid credentials
- [ ] `POST /api/auth/refresh` returns a new token pair and revokes the old refresh token
- [ ] `POST /api/auth/revoke` returns HTTP 204 and marks the refresh token as revoked
- [ ] EF Core migration `InitialUsersSchema` runs cleanly against a local PostgreSQL instance
- [ ] Tables `users.users` and `users.refresh_tokens` exist with correct columns and indexes
- [ ] `npm run build` passes with 0 TypeScript errors
- [ ] `npm run lint` passes with 0 ESLint errors
- [ ] `npm run dev` shows the login page for unauthenticated users
- [ ] Submitting the login form with valid credentials redirects to the dashboard shell
- [ ] Submitting the login form with invalid credentials shows a server error banner
- [ ] Submitting the register form with an existing email shows a server error banner
- [ ] All register form validation rules surface as inline errors before submission
- [ ] Refreshing the browser while logged in does not log the user out
- [ ] The Header shows the logged-in user's name and a Sign out button
- [ ] Clicking Sign out clears tokens, calls `/api/auth/revoke`, and redirects to `/login`
- [ ] `src/api/client.ts` uses the native `fetch` API — no `axios` dependency
- [ ] All 26 tasks are in **Done** status
- [ ] `designer-enforcer` agent has been invoked and its report is clean

---

*Last updated: 25/05/2026*
