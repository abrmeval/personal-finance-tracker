using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Personal.FinanceTracker.Users.Api.Endpoints;
using Personal.FinanceTracker.Users.Application.Interfaces;
using Personal.FinanceTracker.Users.Application.Validators;
using Personal.FinanceTracker.Users.Domain.Interfaces;
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