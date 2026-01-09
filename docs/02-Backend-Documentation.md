# Personal Finance Tracker - Backend Documentation

> **ASP.NET 8 Web API** | Minimal APIs • Entity Framework Core • FluentValidation  
> Neon PostgreSQL • OpenTelemetry • TickerQ

---

## 1. Technology Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Framework** | ASP.NET 8 | Web API runtime |
| **API Style** | Minimal APIs | Lightweight, fast endpoints |
| **ORM** | Entity Framework Core 8 | Database access |
| **Database** | Neon PostgreSQL | Serverless Postgres |
| **Validation** | FluentValidation | Request validation |
| **Scheduling** | TickerQ | Background jobs |
| **Observability** | OpenTelemetry | Traces, metrics, logs |
| **Testing** | xUnit + TestContainers | Unit & integration tests |

---

## 2. Minimal APIs Overview

### Why Minimal APIs?

Minimal APIs in ASP.NET 8 provide a lightweight alternative to MVC controllers:

- **Less boilerplate**: No controller classes needed
- **Better performance**: Reduced overhead
- **Native AOT support**: Faster startup times
- **Cleaner code**: Endpoints are simple lambda expressions

### Basic Endpoint Structure

```csharp
// backend/src/Modules/Finance/Api/Endpoints/TransactionEndpoints.cs

public static class TransactionEndpoints
{
    public static void MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transactions")
            .WithTags("Transactions")
            .RequireAuthorization();

        group.MapGet("/", GetAllTransactions)
            .WithName("GetTransactions")
            .WithDescription("Get all transactions for the authenticated user");

        group.MapGet("/{id:guid}", GetTransactionById)
            .WithName("GetTransactionById");

        group.MapPost("/", CreateTransaction)
            .WithName("CreateTransaction")
            .AddEndpointFilter<ValidationFilter<CreateTransactionRequest>>();

        group.MapPut("/{id:guid}", UpdateTransaction)
            .WithName("UpdateTransaction");

        group.MapDelete("/{id:guid}", DeleteTransaction)
            .WithName("DeleteTransaction");
    }

    private static async Task<IResult> GetAllTransactions(
        ITransactionService transactionService,
        ClaimsPrincipal user,
        [AsParameters] TransactionQueryParams query)
    {
        var userId = user.GetUserId();
        var transactions = await transactionService.GetAllAsync(userId, query);
        return Results.Ok(transactions);
    }

    private static async Task<IResult> GetTransactionById(
        Guid id,
        ITransactionService transactionService,
        ClaimsPrincipal user)
    {
        var userId = user.GetUserId();
        var transaction = await transactionService.GetByIdAsync(id, userId);
        
        return transaction is null 
            ? Results.NotFound() 
            : Results.Ok(transaction);
    }

    private static async Task<IResult> CreateTransaction(
        CreateTransactionRequest request,
        ITransactionService transactionService,
        ClaimsPrincipal user)
    {
        var userId = user.GetUserId();
        var transaction = await transactionService.CreateAsync(request, userId);
        
        return Results.CreatedAtRoute(
            "GetTransactionById",
            new { id = transaction.Id },
            transaction);
    }

    private static async Task<IResult> UpdateTransaction(
        Guid id,
        UpdateTransactionRequest request,
        ITransactionService transactionService,
        ClaimsPrincipal user)
    {
        var userId = user.GetUserId();
        var result = await transactionService.UpdateAsync(id, request, userId);
        
        return result ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> DeleteTransaction(
        Guid id,
        ITransactionService transactionService,
        ClaimsPrincipal user)
    {
        var userId = user.GetUserId();
        var result = await transactionService.DeleteAsync(id, userId);
        
        return result ? Results.NoContent() : Results.NotFound();
    }
}
```

### Query Parameters with AsParameters

```csharp
// DTOs/Requests/TransactionQueryParams.cs

public record TransactionQueryParams
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public Guid? CategoryId { get; init; }
    public TransactionType? Type { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
```

---

## 3. Entity Framework Core with Neon PostgreSQL

### DbContext Configuration

```csharp
// backend/src/Modules/Finance/Infrastructure/Data/FinanceDbContext.cs

public class FinanceDbContext : DbContext
{
    public FinanceDbContext(DbContextOptions<FinanceDbContext> options) 
        : base(options) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply schema for module isolation
        modelBuilder.HasDefaultSchema("finances");
        
        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinanceDbContext).Assembly);
    }
}
```

### Entity Configuration (Fluent API)

```csharp
// Infrastructure/Data/Configurations/TransactionConfiguration.cs

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

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
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Date)
            .HasColumnName("date")
            .IsRequired();

        builder.Property(t => t.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.Date);
        builder.HasIndex(t => new { t.UserId, t.Date });
    }
}
```

### Neon PostgreSQL Connection

```csharp
// FinanceModule.cs

public static class FinanceModule
{
    public static IServiceCollection AddFinanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<FinanceDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("FinanceDb"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable(
                        "__EFMigrationsHistory", 
                        "finances");
                    
                    // Enable retry on failure for transient errors
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                    
                    // Command timeout for Neon cold starts
                    npgsqlOptions.CommandTimeout(30);
                });

            // Enable detailed errors in development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Repositories
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();

        // Services
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBudgetService, BudgetService>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<CreateTransactionValidator>();

        return services;
    }

    public static IEndpointRouteBuilder MapFinanceEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapTransactionEndpoints();
        app.MapCategoryEndpoints();
        app.MapBudgetEndpoints();
        
        return app;
    }
}
```

### Connection String Format (appsettings.json)

```json
{
  "ConnectionStrings": {
    "FinanceDb": "Host=ep-xxx.us-east-2.aws.neon.tech;Database=financetracker;Username=finance;Password=xxx;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

---

## 4. Domain Entities

### Transaction Entity

```csharp
// Domain/Entities/Transaction.cs

public class Transaction : IAuditableEntity
{
    public Guid Id { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public DateTime Date { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation properties
    public Category? Category { get; private set; }

    // Private constructor for EF Core
    private Transaction() { }

    // Factory method
    public static Transaction Create(
        string description,
        decimal amount,
        TransactionType type,
        DateTime date,
        Guid userId,
        Guid? categoryId = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        return new Transaction
        {
            Id = Guid.NewGuid(),
            Description = description,
            Amount = amount,
            Type = type,
            Date = date,
            UserId = userId,
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string description, decimal amount, DateTime date, Guid? categoryId)
    {
        Description = description;
        Amount = amount;
        Date = date;
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### Category Entity

```csharp
// Domain/Entities/Category.cs

public class Category : IAuditableEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Icon { get; private set; }
    public string? Color { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation properties
    public ICollection<Transaction> Transactions { get; private set; } = new List<Transaction>();
    public ICollection<Budget> Budgets { get; private set; } = new List<Budget>();

    private Category() { }

    public static Category Create(
        string name,
        Guid userId,
        string? icon = null,
        string? color = null,
        bool isDefault = false)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            UserId = userId,
            Icon = icon,
            Color = color,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

### Budget Entity

```csharp
// Domain/Entities/Budget.cs

public class Budget : IAuditableEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public BudgetPeriod Period { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation properties
    public Category Category { get; private set; } = null!;

    private Budget() { }

    public static Budget Create(
        string name,
        decimal amount,
        BudgetPeriod period,
        DateTime startDate,
        Guid userId,
        Guid categoryId,
        DateTime? endDate = null)
    {
        return new Budget
        {
            Id = Guid.NewGuid(),
            Name = name,
            Amount = amount,
            Period = period,
            StartDate = startDate,
            EndDate = endDate,
            UserId = userId,
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public decimal CalculateSpent(IEnumerable<Transaction> transactions)
    {
        return transactions
            .Where(t => t.CategoryId == CategoryId 
                     && t.Type == TransactionType.Expense
                     && t.Date >= StartDate
                     && (EndDate == null || t.Date <= EndDate))
            .Sum(t => t.Amount);
    }

    public decimal GetRemainingBudget(decimal spent) => Amount - spent;
    
    public decimal GetPercentageUsed(decimal spent) => 
        Amount > 0 ? (spent / Amount) * 100 : 0;
}
```

### Enums

```csharp
// Domain/Enums/TransactionType.cs
public enum TransactionType
{
    Income,
    Expense
}

// Domain/Enums/BudgetPeriod.cs
public enum BudgetPeriod
{
    Daily,
    Weekly,
    Monthly,
    Yearly
}
```

---

## 5. FluentValidation

### Validator Implementation

```csharp
// Application/Validators/CreateTransactionValidator.cs

public class CreateTransactionValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(1_000_000_000).WithMessage("Amount exceeds maximum allowed value");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid transaction type");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
                .WithMessage("Date cannot be in the future");

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .When(x => x.CategoryId.HasValue)
            .WithMessage("Invalid category ID");
    }
}
```

### Budget Validator with Async Rules

```csharp
// Application/Validators/CreateBudgetValidator.cs

public class CreateBudgetValidator : AbstractValidator<CreateBudgetRequest>
{
    private readonly ICategoryRepository _categoryRepository;

    public CreateBudgetValidator(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Budget name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Budget amount must be greater than zero");

        RuleFor(x => x.Period)
            .IsInEnum().WithMessage("Invalid budget period");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be after start date");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required")
            .MustAsync(CategoryExists).WithMessage("Category does not exist");
    }

    private async Task<bool> CategoryExists(Guid categoryId, CancellationToken ct)
    {
        return await _categoryRepository.ExistsAsync(categoryId, ct);
    }
}
```

### Validation Filter for Minimal APIs

```csharp
// Shared/Validation/ValidationFilter.cs

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices
            .GetService<IValidator<T>>();

        if (validator is null)
            return await next(context);

        var argument = context.Arguments
            .OfType<T>()
            .FirstOrDefault();

        if (argument is null)
            return await next(context);

        var validationResult = await validator.ValidateAsync(argument);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            return Results.ValidationProblem(errors);
        }

        return await next(context);
    }
}
```

---

## 6. Services Layer

### Transaction Service

```csharp
// Application/Services/TransactionService.cs

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repository;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        ITransactionRepository repository,
        ILogger<TransactionService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PagedResult<TransactionResponse>> GetAllAsync(
        Guid userId,
        TransactionQueryParams query,
        CancellationToken ct = default)
    {
        var spec = new TransactionFilterSpecification(userId, query);
        var (transactions, totalCount) = await _repository.GetPagedAsync(spec, ct);

        var items = transactions.Select(t => new TransactionResponse
        {
            Id = t.Id,
            Description = t.Description,
            Amount = t.Amount,
            Type = t.Type,
            Date = t.Date,
            CategoryId = t.CategoryId,
            CategoryName = t.Category?.Name,
            CreatedAt = t.CreatedAt
        }).ToList();

        return new PagedResult<TransactionResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<TransactionResponse?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken ct = default)
    {
        var transaction = await _repository.GetByIdAsync(id, userId, ct);
        
        if (transaction is null)
            return null;

        return new TransactionResponse
        {
            Id = transaction.Id,
            Description = transaction.Description,
            Amount = transaction.Amount,
            Type = transaction.Type,
            Date = transaction.Date,
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category?.Name,
            CreatedAt = transaction.CreatedAt
        };
    }

    public async Task<TransactionResponse> CreateAsync(
        CreateTransactionRequest request,
        Guid userId,
        CancellationToken ct = default)
    {
        var transaction = Transaction.Create(
            request.Description,
            request.Amount,
            request.Type,
            request.Date,
            userId,
            request.CategoryId);

        await _repository.AddAsync(transaction, ct);
        await _repository.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created transaction {TransactionId} for user {UserId}",
            transaction.Id,
            userId);

        return new TransactionResponse
        {
            Id = transaction.Id,
            Description = transaction.Description,
            Amount = transaction.Amount,
            Type = transaction.Type,
            Date = transaction.Date,
            CategoryId = transaction.CategoryId,
            CreatedAt = transaction.CreatedAt
        };
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        UpdateTransactionRequest request,
        Guid userId,
        CancellationToken ct = default)
    {
        var transaction = await _repository.GetByIdAsync(id, userId, ct);
        
        if (transaction is null)
            return false;

        transaction.Update(
            request.Description,
            request.Amount,
            request.Date,
            request.CategoryId);

        await _repository.SaveChangesAsync(ct);
        
        return true;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken ct = default)
    {
        var transaction = await _repository.GetByIdAsync(id, userId, ct);
        
        if (transaction is null)
            return false;

        _repository.Remove(transaction);
        await _repository.SaveChangesAsync(ct);
        
        return true;
    }
}
```

---

## 7. Repository Pattern

### Repository Interface

```csharp
// Domain/Interfaces/ITransactionRepository.cs

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedAsync(
        ISpecification<Transaction> spec,
        CancellationToken ct = default);
    Task<decimal> GetTotalByTypeAsync(
        Guid userId,
        TransactionType type,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default);
    Task AddAsync(Transaction transaction, CancellationToken ct = default);
    void Remove(Transaction transaction);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

### Repository Implementation

```csharp
// Infrastructure/Repositories/TransactionRepository.cs

public class TransactionRepository : ITransactionRepository
{
    private readonly FinanceDbContext _context;

    public TransactionRepository(FinanceDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken ct = default)
    {
        return await _context.Transactions
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
    }

    public async Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedAsync(
        ISpecification<Transaction> spec,
        CancellationToken ct = default)
    {
        var query = _context.Transactions
            .Include(t => t.Category)
            .AsQueryable();

        // Apply specification criteria
        query = spec.Apply(query);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.Date)
            .Skip((spec.Page - 1) * spec.PageSize)
            .Take(spec.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<decimal> GetTotalByTypeAsync(
        Guid userId,
        TransactionType type,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId
                     && t.Type == type
                     && t.Date >= startDate
                     && t.Date <= endDate)
            .SumAsync(t => t.Amount, ct);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken ct = default)
    {
        await _context.Transactions.AddAsync(transaction, ct);
    }

    public void Remove(Transaction transaction)
    {
        _context.Transactions.Remove(transaction);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
```

---

## 8. Authentication & Authorization

### JWT Configuration

```csharp
// backend/src/PersonalFinanceTracker.Api/Configuration/AuthenticationConfiguration.cs

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()!;

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();

        return services;
    }
}
```

### User Claims Extension

```csharp
// Shared/Extensions/ClaimsPrincipalExtensions.cs

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim not found");

        return Guid.Parse(userIdClaim.Value);
    }

    public static string GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value
            ?? throw new UnauthorizedAccessException("Email claim not found");
    }
}
```

---

## 9. OpenTelemetry Observability

### Configuration

```csharp
// Program.cs - OpenTelemetry setup

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("PersonalFinanceTracker"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
            options.SetDbStatementForStoredProcedure = true;
        })
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["Otlp:Endpoint"]!);
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

// Add custom metrics
builder.Services.AddSingleton<FinanceMetrics>();
```

### Custom Metrics

```csharp
// Shared/Observability/FinanceMetrics.cs

public class FinanceMetrics
{
    private readonly Counter<long> _transactionsCreated;
    private readonly Histogram<double> _transactionAmount;

    public FinanceMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("PersonalFinanceTracker");

        _transactionsCreated = meter.CreateCounter<long>(
            "finance.transactions.created",
            description: "Number of transactions created");

        _transactionAmount = meter.CreateHistogram<double>(
            "finance.transaction.amount",
            unit: "USD",
            description: "Transaction amounts");
    }

    public void RecordTransactionCreated(TransactionType type)
    {
        _transactionsCreated.Add(1, new KeyValuePair<string, object?>("type", type.ToString()));
    }

    public void RecordTransactionAmount(decimal amount, TransactionType type)
    {
        _transactionAmount.Record(
            (double)amount,
            new KeyValuePair<string, object?>("type", type.ToString()));
    }
}
```

---

## 10. TickerQ Background Jobs

### Scheduled Job for Monthly Reports

```csharp
// Modules/Reporting/Jobs/MonthlyReportJob.cs

public class MonthlyReportJob : ITickerJob
{
    private readonly IReportingService _reportingService;
    private readonly ILogger<MonthlyReportJob> _logger;

    public MonthlyReportJob(
        IReportingService reportingService,
        ILogger<MonthlyReportJob> logger)
    {
        _reportingService = reportingService;
        _logger = logger;
    }

    [TickerFunction("GenerateMonthlyReports", "0 0 1 * *")] // First day of month at midnight
    public async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting monthly report generation");

        var previousMonth = DateTime.UtcNow.AddMonths(-1);
        
        await _reportingService.GenerateMonthlyReportsAsync(
            previousMonth.Year,
            previousMonth.Month,
            ct);

        _logger.LogInformation("Completed monthly report generation");
    }
}
```

### Budget Alert Job

```csharp
// Modules/Finance/Jobs/BudgetAlertJob.cs

public class BudgetAlertJob : ITickerJob
{
    private readonly IBudgetService _budgetService;
    private readonly INotificationService _notificationService;

    public BudgetAlertJob(
        IBudgetService budgetService,
        INotificationService notificationService)
    {
        _budgetService = budgetService;
        _notificationService = notificationService;
    }

    [TickerFunction("CheckBudgetAlerts", "0 */6 * * *")] // Every 6 hours
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var budgetsNearLimit = await _budgetService
            .GetBudgetsNearLimitAsync(threshold: 80, ct);

        foreach (var budget in budgetsNearLimit)
        {
            await _notificationService.SendBudgetAlertAsync(
                budget.UserId,
                budget.Name,
                budget.PercentageUsed,
                ct);
        }
    }
}
```

### TickerQ Registration

```csharp
// Program.cs

builder.Services.AddTickerQ(options =>
{
    options.UsePostgreSql(builder.Configuration.GetConnectionString("FinanceDb")!);
})
.AddJob<MonthlyReportJob>()
.AddJob<BudgetAlertJob>();
```

---

## 11. Error Handling

### Global Exception Handler

```csharp
// Shared/Middleware/ExceptionHandlingMiddleware.cs

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            ValidationException e => (
                StatusCodes.Status400BadRequest,
                "Validation Error",
                e.Message),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication required"),
            NotFoundException e => (
                StatusCodes.Status404NotFound,
                "Not Found",
                e.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Server Error",
                "An unexpected error occurred")
        };

        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
```

---

## 12. Database Migrations

### Creating Migrations

```bash
# Navigate to the module directory
cd backend/src/Modules/Finance

# Create a migration
dotnet ef migrations add InitialCreate \
  --context FinanceDbContext \
  --output-dir Infrastructure/Migrations \
  --startup-project ../../PersonalFinanceTracker.Api

# Apply migrations
dotnet ef database update \
  --context FinanceDbContext \
  --startup-project ../../PersonalFinanceTracker.Api
```

### Migration in Code (Program.cs)

```csharp
// Apply migrations on startup (for development)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    
    var financeContext = scope.ServiceProvider
        .GetRequiredService<FinanceDbContext>();
    await financeContext.Database.MigrateAsync();
    
    var usersContext = scope.ServiceProvider
        .GetRequiredService<UsersDbContext>();
    await usersContext.Database.MigrateAsync();
}
```

---

## 13. References

- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)
- [FluentValidation](https://docs.fluentvalidation.net/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [TickerQ GitHub](https://github.com/AhmedAliRezk5050/TickerQ)
- [Neon PostgreSQL](https://neon.tech/docs)

---

*Previous: [01-Project-Structure.md](./01-Project-Structure.md)*  
*Next: [03-Frontend-Documentation.md](./03-Frontend-Documentation.md)*
