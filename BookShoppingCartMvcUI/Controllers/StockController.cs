using BookShoppingCart.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCartMvcUI.Controllers
{
    // Restrict access to Admin role only
    [Authorize(Roles = nameof(Roles.Admin))]
    public class StockController : Controller
    {
        private readonly IStockService _stockService;

        // Constructor to inject stock service
        public StockController(IStockService stockService)
        {
            _stockService = stockService;
        }

        // Show list of stocks with optional search term
        public async Task<IActionResult> Index(string sTerm = "")
        {
            var stocks = await _stockService.GetStocks(sTerm);
            return View(stocks);
        }

        // Show form to add or update stock for a specific book
        public async Task<IActionResult> ManangeStock(int bookId)
        {
            // Get existing stock (if any) for the book
            var existingStock = await _stockService.GetStockByBookId(bookId);

            // Prepare stock model for the view
            var stock = new StockDTO
            {
                BookId = bookId,
                Quantity = existingStock != null ? existingStock.Quantity : 0
            };
            return View(stock);
        }

        // Save updated or new stock (POST method)
        [HttpPost]
        public async Task<IActionResult> ManangeStock(StockDTO stock)
        {
            // If form is invalid, return view with data
            if (!ModelState.IsValid)
                return View(stock);

            try
            {
                // Call service to manage stock
                await _stockService.ManageStock(stock);
                TempData["successMessage"] = "Stock is updated successfully.";
            }
            catch (Exception)
            {
                TempData["errorMessage"] = "Something went wrong!!";
            }

            // Redirect back to stock list
            return RedirectToAction(nameof(Index));
        }
    }
}
