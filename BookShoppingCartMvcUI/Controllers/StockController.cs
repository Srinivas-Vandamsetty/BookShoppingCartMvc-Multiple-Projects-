using BookShoppingCart.Business.Services;
using BookShoppingCartMvcUI.Services;
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
        private readonly CommerceApiClient _api;

        public StockController(
            IStockService stockService,
            ILogger<StockController> logger, CommerceApiClient api)
        {
            _stockService = stockService;
            _logger = logger;
            _api = api;
        }

        // Show list of stocks
        public async Task<IActionResult> Index(string sTerm = "", int page = 1)
        {
            _logger.LogInformation("Fetching stock list. SearchTerm: {SearchTerm}", sTerm);

            try
            {
                var result = await _api.Get<CommercePage<StockDisplayModel>>(Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString($"admin/inventory?page={page}", "search", sTerm));
                var stocks = result.Items;
                ViewBag.Page = page;
                ViewBag.HasNext = result.HasNext;

                _logger.LogInformation("Fetched {Count} stock records", stocks.Count());

                return View(stocks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching stock list");

                TempData["errorMessage"] = "Failed to load stock data.";
                return View(new List<StockDisplayModel>());
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
                    OperationId = Guid.NewGuid(),
                    ExpectedQuantity = existingStock?.Quantity ?? 0,
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
            if (string.IsNullOrWhiteSpace(stock.Reason)) ModelState.AddModelError(nameof(stock.Reason), "Enter a reason for this stock adjustment.");
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
