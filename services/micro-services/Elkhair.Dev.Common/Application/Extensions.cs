using System.Security.Claims;


namespace Elkhair.Dev.Common.Application;

public static class Extensions
{
    public static string GetUserId (this ClaimsPrincipal user) 
    {
        return user.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).Select(x=>x.Value).FirstOrDefault() ?? "N/A";
    }
    
    
}