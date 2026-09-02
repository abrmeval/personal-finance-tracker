using Microsoft.AspNetCore.Authentication.JwtBearer;
using Personal.FinanceTracker.Finance;
using Personal.FinanceTracker.Shared.Middleware;
using Personal.FinanceTracker.Users;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Services ───────────────────────────────────────────────────    
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// builder.Services.AddSwaggerGen(c =>
// {
//     c.SwaggerDoc("v1", new OpenApiInfo { Title = "Personal Finance Tracker API", Version = "v1" });

//     c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//     {
//         Name = "Authorization",
//         Type = SecuritySchemeType.Http,
//         Scheme = "bearer",
//         BearerFormat = "JWT",
//         In = ParameterLocation.Header
//     });
//     c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
//     {
//         [new OpenApiSecuritySchemeReference("Bearer")] = []
//     });
// });

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
// TODO Sprint 4: builder.Services.AddReportingModule(builder.Configuration);

// TODO Sprint 6: builder.Services.AddOpenTelemetry(...)

var app = builder.Build();

// Custom exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    // app.UseSwagger();
    // app.UseSwaggerUI();

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

// TODO Sprint 4: app.MapReportingEndpoints();

app.Run();
