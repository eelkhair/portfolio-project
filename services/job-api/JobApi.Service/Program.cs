using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Elkhair.Dev.Common.Application.Abstractions.Dispatcher;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Register FastEndpoints + Swagger
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(options =>
{
    options.DocumentSettings = s =>
    {
        s.Title = "Job Service API";
        s.Version = "v1.0.0";
        s.Description = "API for managing job-related operations";
    };
});

// Add Authorization support (even if not using yet)

builder.Services.AddAuthorization();
builder.Services.AddAuthentication("Fake") 
    .AddScheme<AuthenticationSchemeOptions, DummyAuthHandler>("Fake", null);


builder.Services.AddApplicationDispatcher(typeof(Program).Assembly);

var app = builder.Build();

app.UseAuthentication();    
app.UseAuthorization();     
app.UseFastEndpoints();     
app.UseSwaggerGen();        
app.MapGet("/", (HttpContext ctx) => ctx.Response.Redirect("/swagger")).ExcludeFromDescription();
app.Run();

public class DummyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DummyAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock) {}

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(); // empty identity
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Fake");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}