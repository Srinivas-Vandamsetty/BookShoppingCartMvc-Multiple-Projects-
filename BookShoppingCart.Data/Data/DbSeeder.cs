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

namespace BookShoppingCart.Data.Data;

public class DbSeeder
{

    public static async Task SeedDatabaseAsync(IServiceProvider service)
    {
        var context = service.GetRequiredService<ApplicationDbContext>();
        var roles = service.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "User" })
        {
            if (!await roles.RoleExistsAsync(role))
            {
                var result = await roles.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded) throw new InvalidOperationException("Unable to initialize roles: " +
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
        foreach (var (name, code) in new[] { ("Pending", 1), ("Shipped", 2), ("Delivered", 3), ("Cancelled", 4), ("Returned", 5), ("Refund", 6) })
            if (!await context.OrderStatuses.AnyAsync(s => s.StatusName == name))
                context.OrderStatuses.Add(new OrderStatus { StatusId = code, StatusName = name });
        await context.SaveChangesAsync();
        await SeedGenreAsync(context);
        if (!await context.Books.AnyAsync()) await SeedBooksAsync(context);
        // Administrator accounts are provisioned explicitly; never use a built-in password.
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

        foreach (var genre in genres)
            if (!await context.Genres.AnyAsync(g => g.GenreName == genre.GenreName)) context.Genres.Add(genre);
        await context.SaveChangesAsync();
    }

    private static async Task SeedBooksAsync(ApplicationDbContext context)
    {
        var genreIds = await context.Genres.ToDictionaryAsync(g => g.GenreName, g => g.Id);
        var books = new List<Book>
        {
            new Book { BookName = "Pride and Prejudice", AuthorName = "Jane Austen", Price = 12.99m, GenreId = genreIds["Romance"] },
            new Book { BookName = "The Notebook", AuthorName = "Nicholas Sparks", Price = 11.99m, GenreId = genreIds["Romance"] },
            new Book { BookName = "Outlander", AuthorName = "Diana Gabaldon", Price = 14.99m, GenreId = genreIds["Romance"] },
            new Book { BookName = "Me Before You", AuthorName = "Jojo Moyes", Price = 10.99m, GenreId = genreIds["Romance"] },
            new Book { BookName = "The Fault in Our Stars", AuthorName = "John Green", Price = 9.99m, GenreId = genreIds["Romance"] },
            new Book { BookName = "The Bourne Identity", AuthorName = "Robert Ludlum", Price = 14.99m, GenreId = genreIds["Action"] },
            new Book { BookName = "Die Hard", AuthorName = "Roderick Thorp", Price = 13.99m, GenreId = genreIds["Action"] },
            new Book { BookName = "Jurassic Park", AuthorName = "Michael Crichton", Price = 15.99m, GenreId = genreIds["Action"] },
            new Book { BookName = "Gone Girl", AuthorName = "Gillian Flynn", Price = 11.99m, GenreId = genreIds["Thriller"] },
            new Book { BookName = "The Girl with the Dragon Tattoo", AuthorName = "Stieg Larsson", Price = 10.99m, GenreId = genreIds["Thriller"] },
            new Book { BookName = "The Godfather", AuthorName = "Mario Puzo", Price = 13.99m, GenreId = genreIds["Crime"] },
            new Book { BookName = "The Cuckoo's Calling", AuthorName = "Robert Galbraith", Price = 14.99m, GenreId = genreIds["Crime"] },
            new Book { BookName = "The 7 Habits of Highly Effective People", AuthorName = "Stephen R. Covey", Price = 9.99m, GenreId = genreIds["SelfHelp"] },
            new Book { BookName = "How to Win Friends and Influence People", AuthorName = "Dale Carnegie", Price = 8.99m, GenreId = genreIds["SelfHelp"] },
            new Book { BookName = "Clean Code", AuthorName = "Robert C. Martin", Price = 19.99m, GenreId = genreIds["Programming"] },
            new Book { BookName = "Design Patterns", AuthorName = "Erich Gamma", Price = 17.99m, GenreId = genreIds["Programming"] },
            new Book { BookName = "Code Complete", AuthorName = "Steve McConnell", Price = 21.99m, GenreId = genreIds["Programming"] }
        };

        await context.Books.AddRangeAsync(books);
        await context.SaveChangesAsync();
    }
}
