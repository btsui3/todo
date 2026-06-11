using Microsoft.AspNetCore.Identity;
using TodoApi.Auth;

namespace TodoApi.Features.Account;

/// <summary>
/// The outcome of a register/login attempt, kept HTTP-agnostic so the logic can be
/// unit-tested without spinning up the web stack. The endpoint translates it to an
/// <see cref="IResult"/>.
/// </summary>
public sealed record AuthOutcome
{
    public string? Token { get; private init; }
    public IDictionary<string, string[]>? Errors { get; private init; }
    public bool IsUnauthorized { get; private init; }

    public static AuthOutcome Success(string token) => new() { Token = token };
    public static AuthOutcome Invalid(IDictionary<string, string[]> errors) => new() { Errors = errors };
    public static AuthOutcome Unauthorized() => new() { IsUnauthorized = true };
}

/// <summary>
/// Owns account workflows: creating users, verifying credentials, and turning
/// ASP.NET Identity's results into clean, field-keyed messages. Keeping this out of
/// the endpoint makes the only non-trivial auth logic (the error mapping) testable.
/// </summary>
public sealed class AccountService(UserManager<IdentityUser> users, TokenService tokens)
{
    public async Task<AuthOutcome> RegisterAsync(string email, string password)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(email))
            errors["email"] = ["Email is required."];
        if (string.IsNullOrWhiteSpace(password))
            errors["password"] = ["Password is required."];
        if (errors.Count > 0)
            return AuthOutcome.Invalid(errors);

        var user = new IdentityUser { UserName = email, Email = email };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            return AuthOutcome.Invalid(MapIdentityErrors(result));

        return AuthOutcome.Success(tokens.CreateToken(user));
    }

    public async Task<AuthOutcome> LoginAsync(string email, string password)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null || !await users.CheckPasswordAsync(user, password))
            return AuthOutcome.Unauthorized();

        return AuthOutcome.Success(tokens.CreateToken(user));
    }

    private static IDictionary<string, string[]> MapIdentityErrors(IdentityResult result)
    {
        // A taken email surfaces as both DuplicateUserName + DuplicateEmail (we use
        // the email as the username); collapse to one clean message.
        if (result.Errors.Any(e => e.Code.StartsWith("Duplicate")))
            return new Dictionary<string, string[]>
            {
                ["email"] = ["An account with this email already exists."],
            };

        // Otherwise surface Identity's messages (e.g. password policy) per field.
        return result.Errors
            .GroupBy(e => e.Code.Contains("Password") ? "password" : "email")
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
    }
}
