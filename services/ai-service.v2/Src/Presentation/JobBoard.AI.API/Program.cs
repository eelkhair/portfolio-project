using JobBoard.AI.Application;
using JobBoard.AI.Infrastructure.Configuration;
using JobBoard.AI.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add application layers
builder.Services.AddApplicationServices(typeof(Program).Assembly);
builder.Services.AddConfigurationServices();
builder.Services.AddPersistenceServices();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "JobBoard AI API v1");
    });
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
