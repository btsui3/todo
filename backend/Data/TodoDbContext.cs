using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;

/// <summary>
/// EF Core context. Inherits IdentityDbContext so ASP.NET Identity's user tables
/// (AspNetUsers, etc.) live in the same SQLite database as our Tasks.
/// </summary>
public class TodoDbContext(DbContextOptions<TodoDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<TodoItem> Tasks => Set<TodoItem>();
}
