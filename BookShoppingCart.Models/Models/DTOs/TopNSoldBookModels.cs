using System;
using System.Collections.Generic;

namespace BookShoppingCart.Models.Models.DTOs;

public record TopNSoldBookModel(string BookName, string AuthorName, int TotalUnitSold);
public record TopNSoldBooksVm(DateTime StartDate, DateTime EndDate, IEnumerable<TopNSoldBookModel> TopNSoldBooks);