using FluentValidation;

namespace Stim.Api.Models.Genre;

public class CreateGenreDtoValidator : AbstractValidator<CreateGenreDto>
{
    public CreateGenreDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Genre name is required.")
            .MaximumLength(50).WithMessage("Genre name must not exceed 50 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ImageUrl)
            .Must(BeAValidUrl).WithMessage("Image URL must be a valid HTTP or HTTPS URL.")
            .MaximumLength(500).WithMessage("Image URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl));
    }

    private static bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return true;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}