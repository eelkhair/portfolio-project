using System.Security.Claims;


namespace Elkhair.Dev.Common.Application;

public static class Extensions
{
    public static string GetUserId(this ClaimsPrincipal user)
    {
        return user.Claims.Where(c => string.Equals(c.Type, "sub", StringComparison.Ordinal)).Select(x => x.Value).FirstOrDefault() ?? "N/A";
    }


}
