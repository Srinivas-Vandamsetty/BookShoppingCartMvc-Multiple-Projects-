using System.Collections.Generic;

namespace BookShoppingCart.Models.Models.DTOs
{
    public class BookDisplayModel
    {
        public IEnumerable<Book> Books { get; set; } = [];
        public IEnumerable<Genre> Genres { get; set; } = [];
        public string STerm { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int TotalCount { get; set; }
        public int GenreId { get; set; } = 0;
        public IReadOnlyCollection<int> GenreIds { get; set; } = [];
        public string Author { get; set; } = string.Empty;
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool InStockOnly { get; set; }
        public string Sort { get; set; } = "title";
    }
}
