using BookShoppingCart.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookShoppingCartMvcUI.Controllers
{
    [Authorize(Roles = nameof(Roles.Admin))]
    public class StockController : Controller
    {
        private readonly IStockService _stockService;
        private readonly ILogger<StockController> _logger;

        public StockController(
            IStockService stockService,
            ILogger<StockController> logger)
        {
            _stockService = stockService;
            _logger = logger;
        }

        // Show list of stocks
        public async Task<IActionResult> Index(string sTerm = "")
        {
            _logger.LogInformation("Fetching stock list. SearchTerm: {SearchTerm}", sTerm);

            try
            {
                var stocks = await _stockService.GetStocks(sTerm);

                _logger.LogInformation("Fetched {Count} stock records", stocks.Count());

                return View(stocks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching stock list");

                TempData["errorMessage"] = "Failed to load stock data.";
                return View(new List<StockDTO>());
            }
        }

        // Load ManageStock page
        public async Task<IActionResult> ManangeStock(int bookId)
        {
            _logger.LogInformation("Loading ManageStock page for BookId: {BookId}", bookId);

            try
            {
                var existingStock = await _stockService.GetStockByBookId(bookId);

                var stock = new StockDTO
                {
                    BookId = bookId,
                    Quantity = existingStock != null ? existingStock.Quantity : 0
                };

                return View(stock);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading stock for BookId: {BookId}", bookId);

                TempData["errorMessage"] = "Failed to load stock details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Save stock (POST)
        [HttpPost]
        public async Task<IActionResult> ManangeStock(StockDTO stock)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state while managing stock for BookId: {BookId}", stock.BookId);
                return View(stock);
            }

            _logger.LogInformation(
                "Updating stock. BookId: {BookId}, Quantity: {Quantity}",
                stock.BookId, stock.Quantity);

            try
            {
                await _stockService.ManageStock(stock);

                _logger.LogInformation(
                    "Stock updated successfully for BookId: {BookId}",
                    stock.BookId);

                TempData["successMessage"] = "Stock is updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error updating stock for BookId: {BookId}",
                    stock.BookId);

                TempData["errorMessage"] = "Something went wrong!!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}