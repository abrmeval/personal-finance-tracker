# Personal Finance Tracker - Project Structure

> **Version 1.1** | Modular Monolith Architecture  
> ASP.NET 10 • React • Neon PostgreSQL • Azure

---

## 1. Architecture Overview

The Personal Finance Tracker follows a **Modular Monolith** architecture pattern. This approach provides the organizational benefits of microservices while maintaining the simplicity of a monolithic deployment.

Each module is self-contained with its own domain logic, data access, and API endpoints, but they all run within a single deployable unit.

### Key Benefits

| Benefit | Description |
|---------|-------------|
| **Clear Boundaries** | Each module owns its domain, preventing tight coupling |
| **Simple Deployment** | Single deployment unit reduces operational complexity |
| **Easy Refactoring** | Modules can be extracted to microservices when needed |
| **Schema Isolation** | Each module has its own database schema for data isolation |
| **Shared Infrastructure** | Common concerns like auth and logging are centralized |

### When to Consider Microservices

Extract a module to a microservice when:
- It requires independent scaling
- Different deployment cadence is needed
- Team ownership boundaries require separation
- Technology stack needs to differ

---

## 2. Module Overview

The application is organized into three core modules:

```
┌─────────────────────────────────────────────────────────────┐
│                     Azure App Service                        │
│  ┌─────────────────────────────────────────────────────┐    │
│  │              Personal Finance Tracker API            │    │
│  │  ┌─────────────┐ ┌─────────────┐ ┌──────────────┐   │    │
│  │  │   Finance   │ │    Users    │ │  Reporting   │   │    │
│  │  │   Module    │ │   Module    │ │   Module     │   │    │
│  │  └──────┬──────┘ └──────┬──────┘ └──────┬───────┘   │    │
│  └─────────┼───────────────┼───────────────┼───────────┘    │
└────────────┼───────────────┼───────────────┼────────────────┘
             │               │               │
             ▼               ▼               ▼
┌─────────────────────────────────────────────────────────────┐
│                   Neon PostgreSQL Database                   │
│  ┌─────────────┐ ┌─────────────┐ ┌──────────────┐           │
│  │ finances.*  │ │   users.*   │ │  reports.*   │           │
│  └─────────────┘ └─────────────┘ └──────────────┘           │
└─────────────────────────────────────────────────────────────┘
```

### Module Responsibilities

| Module | Responsibility | Database Schema | Key Entities |
|--------|---------------|-----------------|--------------|
| **Finance** | Core financial operations | `finances.*` | Transaction, Category, Budget |
| **Users** | Identity & authentication | `users.*` | User, RefreshToken |
| **Reporting** | Analytics & dashboards | `reports.*` | MonthlySummary, CategoryReport |

---

## 3. Solution Structure

### Root Directory Layout

```
PersonalFinanceTracker/
│
├── 📁 backend/                          # All .NET code
│   ├── 📄 PersonalFinanceTracker.sln    # Solution file (covers all .NET projects)
│   ├── 📄 Directory.Build.props         # Shared build properties
│   │
│   ├── 📁 src/
│   │   ├── 📁 PersonalFinanceTracker.Api/    # Main API host (startup project)
│   │   ├── 📁 PersonalFinanceTracker.Shared/ # Shared kernel library
│   │   └── 📁 Modules/                       # Feature modules
│   │       ├── 📁 Finance/
│   │       ├── 📁 Users/
│   │       └── 📁 Reporting/
│   │
│   └── 📁 tests/                        # Test projects
│       ├── 📁 Finance.UnitTests/
│       ├── 📁 Finance.IntegrationTests/
│       ├── 📁 Users.UnitTests/
│       └── 📁 Api.IntegrationTests/
│
├── 📁 frontend/                         # All React/frontend code
│   ├── 📁 src/
│   ├── 📄 package.json
│   ├── 📄 vite.config.ts
│   └── 📄 tsconfig.json
│
├── 📁 .github/                          # CI/CD workflows
│   └── 📁 workflows/
│       ├── 📄 api-deploy.yml
│       └── 📄 frontend-deploy.yml
│
├── 📁 docs/                             # Documentation
├── 📄 .gitignore
└── 📄 README.md
```

### Solution File Organization

The solution file (`PersonalFinanceTracker.sln`) is located at the root of the `backend/` folder and includes all .NET projects:

```xml
<!-- backend/PersonalFinanceTracker.sln structure -->
Solution
├── src
│   ├── PersonalFinanceTracker.Api
│   ├── PersonalFinanceTracker.Shared
│   └── Modules
│       ├── Finance
│       ├── Users
│       └── Reporting
└── tests
    ├── Finance.UnitTests
    ├── Finance.IntegrationTests
    └── Api.IntegrationTests
```

---

## 4. Module Internal Structure

Each module follows a consistent layered structure:

### Finance Module Example

```
backend/src/Modules/Users/          ← Implemented (Sprint 1)
│
├── 📁 Domain/
│   ├── 📁 Entities/
│   │   ├── 📄 User.cs
│   │   └── 📄 RefreshToken.cs
│   └── 📁 Interfaces/
│       ├── 📄 IRepository.cs       ← Generic base interface
│       └── 📄 IUserRepository.cs
│
├── 📁 Application/
│   ├── 📁 DTOs/
│   │   ├── 📁 Requests/
│   │   │   ├── 📄 LoginRequest.cs
│   │   │   ├── 📄 RegisterRequest.cs
│   │   │   └── 📄 RefreshTokenRequest.cs
│   │   └── 📁 Responses/
│   │       ├── 📄 AuthResponse.cs
│   │       └── 📄 UserResponse.cs
│   ├── 📁 Interfaces/             ← Service + settings contracts
│   │   ├── 📄 IJwtSettings.cs
│   │   ├── 📄 ITokenService.cs
│   │   └── 📄 IUserService.cs
│   └── 📁 Validators/
│       ├── 📄 LoginRequestValidator.cs
│       └── 📄 RegisterRequestValidator.cs
│
├── 📁 Infrastructure/
│   ├── 📁 Configuration/
│   │   └── 📄 JwtSettings.cs      ← IOptions<JwtSettings> binding
│   ├── 📁 Data/
│   │   ├── 📄 UsersDbContext.cs
│   │   ├── 📁 Configurations/
│   │   │   ├── 📄 UserConfiguration.cs
│   │   │   └── 📄 RefreshTokenConfiguration.cs
│   │   └── 📁 Migrations/
│   │       └── 📄 20260519200017_InitialUsersSchema.cs
│   ├── 📁 Repositories/
│   │   └── 📄 UserRepository.cs
│   └── 📁 Services/
│       ├── 📄 TokenService.cs     ← Infrastructure; implements ITokenService
│       └── 📄 UserService.cs      ← Infrastructure; implements IUserService
│
├── 📁 Api/
│   └── 📁 Endpoints/
│       └── 📄 AuthEnpoints.cs     ← Note: filename typo (missing 'd')
│
└── 📄 DependencyInjection.cs      ← AddUsersModule + MapUsersEndpoints
```

> **Note on service placement:** `UserService` and `TokenService` both live in `Infrastructure/Services/`
> because they depend on infrastructure concerns — BCrypt (external library) and `IOptions<JwtSettings>`
> respectively. Their contracts (`IUserService`, `ITokenService`) remain in `Application/Interfaces/`,
> maintaining correct dependency flow.

### Finance Module Example (planned — Sprint 2)

```
backend/src/Modules/Finance/
│
├── 📁 Domain/                           # Core business logic (no dependencies)
│   ├── 📁 Entities/
│   │   ├── 📄 Transaction.cs
│   │   ├── 📄 Category.cs
│   │   └── 📄 Budget.cs
│   ├── 📁 Enums/
│   │   ├── 📄 TransactionType.cs
│   │   └── 📄 BudgetPeriod.cs
│   └── 📁 Interfaces/
│       ├── 📄 ITransactionRepository.cs
│       └── 📄 IBudgetRepository.cs
│
├── 📁 Application/                      # Use cases and business rules
│   ├── 📁 Services/
│   │   ├── 📄 TransactionService.cs
│   │   ├── 📄 CategoryService.cs
│   │   └── 📄 BudgetService.cs
│   ├── 📁 DTOs/
│   │   ├── 📁 Requests/
│   │   │   ├── 📄 CreateTransactionRequest.cs
│   │   │   └── 📄 UpdateBudgetRequest.cs
│   │   └── 📁 Responses/
│   │       ├── 📄 TransactionResponse.cs
│   │       └── 📄 BudgetSummaryResponse.cs
│   ├── 📁 Validators/
│   │   ├── 📄 CreateTransactionValidator.cs
│   │   └── 📄 UpdateBudgetValidator.cs
│   └── 📁 Mapping/
│       └── 📄 FinanceMappingProfile.cs
│
├── 📁 Infrastructure/                   # External concerns (DB, external services)
│   ├── 📁 Data/
│   │   ├── 📄 FinanceDbContext.cs
│   │   └── 📁 Configurations/
│   │       ├── 📄 TransactionConfiguration.cs
│   │       ├── 📄 CategoryConfiguration.cs
│   │       └── 📄 BudgetConfiguration.cs
│   ├── 📁 Repositories/
│   │   ├── 📄 TransactionRepository.cs
│   │   └── 📄 BudgetRepository.cs
│   └── 📁 Migrations/
│       └── 📄 (EF Core migrations)
│
├── 📁 Api/                              # HTTP layer
│   └── 📁 Endpoints/
│       ├── 📄 TransactionEndpoints.cs
│       ├── 📄 CategoryEndpoints.cs
│       └── 📄 BudgetEndpoints.cs
│
├── 📄 FinanceModule.cs                  # Module registration & DI setup
└── 📄 Finance.csproj                    # Project file
```

### Layer Responsibilities

| Layer | Purpose | Dependencies |
|-------|---------|--------------|
| **Domain** | Entities, value objects, domain interfaces | None (pure C#) |
| **Application** | Services, DTOs, validators, use cases | Domain |
| **Infrastructure** | EF Core, repositories, external integrations | Domain, Application |
| **Api** | Minimal API endpoints, request handling | Application |

---

## 5. Project Dependencies

### Dependency Rules

```
┌─────────────────────────────────────────────────────────────┐
│                PersonalFinanceTracker.Api                    │
│            (References all modules + Shared)                 │
└─────────────────────────┬───────────────────────────────────┘
                          │
          ┌───────────────┼───────────────┐
          │               │               │
          ▼               ▼               ▼
    ┌──────────┐   ┌──────────┐   ┌────────────┐
    │ Finance  │   │  Users   │   │ Reporting  │
    │  Module  │   │  Module  │   │   Module   │
    └────┬─────┘   └────┬─────┘   └─────┬──────┘
         │              │               │
         └──────────────┼───────────────┘
                        │
                        ▼
          ┌─────────────────────────┐
          │ PersonalFinanceTracker  │
          │        .Shared          │
          └─────────────────────────┘
```

### Allowed References

| Project | Can Reference |
|---------|---------------|
| `Api` | All Modules, Shared |
| `Finance` | Shared only |
| `Users` | Shared only |
| `Reporting` | Shared, Finance (read-only contracts) |
| `Shared` | None (leaf dependency) |

### Inter-Module Communication

Modules should **not** directly reference each other's internal types. Use these patterns:

1. **Shared Contracts**: Define interfaces/DTOs in `Shared` project
2. **Integration Events**: Publish domain events for cross-module communication
3. **API Calls**: For complex scenarios, use internal HTTP calls (rare)

```csharp
// ❌ Wrong: Direct module reference
using Finance.Domain.Entities;

// ✅ Correct: Use shared contracts
using PersonalFinanceTracker.Shared.Contracts;
```

---

## 6. Shared Kernel (PersonalFinanceTracker.Shared)

The Shared project contains cross-cutting concerns:

```
backend/src/Personal.FinanceTracker.Shared/
│
├── 📁 Abstractions/
│   └── 📄 Entity.cs                    ← Abstract base: Id, CreatedAt, UpdatedAt
│
├── 📁 Exceptions/
│   └── 📄 NotFoundException.cs
│
├── 📁 Extensions/
│   └── 📄 ClaimsPrincipalExtensions.cs ← GetUserId(), GetEmail()
│
├── 📁 Filters/
│   └── 📄 ValidationFilter.cs          ← IEndpointFilter wrapping FluentValidation
│
├── 📁 Middleware/
│   └── 📄 ExceptionHandlingMiddleware.cs
│
└── 📁 Models/
    ├── 📄 ApiError.cs                  ← Extends ProblemDetails; adds Context, ModelErrors
    ├── 📄 ApiResponse.cs               ← ApiResponse<T>: IsOk, Data, Error, StatusCode, CodeText
    └── 📄 Result.cs                    ← Result<T>: IsSuccess, Value, Error; ErrorResult record
```

---

## 7. API Host Project

The main entry point that composes all modules:

```
backend/src/Personal.FinanceTracker.Api/
│
├── 📄 Program.cs                        # Application bootstrap
├── 📄 appsettings.json                  # Base configuration
├── 📄 appsettings.Development.json      # Dev overrides
└── 📄 appsettings.Local.json            # Local secrets (gitignored)
```

### Program.cs Structure

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add shared services
builder.Services.AddSharedServices(builder.Configuration);

// Register modules
builder.Services.AddFinanceModule(builder.Configuration);
builder.Services.AddUsersModule(builder.Configuration);
builder.Services.AddReportingModule(builder.Configuration);

// Add cross-cutting concerns
builder.Services.AddOpenTelemetry();
builder.Services.AddSwagger();
builder.Services.AddAuthentication();

var app = builder.Build();

// Configure middleware pipeline
app.UseExceptionHandling();
app.UseAuthentication();
app.UseAuthorization();

// Map module endpoints
app.MapFinanceEndpoints();
app.MapUsersEndpoints();
app.MapReportingEndpoints();

app.Run();
```

---

## 8. Configuration Files

### Directory.Build.props

Shared MSBuild properties for all projects:

```xml
<!-- backend/Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="...">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

### .editorconfig (Code Style)

```ini
root = true

[*.cs]
indent_style = space
indent_size = 4
dotnet_sort_system_directives_first = true
csharp_style_var_for_built_in_types = true
csharp_style_var_when_type_is_apparent = true
```

---

## 9. References

- [Microsoft: Modular Monolith Architecture](https://learn.microsoft.com/en-us/dotnet/architecture/)
- [Milan Jovanovic: Modular Monolith Primer](https://www.milanjovanovic.tech/blog/what-is-a-modular-monolith)
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)

---

*Next: [02-Backend-Documentation.md](./02-Backend-Documentation.md)*
