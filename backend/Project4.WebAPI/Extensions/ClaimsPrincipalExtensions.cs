using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Project4.WebAPI.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        var rawUserId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(rawUserId, out var userId) ? userId : null;
    }

    public static string? GetEmailAddress(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email");
    }

    public static string GetDisplayName(this ClaimsPrincipal principal, string fallback = "Google User")
    {
        return principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue("name")
            ?? fallback;
    }

    public static bool IsSellerUser(this ClaimsPrincipal principal)
    {
        return principal.IsInRole("Seller");
    }
}
