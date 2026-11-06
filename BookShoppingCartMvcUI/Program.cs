using BookShoppingCart.Business.Facades;
using BookShoppingCart.Business.Services;
using BookShoppingCart.Business.Strategies;
using BookShoppingCart.Data.Data;
using BookShoppingCart.Data.Repositories;
using BookShoppingCartMvcUI.Middleware;
using BookShoppingCartMvcUI.Services;
using BookStoreCore.Shared;
using BookStoreCore.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args.Where(arg => arg != "--provision-admin" && arg != "--initialize-database").ToArray());
var connection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection.");
if (!builder.Environment.IsDevelopment())
{
    var sql = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection);
    if (sql.TrustServerCertificate || sql.Encrypt == Microsoft.Data.SqlClient.SqlConnectionEncryptOption.Optional)
        throw new InvalidOperationException("Production SQL requires encryption and certificate validation.");
    if (string.IsNullOrWhiteSpace(builder.Configuration["Email:SmtpHost"]) ||
        string.IsNullOrWhiteSpace(builder.Configuration["Email:From"]))
        throw new InvalidOperationException("Configure production email delivery.");
}
builder.Services.AddHttpClient("Catalog", client =>
{
    var url = builder.Configuration["ApiSettings:BaseUrl"]
        ?? throw new InvalidOperationException("Configure ApiSettings:BaseUrl.");
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
        (!builder.Environment.IsDevelopment() && uri.Scheme != "https"))
        throw new InvalidOperationException("Configure an absolute HTTPS API URL.");
    client.BaseAddress = new Uri(uri.AbsoluteUri.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(15);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
    .AddHttpMessageHandler<ApiBearerHandler>().RedactLoggedHeaders(["Authorization"]);
builder.Services.AddHttpClient("Authentication", client =>
{
    var url = builder.Configuration["ApiSettings:BaseUrl"];
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo))
        throw new InvalidOperationException("Authentication requires an absolute HTTPS API URL without credentials.");
    client.BaseAddress = new Uri(uri.AbsoluteUri.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(15);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
    .RedactLoggedHeaders(["Authorization"]);
builder.Services.AddScoped<AuthenticationApiClient>();
builder.Services.AddScoped<BrowserAuthenticationService>();
builder.Services.AddTransient<ApiBearerHandler>();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connection));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultUI().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = false;
    options.Events.OnValidatePrincipal = context => context.HttpContext.RequestServices
        .GetRequiredService<BrowserAuthenticationService>().ValidateCookieAsync(context);
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Headers.Accept.ToString().Contains("application/json"))
            context.Response.StatusCode = 401;
        else context.Response.Redirect(context.HttpContext.Items.ContainsKey("bookverse:session-expired")
            ? Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(context.RedirectUri, "sessionExpired", "true")
            : context.RedirectUri);
        return Task.CompletedTask;
    };
});
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHostedService<ImageCleanupService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IGenreRepository, GenreRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IUserOrderRepository, UserOrderRepository>();
builder.Services.AddScoped<BookService>();
builder.Services.AddScoped<CommerceApiClient>();
builder.Services.AddScoped<ICommerceCartGateway>(sp => sp.GetRequiredService<CommerceApiClient>());
builder.Services.AddScoped<IBookService, RemoteBooks>();
builder.Services.AddScoped<IBookFacade, BookFacade>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IGenreService, GenreService>();
builder.Services.AddScoped<ICartService, RemoteCart>();
builder.Services.AddScoped<IStockService, RemoteStock>();
builder.Services.AddScoped<IUserOrderService, RemoteOrders>();
builder.Services.AddSingleton<IDiscountService, DiscountService>();
builder.Services.AddScoped<IShippingStrategy, StandardShippingStrategy>();


builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("Login", context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddRazorPages(options => options.Conventions.AddAreaPageApplicationModelConvention(
    "Identity", "/Account/Login", model => model.EndpointMetadata.Add(
        new Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute("Login"))));
var app = builder.Build();
// Initialization is explicit, never silently applied to a production database.
if (builder.Configuration.GetValue<bool>("Database:Initialize") || args.Contains("--initialize-database", StringComparer.Ordinal))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedDatabaseAsync(scope.ServiceProvider);
}
if (args.Contains("--initialize-database", StringComparer.Ordinal)) return;
if (args.Contains("--provision-admin", StringComparer.Ordinal))
{
    using var scope = app.Services.CreateScope();
    await AdminProvisioning.Run(scope.ServiceProvider, builder.Configuration);
    return;
}
app.UseMiddleware<ExceptionMiddleware>();
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=FrontPage}/{action=Index}/{id?}");
app.MapRazorPages();
app.Run();
public partial class Program { }
