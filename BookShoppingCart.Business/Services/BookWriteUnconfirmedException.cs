namespace BookShoppingCart.Business.Services;

public sealed class BookWriteUnconfirmedException(Exception cause) : Exception(
    "We could not confirm the save. Check the book list before retrying.", cause);
