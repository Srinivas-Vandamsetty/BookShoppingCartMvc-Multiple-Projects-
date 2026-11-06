namespace BookShoppingCart.Models.Models.DTOs;
public sealed class PagedBooks
{
    public IReadOnlyList<BookListDTO> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}
