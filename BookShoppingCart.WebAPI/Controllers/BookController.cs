using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCart.WebAPI.Controllers
{
    public class BookController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
