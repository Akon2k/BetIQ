using Microsoft.EntityFrameworkCore;
using BetIQ.API.Data;
using BetIQ.API.Services;
using System.Text.Json.Serialization;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.

// 1. Add DbContext (SQL Server)
builder.Services.AddDbContext<BetIQContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BetIQConnection")));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
builder.Services.AddScoped<IEloService, EloService>(); // Registers our custom service
builder.Services.AddMemoryCache(); // Adds in-memory caching service
builder.Services.AddCors(); // Adds CORS services

// Learn more about configuring OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure CORS
app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// Configure the HTTP request pipeline.
app.UseMiddleware<BetIQ.API.Middleware.ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

// This is often added, but not strictly necessary for this API
// app.UseAuthorization(); 

app.MapControllers(); // Maps the controller endpoints

try
{
    Log.Information("Iniciando BetIQ API...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación falló al iniciar.");
}
finally
{
    Log.CloseAndFlush();
}

