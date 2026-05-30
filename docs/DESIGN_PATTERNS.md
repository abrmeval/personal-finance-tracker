# Backend Design Patterns — Personal Finance Tracker

> This document catalogs the design patterns found in the backend codebase.
> It is derived from the actual implemented code, not aspirational docs.
> All patterns listed here are in use and should be followed when adding new modules.

---

## 1. Modular Monolith

Each feature domain is a self-contained class library project under `backend/src/Modules/`. Modules share nothing except the `Personal.FinanceTracker.Shared` kernel. They communicate only through shared contracts, never by direct project-to-project references between modules.

```
backend/src/
├── Personal.FinanceTracker.Api/       ← Host — composes all modules
├── Personal.FinanceTracker.Shared/    ← Shared kernel (no module references)
└── Modules/
    ├── Users/                         ← Self-contained module
    ├── Finance/                       ← (planned)
    └── Reporting/                     ← (planned)
```

**Rule:** `Api` → `Modules` → `Shared`. Modules never reference each other.

---

## 2. Clean Architecture (Layered Module Structure)

Every module follows the same four-layer structure. Dependencies always flow inward.

```
Domain          ← No dependencies. Entities, domain interfaces.
   ↑
Application     ← Depends on Domain. Services (interfaces), DTOs, validators.
   ↑
Infrastructure  ← Depends on Domain + Application. EF Core, external libs, service impls.
   ↑
Api             ← Depends on Application. Minimal API endpoint handlers only.
```

**Concrete example (Users module):**

| Layer | Contents |
|-------|----------|
| `Domain/Entities/` | `User`, `RefreshToken` |
| `Domain/Interfaces/` | `IUserRepository`, `IRepository<T>` |
| `Application/Interfaces/` | `IUserService`, `ITokenService`, `IJwtSettings` |
| `Application/DTOs/` | `RegisterRequest`, `AuthResponse`, etc. |
| `Application/Validators/` | `RegisterRequestValidator`, `LoginRequestValidator` |
| `Infrastructure/Services/` | `UserService`, `TokenService` |
| `Infrastructure/Repositories/` | `UserRepository` |
| `Infrastructure/Data/` | `UsersDbContext`, entity configurations, migrations |
| `Infrastructure/Configuration/` | `JwtSettings` |
| `Api/Endpoints/` | `AuthEndpoints` |

---

## 3. Service Placement Rule (Infrastructure Services)

Services that depend on **infrastructure concerns** (external libraries, EF Core, options config) live in `Infrastructure/Services/`, not `Application/Services/`. Their contracts (interfaces) always remain in `Application/Interfaces/`.

**Why:** `UserService` uses `BCrypt.Net.BCrypt` (external library) and depends on `IUserRepository` and `ITokenService`. `TokenService` depends on `IOptions<JwtSettings>`. Both are infrastructure concerns.

```
Application/Interfaces/IUserService.cs      ← contract (Application layer)
Infrastructure/Services/UserService.cs      ← implementation (Infrastructure layer)

Application/Interfaces/ITokenService.cs     ← contract
Infrastructure/Services/TokenService.cs     ← implementation
```

This mirrors the repository pattern exactly and satisfies the architecture rule:
> "Infrastructure implements Application interfaces."

---

## 4. Repository Pattern

Domain-defined interfaces in `Domain/Interfaces/`; EF Core implementations in `Infrastructure/Repositories/`.

```csharp
// Domain/Interfaces/IUserRepository.cs
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

**Rules:**
- Single-entity lookups return `T?` (nullable) — never throw for not-found.
- `CancellationToken` passed through to all EF Core async calls.
- `SaveChangesAsync` is explicit on the repository — not automatically called by `AddAsync`.

A generic `IRepository<T>` base interface lives in `Domain/Interfaces/IRepository.cs`. Module-specific repositories extend it and add domain-specific methods.

---

## 5. Domain Entity Pattern

All entities use:
- `private` constructor (prevents direct instantiation)
- Static `Create(...)` factory method (validates inputs, constructs the entity)
- `private set` on all properties
- Extension of `Personal.FinanceTracker.Shared.Abstractions.Entity` (provides `Id`, `CreatedAt`, `UpdatedAt`)

```csharp
public sealed class User : Entity
{
    public string Email { get; private set; } = string.Empty;
    // ... other properties

    private User() { }   // ← EF Core constructor

    public static User Create(string email, string passwordHash, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        // ... other guards

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant().Trim(),
            // ...
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdatePassword(string newPasswordHash) { ... }
}
```

`RefreshToken` does not extend `Entity` — it has no `UpdatedAt` semantics. It uses the same private constructor + static `Create` pattern, plus a `Revoke()` method for the one-way revocation operation.

---

## 6. Result Pattern

Services return `Result<T>` instead of nullable types or throwing exceptions for expected business failures. This makes outcomes explicit at every call site.

```csharp
// Shared/Models/Result.cs
public class Result<T>
{
    public bool IsSuccess { get; init; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; init; }
    public ErrorResult? Error { get; init; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(ErrorResult error) => new() { IsSuccess = false, Error = error };
}

public sealed record ErrorResult(string Code, string Description)
{
    public static readonly ErrorResult None = new(string.Empty, string.Empty);
}
```

**Usage:**

```csharp
// In a service:
if (await repository.EmailExistsAsync(request.Email, ct))
    return Result<AuthResponse>.Failure(new("RESOURCE_ALREADY_EXISTS", "An account with this email already exists."));

return Result<AuthResponse>.Success(BuildAuthResponse(user, refreshTokenValue).Value!);

// In an endpoint:
var result = await userService.RegisterAsync(request, ct);

if (result.IsFailure)
    return TypedResults.Conflict(new ApiResponse<AuthResponse> { IsOk = false, Error = ..., });

return TypedResults.Ok(new ApiResponse<AuthResponse> { IsOk = true, Data = result.Value });
```

**When to use `Result<T>` vs exceptions:**
- Expected business failures (duplicate email, invalid credentials, token revoked) → `Result<T>.Failure`
- Programming errors or truly invalid domain state → `ArgumentException` in factory methods
- Not-found cases that map to 404 → `NotFoundException` (caught by `ExceptionHandlingMiddleware`)

---

## 7. API Response Envelope Pattern

All endpoints return a consistent `ApiResponse<T>` wrapper. Clients always receive the same shape regardless of success or failure.

```csharp
// Shared/Models/ApiResponse.cs
public class ApiResponse<T>
{
    public bool IsOk { get; init; }
    public T? Data { get; init; }
    public ApiError? Error { get; init; }
    public int StatusCode { get; init; }
    public string CodeText { get; init; } = string.Empty;
}
```

```csharp
// Shared/Models/ApiError.cs — extends ProblemDetails
public class ApiError : ProblemDetails
{
    public string? Context { get; init; }
    public Dictionary<string, string[]>? ModelErrors { get; init; }
}
```

**Frontend mirror:** `src/types/http.ts` defines `ApiResponse<T>` as an interface matching this shape exactly.

---

## 8. Options Pattern (Strongly-Typed Configuration)

Configuration sections are bound to typed classes via `IOptions<T>`, not read as raw strings from `IConfiguration`.

```csharp
// Infrastructure/Configuration/JwtSettings.cs
public sealed class JwtSettings : IJwtSettings
{
    public const string SectionName = "Jwt";
    public string SecretKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int ExpiryMinutes { get; init; } = 60;
    public int RefreshTokenExpiryDays { get; init; } = 7;
}
```

Registration:

```csharp
services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
services.AddSingleton<IJwtSettings>(sp => sp.GetRequiredService<IOptions<JwtSettings>>().Value);
```

The interface (`IJwtSettings`) lives in `Application/Interfaces/`. The concrete class (`JwtSettings`) lives in `Infrastructure/Configuration/`. Services depend on `IJwtSettings`, not on `IConfiguration` string indexers.

---

## 9. Module Self-Registration Pattern

Each module exposes two extension methods on a static `DependencyInjection` class at the module root:
- `AddXxxModule(IServiceCollection, IConfiguration)` — registers all DI services
- `MapXxxEndpoints(IEndpointRouteBuilder)` — maps all endpoint groups

```csharp
// Users/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddUsersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<UsersDbContext>(...);
        services.Configure<JwtSettings>(...);
        services.AddSingleton<IJwtSettings>(...);
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();
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

`Program.cs` remains clean — it only calls `builder.Services.AddUsersModule(...)` and `app.MapUsersEndpoints()`.

---

## 10. Minimal API Endpoint Pattern

Endpoints are static classes with private static handler methods. No business logic in handlers — they delegate entirely to the service layer.

```csharp
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>();

        group.MapPost("/revoke", RevokeAsync)
            .WithName("RevokeToken")
            .RequireAuthorization();

        return app;
    }

    private static async Task<Results<Ok<ApiResponse<AuthResponse>>, Conflict<ApiResponse<AuthResponse>>>>
        RegisterAsync(RegisterRequest request, IUserService userService, HttpContext httpContext, CancellationToken ct)
    {
        var result = await userService.RegisterAsync(request, ct);

        if (result.IsFailure)
            return TypedResults.Conflict(new ApiResponse<AuthResponse> { IsOk = false, Error = ..., });

        return TypedResults.Ok(new ApiResponse<AuthResponse> { IsOk = true, Data = result.Value });
    }
}
```

**Rules:**
- Always use `TypedResults` (not `Results`) for full OpenAPI type inference.
- Apply `ValidationFilter<TRequest>` via `.AddEndpointFilter<>()` on mutating endpoints.
- Group endpoints under `.MapGroup(...)` with `.WithTags(...)` for Swagger grouping.
- `.RequireAuthorization()` applied per-endpoint or at group level as appropriate.

---

## 11. EF Core Configuration Pattern

Entity configurations use `IEntityTypeConfiguration<T>` (Fluent API only — no Data Annotations on entities). Column names are snake_case. `UsersDbContext` uses `HasDefaultSchema("users")` for schema isolation.

```csharp
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(256)
            .IsRequired();
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("ix_users_email");
    }
}
```

Applied via `modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly)`.

Computed properties (e.g., `IsExpired`, `IsActive` on `RefreshToken`) are excluded from mapping with `builder.Ignore(...)`.

---

## 12. Global Exception Handling Middleware

`ExceptionHandlingMiddleware` in the Shared project maps well-known exceptions to RFC 7807 `ProblemDetails` (wrapped in `ApiResponse<object>`):

| Exception | HTTP Status |
|-----------|------------|
| `ValidationException` | 400 |
| `UnauthorizedAccessException` | 401 |
| `NotFoundException` | 404 |
| Unhandled | 500 |

Services and repositories never catch-and-swallow exceptions. They let them propagate to the middleware. The only place to catch is in endpoint handlers when inspecting `Result<T>` outcomes.

---

## 13. FluentValidation Pattern

One `AbstractValidator<T>` per request DTO, named `CreateXxxValidator` or `XxxRequestValidator`. Validators are in `Application/Validators/` and registered via:

```csharp
services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
```

Applied to endpoints via `ValidationFilter<T>`:

```csharp
group.MapPost("/register", RegisterAsync)
    .AddEndpointFilter<ValidationFilter<RegisterRequest>>();
```

Async validators (e.g., DB existence checks) use `.MustAsync(...)`. Duplicate email checks do **not** belong in validators — that is a business rule owned by the service.

---

*Last updated: Sprint 1 completion — 29/05/2026*
