using BookShoppingCart.Business.Services;
using BookShoppingCart.Business.Strategies;
using BookShoppingCart.Data.Data;
using BookShoppingCart.Data.Repositories;
using BookStoreCore.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Registers HttpClient for dependency injection to make HTTP requests
builder.Services.AddHttpClient();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();
builder.Services.AddControllersWithViews();

// Register application services
builder.Services.AddMemoryCache();

// Register repositories
//builder.Services.AddScoped<IHomeRepository, HomeRepository>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IGenreRepository, GenreRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<StockRepository>();
builder.Services.AddScoped<IUserOrderRepository, UserOrderRepository>();
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

// Register services
builder.Services.AddScoped<IBookService, BookService>();
//builder.Services.AddScoped<IHomeService, HomeService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IGenreService, GenreService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IUserOrderService, UserOrderService>();

builder.Services.AddSingleton<IDiscountService, DiscountService>();
builder.Services.AddScoped<IShippingStrategy, ShippingStrategy>();

var app = builder.Build();


// Database Seeding Logic
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
