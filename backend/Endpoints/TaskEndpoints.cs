using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TodoApi.Auth;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        // All /api/tasks routes require a valid JWT and act only on the caller's tasks.
        var tasks = app.MapGroup("/api/tasks").RequireAuthorization();

        tasks.MapGet("", GetTasks);
        tasks.MapPost("", CreateTask);
        tasks.MapPut("/{id:guid}", UpdateTask);
        tasks.MapDelete("/{id:guid}", DeleteTask);
    }

    // GET /api/tasks — the caller's own tasks, newest first.
    private static async Task<IResult> GetTasks(TodoDbContext db, ClaimsPrincipal principal) =>
        Results.Ok(await db.Tasks
            .Where(t => t.UserId == principal.GetUserId())
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync());

    // POST /api/tasks — create a task owned by the caller.
    private static async Task<IResult> CreateTask(CreateTaskRequest req, TodoDbContext db, ClaimsPrincipal principal)
    {
        if (!TryValidate(req.Title, req.DueDate, out var dueDate, out var problem))
            return problem;

        var task = new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = req.Title.Trim(),
            IsComplete = false,
            DueDate = dueDate,
            CreatedAt = DateTime.UtcNow,
            UserId = principal.GetUserId(),
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return Results.Created($"/api/tasks/{task.Id}", task);
    }

    // PUT /api/tasks/{id} — edit title / due date / toggle complete. 404 if not the caller's.
    private static async Task<IResult> UpdateTask(Guid id, UpdateTaskRequest req, TodoDbContext db, ClaimsPrincipal principal)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == principal.GetUserId());
        if (task is null)
            return Results.NotFound();
        if (!TryValidate(req.Title, req.DueDate, out var dueDate, out var problem))
            return problem;

        task.Title = req.Title.Trim();
        task.IsComplete = req.IsComplete;
        task.DueDate = dueDate;
        await db.SaveChangesAsync();
        return Results.Ok(task);
    }

    // DELETE /api/tasks/{id} — delete. 404 if not the caller's.
    private static async Task<IResult> DeleteTask(Guid id, TodoDbContext db, ClaimsPrincipal principal)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == principal.GetUserId());
        if (task is null)
            return Results.NotFound();

        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    // Validates task input. On failure, `problem` is a user-facing 400
    // (ValidationProblemDetails, keyed by field) that the React form shows inline.
    // Due date is optional: missing is fine; a provided value must be YYYY-MM-DD.
    private static bool TryValidate(string title, string? dueDateRaw, out DateOnly? dueDate, out IResult problem)
    {
        var errors = new Dictionary<string, string[]>();

        var trimmed = title?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            errors["title"] = ["Title is required."];
        else if (trimmed.Length > 200)
            errors["title"] = ["Title must be 200 characters or fewer."];

        dueDate = null;
        if (!string.IsNullOrWhiteSpace(dueDateRaw))
        {
            if (DateOnly.TryParseExact(dueDateRaw, "yyyy-MM-dd", out var parsed))
                dueDate = parsed;
            else
                errors["dueDate"] = ["Please choose a valid due date."];
        }

        problem = errors.Count > 0 ? Results.ValidationProblem(errors) : Results.Empty;
        return errors.Count == 0;
    }
}

// Request bodies. DueDate is a string so we can return a friendly message on a
// bad format instead of a framework JSON-binding error.
public record CreateTaskRequest(string Title, string? DueDate);
public record UpdateTaskRequest(string Title, bool IsComplete, string? DueDate);
