using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models.DTOs;
using BookShoppingCartMvcUI.Controllers;
using BookShoppingCartMvcUI.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookShoppingCart.Tests.Tests;

public sealed class FrontendAuthHarness : IAsyncDisposable
{
    private readonly SqlConnectionStringBuilder _connection = new(Environment.GetEnvironmentVariable("BOOKSTORE_TEST_SQL")!)
    { InitialCatalog = "BookShoppingCartTests_" + Guid.NewGuid().ToString("N") };
    public WebApplicationFactory<GetBooksEndpoint> Api { get; }
    public WebApplicationFactory<BookController> Mvc { get; }
    public ConcurrentBag<(string Path, string? Scheme)> Calls { get; } = [];
    public bool Offline { get; set; }
    public const string Email = "browser@example.test";
    public const string Password = "Browser-Test-123!";
    private bool _created;
    private DbContextOptions<ApplicationDbContext> Options => new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer(_connection.ConnectionString).Options;

    public FrontendAuthHarness()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        Api = new WebApplicationFactory<GetBooksEndpoint>().WithWebHostBuilder(builder =>
        {
            Configure(builder);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string,string?>
            { ["Jwt:SigningKey"] = key, ["Jwt:Issuer"] = "https://browser.tests", ["Jwt:Audience"] = "browser-tests", ["RateLimits:AuthenticationPerMinute"] = "1000" }));
        });
        Mvc = new WebApplicationFactory<BookController>().WithWebHostBuilder(builder =>
        {
            Configure(builder);
            builder.ConfigureServices(services =>
            {
                foreach (var name in new[] { "Authentication", "Catalog" })
                    services.AddHttpClient(name).ConfigurePrimaryHttpMessageHandler(() =>
                        new RecordingHandler(this) { InnerHandler = Api.Server.CreateHandler() });
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
            });
        });
    }
    private void Configure(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string,string?>
        {
            ["ConnectionStrings:DefaultConnection"] = _connection.ConnectionString, ["Database:Initialize"] = "false",
            ["ApiSettings:BaseUrl"] = "https://api.tests/api/", ["Logging:LogLevel:Default"] = "Warning"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(_connection.ConnectionString));
            services.Configure<PasswordHasherOptions>(o => o.IterationCount = 10000);
        });
    }
    public async Task InitializeAsync(string role = "User")
    {
        await using var db = new ApplicationDbContext(Options);
        await db.Database.MigrateAsync();
        _created = true;
        using var scope = Api.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        Assert.True((await roles.CreateAsync(new IdentityRole(role))).Succeeded);
        var user = new IdentityUser { Email = Email, UserName = Email, EmailConfirmed = true };
        Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
    }
    public HttpClient Client() => Mvc.CreateClient(new WebApplicationFactoryClientOptions
    { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    public static string Antiforgery(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success);
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    public static async Task<HttpResponseMessage> Login(HttpClient client, string password = Password, string? returnUrl = null, bool remember = false)
    {
        var url = "/Identity/Account/Login" + (returnUrl is null ? "" : "?returnUrl=" + Uri.EscapeDataString(returnUrl));
        var html = await client.GetStringAsync(url);
        return await client.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string,string>
        {
            ["Input.Email"] = Email, ["Input.Password"] = password, ["Input.RememberMe"] = remember.ToString(),
            ["__RequestVerificationToken"] = Antiforgery(html)
        }));
    }
    public async Task<AuthTokens> StoredTokens(bool expiring = false, bool expiredRefresh = false)
    {
        using var scope = Mvc.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var session = await db.ApiSessions.SingleAsync();
        var protector = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("BookVerse.BrowserTokens.v1");
        var tokens = JsonSerializer.Deserialize<AuthTokens>(protector.Unprotect(session.ProtectedBrowserTokens!))!;
        if (expiring || expiredRefresh)
        {
            tokens = tokens with
            {
                AccessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(10),
                RefreshTokenExpiresAtUtc = expiredRefresh ? DateTime.UtcNow.AddMinutes(-1) : tokens.RefreshTokenExpiresAtUtc
            };
            session.ProtectedBrowserTokens = protector.Protect(JsonSerializer.Serialize(tokens));
            await db.SaveChangesAsync();
        }
        return tokens;
    }
    public async Task AssertRevoked()
    {
        await using var db = new ApplicationDbContext(Options);
        var session = await db.ApiSessions.SingleAsync();
        Assert.NotNull(session.RevokedAtUtc);
        Assert.Null(session.ProtectedBrowserTokens);
    }
    public async ValueTask DisposeAsync()
    {
        await Mvc.DisposeAsync();
        await Api.DisposeAsync();
        if (_created && _connection.InitialCatalog.StartsWith("BookShoppingCartTests_", StringComparison.Ordinal))
        { await using var db = new ApplicationDbContext(Options); await db.Database.EnsureDeletedAsync(); }
    }
    private sealed class RecordingHandler(FrontendAuthHarness owner) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            owner.Calls.Add((request.RequestUri!.AbsolutePath, request.Headers.Authorization?.Scheme));
            return owner.Offline ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)) : base.SendAsync(request, ct);
        }
    }
}

public class FrontendAuthenticationTests
{
    [SqlServerFact]
    public async Task RefreshOutage_RejectsCookieAndRevokesAmbiguousSession()
    {
        await using var app = new FrontendAuthHarness(); await app.InitializeAsync();
        using var client = app.Client(); using var login = await FrontendAuthHarness.Login(client);
        await app.StoredTokens(expiring: true);
        app.Offline = true;
        using var denied = await client.GetAsync("/Identity/Account/Manage");
        Assert.Contains("sessionExpired=true", denied.Headers.Location!.ToString());
        await app.AssertRevoked();
    }

    [SqlServerFact]
    public async Task RoleChange_RefreshesBearerAndUpdatesUiAuthorization_StampChangeRejectsCookie()
    {
        await using var app = new FrontendAuthHarness(); await app.InitializeAsync();
        using var client = app.Client(); using var login = await FrontendAuthHarness.Login(client);
        var before = await app.StoredTokens();
        using (var scope = app.Api.Services.CreateScope())
        {
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            Assert.True((await roles.CreateAsync(new IdentityRole("Admin"))).Succeeded);
            var user = (await users.FindByEmailAsync(FrontendAuthHarness.Email))!;
            Assert.True((await users.AddToRoleAsync(user, "Admin")).Succeeded);
        }
        using var admin = await client.GetAsync("/AdminOperations/Dashboard");
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
        Assert.NotEqual(before.RefreshToken, (await app.StoredTokens()).RefreshToken);
        using var catalog = await client.GetAsync("/Home/Index");
        Assert.DoesNotContain("Unable to load books", await catalog.Content.ReadAsStringAsync());
        using (var scope = app.Api.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            Assert.True((await users.UpdateSecurityStampAsync((await users.FindByEmailAsync(FrontendAuthHarness.Email))!)).Succeeded);
        }
        using var denied = await client.GetAsync("/AdminOperations/Dashboard");
        Assert.Contains("/Identity/Account/Login", denied.Headers.Location!.ToString());
    }

    [SqlServerFact]
    public async Task Login_UsesBasicApi_SetsSecureCookie_AndForwardsBearerForParallelCatalogRequests()
    {
        await using var app = new FrontendAuthHarness(); await app.InitializeAsync();
        using var client = app.Client();
        using var login = await FrontendAuthHarness.Login(client, remember: true);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/Home/Index", login.Headers.Location!.ToString());
        var cookie = login.Headers.GetValues("Set-Cookie").Single(c => c.Contains(".AspNetCore.Identity.Application="));
        Assert.Contains("secure", cookie); Assert.Contains("httponly", cookie); Assert.Contains("samesite=lax", cookie);
        Assert.Contains("expires=", cookie);
        var tokens = await app.StoredTokens();
        Assert.DoesNotContain(tokens.AccessToken, cookie); Assert.DoesNotContain(tokens.RefreshToken, cookie);
        using var page = await client.GetAsync("/Home/Index");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.True(page.Headers.CacheControl!.NoStore);
        var html = await page.Content.ReadAsStringAsync();
        Assert.Contains(FrontendAuthHarness.Email, html);
        Assert.DoesNotContain(tokens.AccessToken, html); Assert.DoesNotContain(tokens.RefreshToken, html);
        Assert.Contains(app.Calls, call => call == ("/api/auth/login", "Basic"));
        Assert.Contains(app.Calls, call => call == ("/api/Home/GetBooksPage", "Bearer"));
        Assert.Contains(app.Calls, call => call == ("/api/Home/GetGenres", "Bearer"));
        using var admin = await client.GetAsync("/AdminOperations/Dashboard");
        Assert.Contains("AccessDenied", admin.Headers.Location!.ToString());
    }

    [SqlServerFact]
    public async Task Refresh_IsSerializedAcrossConcurrentBrowserRequests_AndLogoutRevokesFamily()
    {
        await using var app = new FrontendAuthHarness(); await app.InitializeAsync();
        using var client = app.Client(); using var login = await FrontendAuthHarness.Login(client);
        var before = await app.StoredTokens(expiring: true);
        var pages = await Task.WhenAll(client.GetAsync("/Identity/Account/Manage"), client.GetAsync("/Identity/Account/Manage"));
        foreach (var page in pages) { Assert.Equal(HttpStatusCode.OK, page.StatusCode); page.Dispose(); }
        var after = await app.StoredTokens();
        Assert.NotEqual(before.RefreshToken, after.RefreshToken);
        Assert.Single(app.Calls, call => call.Path == "/api/auth/refresh");
        var html = await client.GetStringAsync("/Identity/Account/Manage");
        using var logout = await client.PostAsync("/Identity/Account/Logout", new FormUrlEncodedContent(new Dictionary<string,string>
        { ["__RequestVerificationToken"] = FrontendAuthHarness.Antiforgery(html) }));
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Contains(app.Calls, call => call == ("/api/auth/logout", "Bearer"));
        await app.AssertRevoked();
        using var denied = await client.GetAsync("/Identity/Account/Manage");
        Assert.Contains("/Identity/Account/Login", denied.Headers.Location!.ToString());
    }

    [SqlServerFact]
    public async Task LoginErrors_Validation_Csrf_AndSafeReturnUrls()
    {
        await using var app = new FrontendAuthHarness(); await app.InitializeAsync("Admin");
        using var client = app.Client();
        using var csrf = await client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(new Dictionary<string,string>
        { ["Input.Email"] = FrontendAuthHarness.Email, ["Input.Password"] = FrontendAuthHarness.Password }));
        Assert.Equal(HttpStatusCode.BadRequest, csrf.StatusCode);
        using var bad = await FrontendAuthHarness.Login(client, "wrong");
        Assert.Equal(HttpStatusCode.OK, bad.StatusCode);
        Assert.Contains("couldn&#x27;t sign you in", await bad.Content.ReadAsStringAsync());
        app.Offline = true;
        using var offline = await FrontendAuthHarness.Login(client);
        Assert.Contains("temporarily unavailable", await offline.Content.ReadAsStringAsync());
        app.Offline = false;
        using var login = await FrontendAuthHarness.Login(client, returnUrl: "https://evil.example/");
        Assert.Equal("/AdminOperations/Dashboard", login.Headers.Location!.ToString());
    }

    [SqlServerFact]
    public async Task ExpiredRefresh_RejectsProtectedPagesAndRevokesSession()
    {
        await using var app = new FrontendAuthHarness(); await app.InitializeAsync();
        using var client = app.Client(); using var login = await FrontendAuthHarness.Login(client, returnUrl: "/Identity/Account/Manage");
        Assert.Equal("/Identity/Account/Manage", login.Headers.Location!.ToString());
        await app.StoredTokens(expiredRefresh: true);
        using var denied = await client.GetAsync("/Identity/Account/Manage");
        Assert.Contains("/Identity/Account/Login", denied.Headers.Location!.ToString());
        await app.AssertRevoked();
    }

    [SqlServerFact]
    public async Task OfflineLogout_RevokesSharedSession()
    {
        await using var app = new FrontendAuthHarness(); await app.InitializeAsync();
        using var client = app.Client(); using var login = await FrontendAuthHarness.Login(client);
        var html = await client.GetStringAsync("/Identity/Account/Manage");
        app.Offline = true;
        using var logout = await client.PostAsync("/Identity/Account/Logout", new FormUrlEncodedContent(new Dictionary<string,string>
        { ["__RequestVerificationToken"] = FrontendAuthHarness.Antiforgery(html) }));
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        await app.AssertRevoked();
    }
}
