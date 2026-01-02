using FastEndpoints;
using FluentValidation;

public class BookRequestValidator : Validator<BookRequest>
{
    public BookRequestValidator()
    {
        RuleFor(x => x.BookName)
            .NotEmpty().WithMessage("Book name is required.")
            .MaximumLength(40).WithMessage("Book name must not exceed 40 characters.");

        RuleFor(x => x.AuthorName)
            .NotEmpty().WithMessage("Author name is required.")
            .MaximumLength(40).WithMessage("Author name must not exceed 40 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.");

        RuleFor(x => x.GenreId)
            .GreaterThan(0).WithMessage("Genre is required.");
    }
}
