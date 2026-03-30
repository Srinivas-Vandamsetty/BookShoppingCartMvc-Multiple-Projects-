using BookShoppingCart.Business.Facades;
using BookShoppingCart.Business.Proxies;
using BookShoppingCart.Business.Services;
using BookShoppingCart.Business.Strategies;
using BookShoppingCart.Data.Data;
using BookShoppingCart.Data.Repositories;
using BookShoppingCartMvcUI.Middleware;
using BookStoreCore.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();

builder.Services.AddMemoryCache();

// Repositories
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IGenreRepository, GenreRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<StockRepository>();
builder.Services.AddScoped<IUserOrderRepository, UserOrderRepository>();
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

// Services + Proxy
builder.Services.AddScoped<BookService>();
builder.Services.AddScoped<IBookService>(provider =>
{
    var realService = provider.GetRequiredService<BookService>();
    return new BookServiceProxy(realService);
});

builder.Services.AddScoped<IBookFacade, BookFacade>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IGenreService, GenreService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IUserOrderService, UserOrderService>();

builder.Services.AddSingleton<IDiscountService, DiscountService>();
builder.Services.AddScoped<IShippingStrategy, StandardShippingStrategy>();

var app = builder.Build();

// Database seeding
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    try
    {
        await DbSeeder.SeedDatabaseAsync(serviceProvider);
    }
    catch (Exception ex)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Global Exception Middleware
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=FrontPage}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();