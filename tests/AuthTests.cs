using System.Net;
using System.Net.Http.Json;
using TodoApi.Data;
using Xunit;
using static TodoApi.Tests.ApiHelpers;

namespace TodoApi.Tests;

public class AuthTests(TestAppFactory factory) : IClassFixture<TestAppFactory>
{
    [Fact]
    public async Task Login_with_seeded_demo_user_returns_a_token()
    {
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/auth/login",
            new { email = SeedData.DemoEmail, password = SeedData.DemoPassword });

        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
    }

    [Fact]
    public async Task Login_with_wrong_password_is_rejected()
    {
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/auth/login",
            new { email = SeedData.DemoEmail, password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Getting_tasks_without_a_token_is_unauthorized()
    {
        var client = factory.CreateClient(); // no Authorization header

        var res = await client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
