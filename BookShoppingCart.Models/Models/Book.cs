using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookShoppingCart.Models.Models
{
    [Table("Book")]
    public class Book : IEntity
    {
        public int Id { get; set; }
        public bool IsArchived { get; set; }

        [Required]
        [MaxLength(40)]
        public string BookName { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string AuthorName { get; set; } = string.Empty;
        [Required]
        public decimal Price { get; set; }
        public string Image { get; set; } = string.Empty;
        [Required]
        public int GenreId { get; set; }
        public Genre Genre { get; set; } = null!;
        public List<OrderDetail> OrderDetail { get; set; } = [];
        public List<CartDetail> CartDetail { get; set; } = [];
        public Stock? Stock { get; set; }

        [NotMapped]
        public string GenreName { get; set; } = string.Empty;
        [NotMapped]
        public int Quantity { get; set; }

        public Book Clone()
        {
            return new Book
            {
                BookName = this.BookName,
                AuthorName = this.AuthorName,
                Price = this.Price,
                Image = this.Image,
                GenreId = this.GenreId
 
            };
        }
    }
}
