using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;

/// <summary>
/// Applies migrations and seeds a known demo account (demo@todo.app / Todo-Demo-Acct-2026!)
/// with one task, so a reviewer can log in and see data immediately.
/// </summary>
public static class SeedData
{
    public const string DemoEmail = "demo@todo.app";

    // Unique, non-breached value: browsers flag common passwords (e.g. "Demo123!")
    // as compromised. Still satisfies Identity's default policy (upper/lower/digit/symbol).
    public const string DemoPassword = "Todo-Demo-Acct-2026!";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<TodoDbContext>();
        await db.Database.MigrateAsync();

        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        var demo = await users.FindByEmailAsync(DemoEmail);
        if (demo is null)
        {
            demo = new IdentityUser { UserName = DemoEmail, Email = DemoEmail };
            await users.CreateAsync(demo, DemoPassword);
        }
        else if (!await users.CheckPasswordAsync(demo, DemoPassword))
        {
            // Keep the demo login working even if the seed password changed since the
            // db was first created (dev convenience; the demo account is not real data).
            // Remove + add avoids needing a reset-token provider in this minimal setup.
            await users.RemovePasswordAsync(demo);
            await users.AddPasswordAsync(demo, DemoPassword);
        }

        if (!await db.Tasks.AnyAsync(t => t.UserId == demo.Id))
        {
            db.Tasks.Add(new TodoItem
            {
                Id = Guid.NewGuid(),
                Title = "Welcome — this task was seeded for the demo user",
                IsComplete = false,
                CreatedAt = DateTime.UtcNow,
                UserId = demo.Id,
            });
            await db.SaveChangesAsync();
        }
    }
}
