using FastEndpoints;
using FluentValidation;
using BookShoppingCart.Models.Models.DTOs;
public class GenreRequestValidator : Validator<GenreDTO>
{
    public GenreRequestValidator()
    {
        RuleFor(x => x.GenreName).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Id).GreaterThanOrEqualTo(0);
    }
}
public class DeleteGenreValidator : Validator<DeleteGenreRequest>
{
    public DeleteGenreValidator() { RuleFor(x => x.Id).GreaterThan(0); }
}
public class GetGenreValidator : Validator<GetGenreByIdRequest>
{
    public GetGenreValidator() { RuleFor(x => x.Id).GreaterThan(0); }
}
