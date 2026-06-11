using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TodoApi.Tests;

public static class ApiHelpers
{
    public record TokenResponse(string Token);
    public record TaskDto(Guid Id, string Title, bool IsComplete, string? DueDate);

    // Registers a fresh user (unique email) and returns a client authenticated as them.
    public static async Task<HttpClient> NewUserClientAsync(this TestAppFactory factory)
    {
        var client = factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var res = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Passw0rd!" });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    public static async Task<Guid> CreateTaskAsync(this HttpClient client, string title)
    {
        var res = await client.PostAsJsonAsync("/api/tasks", new { title, dueDate = (string?)null });
        res.EnsureSuccessStatusCode();
        var task = await res.Content.ReadFromJsonAsync<TaskDto>();
        return task!.Id;
    }
}
