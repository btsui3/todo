using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using TodoApi.Data;

namespace TodoApi.Tests;

/// <summary>
/// Boots the real API in-process for tests, but replaces the file-based SQLite
/// with an in-memory database. The connection is held open for the factory's
/// lifetime because an in-memory SQLite db is discarded once its last connection
/// closes. A test JWT key is injected so tests don't depend on appsettings files.
/// </summary>
public class TestAppFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-at-least-32-characters-long!!",
                ["Jwt:Issuer"] = "TodoApi",
                ["Jwt:Audience"] = "TodoApi",
            }));

        builder.ConfigureServices(services =>
        {
            // Replace the app's DbContext registration with the in-memory connection.
            var descriptor = services.Single(
                d => d.ServiceType == typeof(DbContextOptions<TodoDbContext>));
            services.Remove(descriptor);
            services.AddDbContext<TodoDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
