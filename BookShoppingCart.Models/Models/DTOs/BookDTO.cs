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
    public string BookName { get; set; }

    [Required]
    [MaxLength(40)]
    public string AuthorName { get; set; }
    [Required]
    public double Price { get; set; }
    public string Image { get; set; }
    [Required]
    public int GenreId { get; set; }
    public IFormFile? ImageFile { get; set; }
    public IEnumerable<SelectListItem>? GenreList { get; set; }
}
