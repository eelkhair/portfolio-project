using System.Security.Claims;

namespace Elkhair.Dev.Common.Dapr;

public class EventDto<T>(string userId, string idempotencyKey, T data)
{
    public string UserId { get; set; } = userId;
    public T Data { get; set; } = data;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public string IdempotencyKey { get; set; } = idempotencyKey;
    
    private static ClaimsPrincipal CreateUser(string userId)
    {
        var claims = new List<Claim> () { 
            new Claim (ClaimTypes.NameIdentifier, userId), 
        }; 
        var identity = new ClaimsIdentity (claims, "TestAuthType"); 
        return new ClaimsPrincipal (identity);
    }
}
