using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TodoApi.Auth;
using TodoApi.Data;
using TodoApi.Features.Account;

namespace TodoApi;

/// <summary>
/// Service-registration and startup helpers, so <c>Program.cs</c> stays a thin
/// composition root that reads as a table of contents.
/// </summary>
public static class HostingExtensions
{
    public static IServiceCollection AddTodoPersistence(this IServiceCollection services, IConfiguration config)
    {
        // File-based SQLite so data survives restarts (see ADR-0001).
        services.AddDbContext<TodoDbContext>(options =>
            options.UseSqlite(config.GetConnectionString("TodoDb") ?? "Data Source=todo.db"));
        return services;
    }

    public static IServiceCollection AddTodoAuth(this IServiceCollection services, IConfiguration config)
    {
        // Bind + validate JWT settings at startup, so a missing/blank key fails fast
        // on boot rather than at the first request that needs to sign a token.
        services.AddOptions<JwtOptions>()
            .Bind(config.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // ASP.NET Identity: handles user storage + secure password hashing for us.
        services
            .AddIdentityCore<IdentityUser>(options => options.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<TodoDbContext>();

        // Authenticate requests by validating the signed JWT they carry. Configure
        // the validation parameters from the bound JwtOptions so signing
        // (TokenService) and validation always read the same settings from the same
        // source — avoids any config-timing skew.
        services.AddAuthentication("Bearer").AddJwtBearer("Bearer");
        services.AddOptions<JwtBearerOptions>("Bearer")
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                };
            });
        services.AddAuthorization();

        services.AddScoped<TokenService>();
        services.AddScoped<AccountService>();
        return services;
    }

    // Applies migrations, then seeds a demo user with one task (so a reviewer can
    // log in immediately), before the app starts serving requests.
    public static async Task UseDatabaseSeedingAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        await SeedData.InitializeAsync(scope.ServiceProvider);
    }
}
