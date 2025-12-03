
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHealthChecksUI(s=> s.SetEvaluationTimeInSeconds(8))
    .AddInMemoryStorage();

builder.Logging.AddJsonConsole();

var app = builder.Build();


app.UseHealthChecksUI(config =>
{
    config.UIPath = "/health";
    config.AddCustomStylesheet("wwwroot/css/style.css");
});
app.MapGet("/", (HttpContext ctx) => ctx.Response.Redirect("/health"));

app.MapHealthChecksUI();

app.Run();