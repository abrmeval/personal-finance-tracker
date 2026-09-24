using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Personal.FinanceTracker.Finance;
using Personal.FinanceTracker.Reporting;
using Personal.FinanceTracker.Reporting.Api.Endpoints;
using Personal.FinanceTracker.Reporting.Infrastructure.Data;
using Personal.FinanceTracker.Shared.Middleware;
using Personal.FinanceTracker.Users;
using Scalar.AspNetCore;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;
using TickerQ.EntityFrameworkCore.Customizer;

var builder = WebApplication.CreateBuilder(args);

// Local development overrides (gitignored) — see docs/06-Local-Development.md
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// ── Services ───────────────────────────────────────────────────    
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Serialize and bind enums as strings (e.g. "Income"/"Expense") for JSON endpoints
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
      var jwtConfig = builder.Configuration.GetSection("Jwt");
      options.TokenValidationParameters = new()
      {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = jwtConfig["Issuer"],
          ValidAudience = jwtConfig["Audience"],
          IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
              System.Text.Encoding.UTF8.GetBytes(jwtConfig["SecretKey"]!))
      };
  });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

builder.Services.AddHealthChecks();

builder.Services.AddUsersModule(builder.Configuration);
builder.Services.AddFinanceModule(builder.Configuration);
builder.Services.AddReportingModule(builder.Configuration);

builder.Services.AddTickerQ(options =>
{
    options.AddOperationalStore(ef =>
    {
        ef.UseApplicationDbContext<ReportingDbContext>(ConfigurationType.UseModelCustomizer);
        ef.SetSchema("ticker");
    });
});

// TODO Sprint 6: builder.Services.AddOpenTelemetry(...)

var app = builder.Build();

// Custom exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
{
    options.Title = "Personal Finance Tracker API Reference";
    options.Theme = ScalarTheme.BluePlanet;
    options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
});
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");



app.MapUsersEndpoints();
app.MapFinanceEndpoints();
app.MapReportingEndpoints();
app.UseTickerQ();

app.Run();
