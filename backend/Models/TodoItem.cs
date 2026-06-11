namespace TodoApi.Models;

/// <summary>
/// A single to-do item (the domain calls this a "Task"; the C# type is named
/// TodoItem to avoid clashing with System.Threading.Tasks.Task).
/// Owned by exactly one user — though ownership/auth is added in a later slice.
/// </summary>
public class TodoItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public DateOnly? DueDate { get; set; }

    /// <summary>UTC creation time. Stored as DateTime (not DateTimeOffset)
    /// because SQLite cannot ORDER BY a DateTimeOffset.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Id of the owning user (Identity user id). Every task query is
    /// filtered by this, so a user only ever sees their own tasks.</summary>
    public string UserId { get; set; } = string.Empty;
}
