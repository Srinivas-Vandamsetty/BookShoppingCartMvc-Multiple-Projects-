namespace BookShoppingCart.Models.Models.DTOs;
public static class CommerceMapping
{
    public static BookResponse ToResponse(this Book book) => new(book.Id, book.BookName, book.AuthorName, book.Price,
        book.GenreId, book.Genre?.GenreName ?? book.GenreName, book.Image, book.Stock?.Quantity ?? book.Quantity, book.IsArchived);
    public static Book ToModel(this BookResponse book) => new()
    {
        Id = book.Id, BookName = book.BookName, AuthorName = book.AuthorName, Price = book.Price, GenreId = book.GenreId,
        GenreName = book.GenreName, Genre = new Genre { Id = book.GenreId, GenreName = book.GenreName },
        Image = book.Image, Quantity = book.Quantity, Stock = new Stock { BookId = book.Id, Quantity = book.Quantity }, IsArchived = book.IsArchived
    };
    public static OrderResponse ToResponse(this Order order) => new(order.Id, order.CreateDate, order.OrderStatusId,
        order.OrderStatus.StatusName, order.Subtotal, order.Discount, order.Shipping, order.Total, order.CouponCode,
        order.Name, order.Email, order.MobileNumber, order.Address, order.PaymentMethod, order.IsPaid,
        order.OrderDetail.Select(d => new OrderLineResponse(d.BookId, string.IsNullOrEmpty(d.BookName) ? d.Book?.BookName ?? "" : d.BookName,
            string.IsNullOrEmpty(d.AuthorName) ? d.Book?.AuthorName ?? "" : d.AuthorName,
            string.IsNullOrEmpty(d.GenreName) ? d.Book?.Genre?.GenreName ?? "" : d.GenreName, d.Quantity, d.UnitPrice, d.Book?.Image ?? "")).ToList());
    public static Order ToModel(this OrderResponse order) => new()
    {
        Id = order.Id, CreateDate = order.CreateDate, OrderStatusId = order.OrderStatusId,
        OrderStatus = new OrderStatus { Id = order.OrderStatusId, StatusName = order.Status },
        Subtotal = order.Subtotal, Discount = order.Discount, Shipping = order.Shipping, Total = order.Total,
        CouponCode = order.CouponCode, Name = order.Name, Email = order.Email, MobileNumber = order.MobileNumber,
        Address = order.Address, PaymentMethod = order.PaymentMethod, IsPaid = order.IsPaid,
        OrderDetail = order.Items.Select(d => new OrderDetail { BookId = d.BookId, BookName = d.BookName, AuthorName = d.AuthorName,
            GenreName = d.GenreName, Quantity = d.Quantity, UnitPrice = d.UnitPrice,
            Book = new Book { Id = d.BookId, BookName = d.BookName, AuthorName = d.AuthorName, Image = d.Image,
                Genre = new Genre { GenreName = d.GenreName } } }).ToList()
    };
}
