using BookShoppingCart.Business.Services;
using BookShoppingCart.Data.Data;
using BookShoppingCart.Data.Repositories;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add FastEndpoints (should come **before** AddControllers)
builder.Services.AddFastEndpoints();

// Add EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add Controllers (optional if using FastEndpoints only)
builder.Services.AddControllers();

// Register application services
builder.Services.AddMemoryCache();

// Register Repositories & Services
builder.Services.AddScoped<IHomeRepository, HomeRepository>();
builder.Services.AddScoped<IHomeService, HomeService>();
builder.Services.AddScoped<IGenreRepository, GenreRepository>();
builder.Services.AddScoped<IGenreService, GenreService>();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("BooksPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,                  // 10 requests
                Window = TimeSpan.FromMinutes(1),  // per 1 minute
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }));
});

// Output Caching
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("GenreCache", policy =>
    {
        policy.Expire(TimeSpan.FromSeconds(60));
    });
});

// CORS
var corsPolicy = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy,
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Swagger (with FastEndpoints support)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Book Shopping Cart API", Version = "v1" });
});
builder.Services.SwaggerDocument();

var app = builder.Build();

// Middlewares order matters
app.UseHttpsRedirection();
app.UseCors(corsPolicy);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BookShoppingCart API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthorization();

// Register FastEndpoints first
app.UseFastEndpoints();

// Map controllers if you use any MVC/WebAPI controllers
app.MapControllers();

app.UseRouting();

app.UseOutputCache();

app.UseRateLimiter();

app.Run();
