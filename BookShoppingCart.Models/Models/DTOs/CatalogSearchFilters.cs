namespace BookShoppingCart.Models.Models.DTOs;

public sealed class CatalogSearchFilters
{
    public IReadOnlyCollection<int> GenreIds { get; init; } = [];
    public string Author { get; init; } = string.Empty;
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? InStock { get; init; }
    public string Sort { get; init; } = "id";
}
