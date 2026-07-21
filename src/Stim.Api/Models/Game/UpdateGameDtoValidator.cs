using FluentValidation;

namespace Stim.Api.Models.Game;

public class UpdateGameDtoValidator : AbstractValidator<CreateGameDto>
{
    public UpdateGameDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Game title is required.")
            .MaximumLength(150).WithMessage("Game title must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.")
            .LessThanOrEqualTo(10000).WithMessage("Price must be realistic (max 10,000).");

        RuleFor(x => x.ReleaseDateUtc)
            .NotEmpty().WithMessage("Release date is required.")
            .Must(BeUtc).WithMessage("Release date must be specified in UTC (Kind == DateTimeKind.Utc).");

        RuleFor(x => x.ImageUrl)
            .Must(BeAValidUrl).WithMessage("Image URL must be a valid HTTP or HTTPS URL.")
            .MaximumLength(500).WithMessage("Image URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl));

        RuleFor(x => x.DeveloperId)
            .NotEmpty().WithMessage("Developer ID is required.");
    }

    private static bool BeUtc(DateTime date)
    {
        return date.Kind == DateTimeKind.Utc;
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