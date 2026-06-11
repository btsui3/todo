using System.Security.Claims;

namespace TodoApi.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The authenticated user's id (from the JWT "sub" claim).</summary>
    public static string GetUserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
