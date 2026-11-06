using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookShoppingCart.Models.Models.DTOs;
public class BookDTO
{
    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    public string BookName { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string AuthorName { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "999999999.99")]
    public decimal Price { get; set; }
    public string Image { get; set; } = string.Empty;
    [Range(1, int.MaxValue)]
    public int GenreId { get; set; }
    public IFormFile? ImageFile { get; set; }
    public IEnumerable<SelectListItem>? GenreList { get; set; }
}
