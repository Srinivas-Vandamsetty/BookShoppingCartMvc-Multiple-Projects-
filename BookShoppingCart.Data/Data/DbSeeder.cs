using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models;
using BookStoreCore.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookShoppingCartMvcUI.Data;

public class DbSeeder
{
    public static async Task SeedDatabaseAsync(IServiceProvider service)
    {
        try
        {
            var context = service.GetService<ApplicationDbContext>();

            if ((await context.Database.GetPendingMigrationsAsync()).Any())
            {
                await context.Database.MigrateAsync();
            }

            var roleMgr = service.GetService<RoleManager<IdentityRole>>();
            var userMgr = service.GetService<UserManager<IdentityUser>>();

            var adminRoleExists = await roleMgr.RoleExistsAsync(Roles.Admin.ToString());
            if (!adminRoleExists)
            {
                await roleMgr.CreateAsync(new IdentityRole(Roles.Admin.ToString()));
            }

            var userRoleExists = await roleMgr.RoleExistsAsync(Roles.User.ToString());
            if (!userRoleExists)
            {
                await roleMgr.CreateAsync(new IdentityRole(Roles.User.ToString()));
            }

            var admin = new IdentityUser
            {
                UserName = "admin@gmail.com",
                Email = "admin@gmail.com",
                EmailConfirmed = true
            };

            var userInDb = await userMgr.FindByEmailAsync(admin.Email);
            if (userInDb is null)
            {
                await userMgr.CreateAsync(admin, "Admin@123");
                await userMgr.AddToRoleAsync(admin, Roles.Admin.ToString());
            }

            if (!context.Genres.Any())
            {
                await SeedGenreAsync(context);
            }

            if (!context.Books.Any())
            {
                await SeedBooksAsync(context);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static async Task SeedGenreAsync(ApplicationDbContext context)
    {
        var genres = new[]
        {
            new Genre { GenreName = "Romance" },
            new Genre { GenreName = "Action" },
            new Genre { GenreName = "Thriller" },
            new Genre { GenreName = "Crime" },
            new Genre { GenreName = "SelfHelp" },
            new Genre { GenreName = "Programming" }
        };

        await context.Genres.AddRangeAsync(genres);
        await context.SaveChangesAsync();
    }

    private static async Task SeedBooksAsync(ApplicationDbContext context)
    {
        var books = new List<Book>
        {
            new Book { BookName = "Pride and Prejudice", AuthorName = "Jane Austen", Price = 12.99, GenreId = 1 },
            new Book { BookName = "The Notebook", AuthorName = "Nicholas Sparks", Price = 11.99, GenreId = 1 },
            new Book { BookName = "Outlander", AuthorName = "Diana Gabaldon", Price = 14.99, GenreId = 1 },
            new Book { BookName = "Me Before You", AuthorName = "Jojo Moyes", Price = 10.99, GenreId = 1 },
            new Book { BookName = "The Fault in Our Stars", AuthorName = "John Green", Price = 9.99, GenreId = 1 },
            new Book { BookName = "The Bourne Identity", AuthorName = "Robert Ludlum", Price = 14.99, GenreId = 2 },
            new Book { BookName = "Die Hard", AuthorName = "Roderick Thorp", Price = 13.99, GenreId = 2 },
            new Book { BookName = "Jurassic Park", AuthorName = "Michael Crichton", Price = 15.99, GenreId = 2 },
            new Book { BookName = "Gone Girl", AuthorName = "Gillian Flynn", Price = 11.99, GenreId = 3 },
            new Book { BookName = "The Girl with the Dragon Tattoo", AuthorName = "Stieg Larsson", Price = 10.99, GenreId = 3 },
            new Book { BookName = "The Godfather", AuthorName = "Mario Puzo", Price = 13.99, GenreId = 4 },
            new Book { BookName = "The Cuckoo's Calling", AuthorName = "Robert Galbraith", Price = 14.99, GenreId = 4 },
            new Book { BookName = "The 7 Habits of Highly Effective People", AuthorName = "Stephen R. Covey", Price = 9.99, GenreId = 5 },
            new Book { BookName = "How to Win Friends and Influence People", AuthorName = "Dale Carnegie", Price = 8.99, GenreId = 5 },
            new Book { BookName = "Clean Code", AuthorName = "Robert C. Martin", Price = 19.99, GenreId = 6 },
            new Book { BookName = "Design Patterns", AuthorName = "Erich Gamma", Price = 17.99, GenreId = 6 },
            new Book { BookName = "Code Complete", AuthorName = "Steve McConnell", Price = 21.99, GenreId = 6 }
        };

        await context.Books.AddRangeAsync(books);
        await context.SaveChangesAsync();
    }
}