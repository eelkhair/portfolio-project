using Microsoft.AspNetCore.Http;

namespace Elkhair.Dev.Common.Application;

public class UserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentUser()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.Name;
    }

    public string? GetHeader(string key)
    {
        return _httpContextAccessor.HttpContext?.Request.Headers[key].FirstOrDefault();
    }
}