using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
namespace BookShoppingCart.Business.Services;
public class GenreService(IGenreRepository genres) : IGenreService
{
    public Task<IEnumerable<Genre>> GetGenres() => genres.GetAllAsync();
    public Task<Genre?> GetGenreById(int id)
    {
        if (id <= 0) throw new ArgumentException("Invalid genre id.");
        return genres.GetByIdAsync(id);
    }
    private async Task<string> Validate(Genre genre)
    {
        ArgumentNullException.ThrowIfNull(genre);
        var name = genre.GenreName?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 40) throw new ArgumentException("Genre name must contain 1–40 characters.");
        if (await genres.NameExists(name, genre.Id)) throw new InvalidOperationException("Genre already exists.");
        return name;
    }
    public async Task AddGenre(Genre genre)
    {
        var name = await Validate(genre);
        // Construct a scalar entity rather than persisting a request-supplied object graph.
        var created = new Genre { GenreName = name };
        await genres.AddAsync(created);
        genre.Id = created.Id;
        genre.GenreName = created.GenreName;
    }
    public async Task UpdateGenre(Genre genre)
    {
        if (genre is null) throw new ArgumentNullException(nameof(genre));
        if (genre.Id <= 0) throw new ArgumentException("Invalid genre id.");
        var name = await Validate(genre);
        var existing = await genres.GetByIdAsync(genre.Id)
            ?? throw new InvalidOperationException("Genre not found.");
        existing.GenreName = name;
        await genres.UpdateAsync(existing);
    }
    public async Task DeleteGenre(int id)
    {
        if (id <= 0) throw new ArgumentException("Invalid genre id.");
        var genre = await genres.GetByIdAsync(id) ?? throw new InvalidOperationException("Genre not found.");
        if (await genres.HasBooks(id)) throw new InvalidOperationException("This genre is linked to existing books and cannot be deleted.");
        await genres.DeleteAsync(genre);
    }
}
