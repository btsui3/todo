using System.ComponentModel.DataAnnotations;

namespace TodoApi.Auth;

/// <summary>
/// JWT signing settings, bound from the "Jwt" configuration section. Validated at
/// startup (see <c>AddTodoAuth</c>) so a missing key fails fast on boot rather than
/// at the first request.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Key { get; set; } = string.Empty;
    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;
}
