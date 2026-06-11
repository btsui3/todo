using TodoApi.Features.Account;

namespace TodoApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /api/auth/register — create an account, then auto-login by returning a JWT.
        app.MapPost("/api/auth/register", async (RegisterRequest req, AccountService accounts) =>
            ToResult(await accounts.RegisterAsync(req.Email, req.Password)));

        // POST /api/auth/login — verify credentials, return a JWT.
        app.MapPost("/api/auth/login", async (LoginRequest req, AccountService accounts) =>
            ToResult(await accounts.LoginAsync(req.Email, req.Password)));
    }

    // Translates the HTTP-agnostic outcome into a response. Failures use RFC-9110
    // ProblemDetails so the API's error contract is uniform.
    private static IResult ToResult(AuthOutcome outcome)
    {
        if (outcome.IsUnauthorized)
            return Results.Problem(detail: "Invalid email or password.", statusCode: 401);
        if (outcome.Errors is not null)
            return Results.ValidationProblem(outcome.Errors);
        return Results.Ok(new { token = outcome.Token });
    }
}

// Request bodies for the auth endpoints.
public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
