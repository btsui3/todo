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

public static class HostingExtensions
{
    public static IServiceCollection AddTodoPersistence(this IServiceCollection services, IConfiguration config)
    {
        // File-based SQLite so data survives restarts
        services.AddDbContext<TodoDbContext>(options =>
            options.UseSqlite(config.GetConnectionString("TodoDb") ?? "Data Source=todo.db"));
        return services;
    }

    public static IServiceCollection AddTodoAuth(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<JwtOptions>()
            .Bind(config.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddIdentityCore<IdentityUser>(options => options.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<TodoDbContext>();

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
