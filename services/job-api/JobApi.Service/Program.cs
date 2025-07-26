using System.Security.Claims;
using System.Text.Encodings.Web;
using Elkhair.Dev.Common.Application.Abstractions.Dispatcher;
using FastEndpoints;
using FastEndpoints.Swagger;
using FluentValidation;
using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Jobs.Requests;
using JobApi.Data;
using JobApi.Presentation.Endpoints.Jobs.Create;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Register FastEndpoints + Swagger
builder.Services.AddFastEndpoints();
builder.Services.AddCors();
builder.Services.AddScoped<IValidator<CreateJobRequest>, CreateJobValidator>();
builder.Services.SwaggerDocument(options =>
{
    options.DocumentSettings = s =>
    {
        s.Title = "Job Service API";
        s.Version = "v1.0.0";
        s.Description = "API for managing job-related operations";
    };
});
builder.Services.AddDbContext<JobDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("JobDbContext"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "Jobs");
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 10,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
});
builder.Services.AddScoped<IJobDbContext, JobDbContext>();
// Add Authorization support (even if not using yet)

builder.Services.AddAuthorization();
builder.Services.AddAuthentication("Fake") 
    .AddScheme<AuthenticationSchemeOptions, DummyAuthHandler>("Fake", null);


builder.Services.AddApplicationDispatcher(typeof(Program).Assembly);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User ?? new ClaimsPrincipal());

var app = builder.Build();
app.UseCors(policy => policy.AllowAnyHeader()
    .AllowAnyMethod()
    .WithOrigins("http://localhost:4200")
    .WithOrigins("https://job-admin.eelkhair.net")
);

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