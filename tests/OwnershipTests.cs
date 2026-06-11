using System.Net;
using System.Net.Http.Json;
using Xunit;
using static TodoApi.Tests.ApiHelpers;

namespace TodoApi.Tests;

public class OwnershipTests(TestAppFactory factory) : IClassFixture<TestAppFactory>
{
    [Fact]
    public async Task A_user_does_not_see_another_users_tasks()
    {
        var alice = await factory.NewUserClientAsync();
        var bob = await factory.NewUserClientAsync();
        await alice.CreateTaskAsync("Alice's secret task");

        var bobsTasks = await bob.GetFromJsonAsync<List<TaskDto>>("/api/tasks");

        Assert.DoesNotContain(bobsTasks!, t => t.Title == "Alice's secret task");
    }

    [Fact]
    public async Task A_user_cannot_delete_another_users_task()
    {
        var alice = await factory.NewUserClientAsync();
        var bob = await factory.NewUserClientAsync();
        var aliceTaskId = await alice.CreateTaskAsync("Alice's task");

        // 404 (not 403) on purpose: don't reveal that another user's task exists.
        var res = await bob.DeleteAsync($"/api/tasks/{aliceTaskId}");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

        // And Alice's task is untouched.
        var aliceTasks = await alice.GetFromJsonAsync<List<TaskDto>>("/api/tasks");
        Assert.Contains(aliceTasks!, t => t.Id == aliceTaskId);
    }

    [Fact]
    public async Task A_user_cannot_update_another_users_task()
    {
        var alice = await factory.NewUserClientAsync();
        var bob = await factory.NewUserClientAsync();
        var aliceTaskId = await alice.CreateTaskAsync("Alice's task");

        var res = await bob.PutAsJsonAsync($"/api/tasks/{aliceTaskId}",
            new { title = "Hijacked", isComplete = true, dueDate = (string?)null });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
