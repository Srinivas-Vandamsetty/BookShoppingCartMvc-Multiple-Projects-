using BookShoppingCart.Business.Services;
using BookShoppingCart.Data.Data;
using BookShoppingCart.Data.Repositories;
using BookShoppingCart.WebAPI.Security;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection.");
if (!builder.Environment.IsDevelopment())
{
    var sql = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection);
    if (sql.TrustServerCertificate || sql.Encrypt == Microsoft.Data.SqlClient.SqlConnectionEncryptOption.Optional)
        throw new InvalidOperationException("Production SQL requires encryption and certificate validation.");
}
builder.Services.AddFastEndpoints();
builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connection));

builder.Services.AddOptions<JwtOptions>().BindConfiguration("Jwt").ValidateDataAnnotations()
    .Validate(o => o.HasValidKeys(), "Configure valid base64 JWT signing keys (at least 32 random bytes) and token lifetimes.")
    .ValidateOnStart();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddIdentityCore<IdentityUser>(options =>
{
    options.User.RequireUniqueEmail=true;
    options.SignIn.RequireConfirmedAccount=true;
    options.Lockout.MaxFailedAccessAttempts=5;
    options.Lockout.DefaultLockoutTimeSpan=TimeSpan.FromMinutes(15);
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddSignInManager();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions,BasicLoginHandler>(BasicLoginHandler.SchemeName,_=>{})
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims=false;
        options.IncludeErrorDetails=false;
        options.SaveToken=false;
        options.Events=new JwtBearerEvents
        {
            OnTokenValidated=async context =>
            {
                if (!await context.HttpContext.RequestServices.GetRequiredService<IApiAuthenticationService>()
                    .ValidateAccessAsync(context.Principal!,context.HttpContext.RequestAborted))
                    context.Fail("Invalid authentication session.");
            }
        };
    });
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer,jwt) => bearer.TokenValidationParameters=jwt.Value.ValidationParameters());
builder.Services.AddScoped<IAuthSessionRepository,AuthSessionRepository>();
builder.Services.AddScoped<IApiAuthenticationService,ApiAuthenticationService>();
builder.Services.AddSingleton<IAccessTokenIssuer,JwtAccessTokenIssuer>();
builder.Services.AddSingleton(provider =>
{
    var jwt=provider.GetRequiredService<IOptions<JwtOptions>>().Value;
    return new ApiTokenLifetimes
    {
        RefreshLifetime=TimeSpan.FromDays(jwt.RefreshTokenDays),
        SessionLifetime=TimeSpan.FromDays(jwt.SessionDays)
    };
});
builder.Services.AddHostedService<AuthenticationCleanupService>();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BookStoreCore.Security.ICurrentUser, ApiCurrentUser>();
builder.Services.AddScoped<CommerceOperations>();
builder.Services.AddScoped<CartRepository>();
builder.Services.AddScoped<ICartRepository>(services => services.GetRequiredService<CartRepository>());
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<BookCatalogService>();
builder.Services.AddScoped<IUserOrderRepository, UserOrderRepository>();
builder.Services.AddScoped<IUserOrderService, UserOrderService>();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<IHomeRepository, HomeRepository>();
builder.Services.AddScoped<IHomeService, HomeService>();
builder.Services.AddScoped<IGenreRepository, GenreRepository>();
builder.Services.AddScoped<IGenreService, GenreService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsync("The service is busy. Please retry shortly.", ct);
    };
    options.AddPolicy("AuthenticationPolicy", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit=builder.Configuration.GetValue("RateLimits:AuthenticationPerMinute",20), Window=TimeSpan.FromMinutes(1), QueueLimit=0 }));
    options.AddPolicy("BooksPolicy", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("RateLimits:CatalogPerMinute", 600),
            Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
});
builder.Services.SwaggerDocument(options =>
{
    options.EnableJWTBearerAuth=false;
    options.DocumentSettings = settings =>
    {
        settings.AddAuth(JwtBearerDefaults.AuthenticationScheme, new NSwag.OpenApiSecurityScheme
        {
            Type=NSwag.OpenApiSecuritySchemeType.Http, Scheme="bearer", BearerFormat="JWT",
            Description="JWT access token returned by POST /api/auth/login or /api/auth/refresh."
        });
        settings.OperationProcessors.Add(new NSwag.Generation.Processors.Security.AspNetCoreOperationSecurityScopeProcessor(JwtBearerDefaults.AuthenticationScheme));
        settings.AddSecurity(BasicLoginHandler.SchemeName, new NSwag.OpenApiSecurityScheme
        {
            Type=NSwag.OpenApiSecuritySchemeType.Http, Scheme="basic",
            Description="HTTPS Basic credentials for POST /api/auth/login only."
        });
        settings.OperationProcessors.Add(new NSwag.Generation.Processors.Security.AspNetCoreOperationSecurityScopeProcessor(BasicLoginHandler.SchemeName));
    };
});
var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var infrastructureFailure=false;
    for(var current=error; current is not null; current=current.InnerException)
        if(current is System.Data.Common.DbException && error is not DbUpdateException) infrastructureFailure=true;
    var status = infrastructureFailure ? 503 : error switch { ArgumentException or System.ComponentModel.DataAnnotations.ValidationException => 400, KeyNotFoundException => 404,
        UnauthorizedAccessException => 403, NotSupportedException => 422,
        InvalidOperationException or DbUpdateException => 409, _ => 500 };
    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Status = status, Title = status == 503 ? "The service is currently unavailable." : status == 500 ? "An unexpected error occurred." :
            error is DbUpdateException ? "The request conflicts with existing data." : error?.Message,
        Extensions = { ["traceId"] = context.TraceIdentifier }
    });
}));
app.Use(async (context,next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/auth"))
    {
        context.Response.Headers.CacheControl="no-store";
        context.Response.Headers.Pragma="no-cache";
        if (!context.Request.IsHttps)
        {
            context.Response.StatusCode=400;
            await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails { Status=400,Title="HTTPS is required." });
            return;
        }
    }
    await next();
});
app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints(options => options.Validation.EnableDataAnnotationsSupport = true);
app.MapControllers();
if (app.Environment.IsDevelopment()) app.UseSwaggerGen();
app.Run();
public partial class Program { }
