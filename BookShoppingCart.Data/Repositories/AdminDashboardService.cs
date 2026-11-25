using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingCart.Data.Repositories;

/// <summary>Builds read-only operational metrics for the administrator dashboard.</summary>
public sealed class AdminDashboardService(ApplicationDbContext db)
{
    private const int TrendDays = 30;
    private const int LowStockThreshold = 5;

    public async Task<AdminDashboardResponse> GetAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var currentStart = today.AddDays(1 - TrendDays);
        var previousStart = currentStart.AddDays(-TrendDays);

        var activeOrders = db.Orders.AsNoTracking().Where(order => !order.IsDeleted);
        var paidOrders = activeOrders.Where(order => order.IsPaid && order.OrderStatus.StatusName != "Cancelled");

        var orderCount = await activeOrders.CountAsync(cancellationToken);
        var paidSales = await paidOrders.SumAsync(order => (decimal?)order.Total, cancellationToken) ?? 0m;
        var bookCount = await db.Books.AsNoTracking().CountAsync(book => !book.IsArchived, cancellationToken);
        var customerCount = await activeOrders.Select(order => order.UserId).Distinct().CountAsync(cancellationToken);
        var lowStockCount = await db.Books.AsNoTracking()
            .CountAsync(book => !book.IsArchived && (book.Stock == null || book.Stock.Quantity <= LowStockThreshold), cancellationToken);

        var currentOrders = await activeOrders.CountAsync(order => order.CreateDate >= currentStart, cancellationToken);
        var previousOrders = await activeOrders.CountAsync(order => order.CreateDate >= previousStart && order.CreateDate < currentStart, cancellationToken);
        var currentSales = await paidOrders.Where(order => order.CreateDate >= currentStart)
            .SumAsync(order => (decimal?)order.Total, cancellationToken) ?? 0m;
        var previousSales = await paidOrders.Where(order => order.CreateDate >= previousStart && order.CreateDate < currentStart)
            .SumAsync(order => (decimal?)order.Total, cancellationToken) ?? 0m;

        var revenueRows = await paidOrders
            .Where(order => order.CreateDate >= currentStart)
            .GroupBy(order => order.CreateDate.Date)
            .Select(group => new { Date = group.Key, Revenue = group.Sum(order => order.Total) })
            .ToListAsync(cancellationToken);
        var revenueByDate = revenueRows.ToDictionary(row => row.Date, row => row.Revenue);
        var revenue = Enumerable.Range(0, TrendDays)
            .Select(offset =>
            {
                var date = currentStart.AddDays(offset);
                return new DashboardRevenuePoint(date, revenueByDate.GetValueOrDefault(date));
            })
            .ToList();

        var categoryRows = await db.OrderDetails.AsNoTracking()
            .Where(line => !line.Order.IsDeleted && line.Order.IsPaid && line.Order.OrderStatus.StatusName != "Cancelled")
            .GroupBy(line => string.IsNullOrWhiteSpace(line.GenreName) ? "Uncategorised" : line.GenreName)
            .Select(group => new { Category = group.Key, Sales = group.Sum(line => line.UnitPrice * line.Quantity) })
            .OrderByDescending(row => row.Sales)
            .ToListAsync(cancellationToken);
        var categoryTotal = categoryRows.Sum(row => row.Sales);
        var categories = categoryRows.Take(5)
            .Select(row => new DashboardCategoryMetric(row.Category, row.Sales, Percentage(row.Sales, categoryTotal)))
            .ToList();
        var otherSales = categoryRows.Skip(5).Sum(row => row.Sales);
        if (otherSales > 0)
            categories.Add(new DashboardCategoryMetric("Other", otherSales, Percentage(otherSales, categoryTotal)));

        var recentOrders = await activeOrders.OrderByDescending(order => order.CreateDate).ThenByDescending(order => order.Id)
            .Take(5)
            .Select(order => new DashboardRecentOrder(order.Id, order.Name,
                order.OrderDetail.Sum(line => (int?)line.Quantity) ?? 0, order.Total, order.PaymentMethod,
                order.IsPaid, order.OrderStatus.StatusName, order.CreateDate))
            .ToListAsync(cancellationToken);

        return new AdminDashboardResponse(orderCount, paidSales, bookCount, customerCount,
            Percentage(currentOrders - previousOrders, previousOrders), Percentage(currentSales - previousSales, previousSales),
            lowStockCount, revenue, categories, recentOrders);
    }

    private static decimal Percentage(decimal difference, decimal baseline) =>
        baseline == 0 ? 0m : Math.Round(difference / baseline * 100m, 1, MidpointRounding.AwayFromZero);
}
