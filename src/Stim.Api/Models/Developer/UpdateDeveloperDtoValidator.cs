using FluentValidation;

namespace Stim.Api.Models.Developer;

public class UpdateDeveloperDtoValidator : AbstractValidator<CreateDeveloperDto>
{
    public UpdateDeveloperDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Developer name is required.")
            .MaximumLength(100).WithMessage("Developer name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.WebsiteUrl)
            .Must(BeAValidUrl).WithMessage("Website URL must be a valid HTTP or HTTPS URL.")
            .MaximumLength(200).WithMessage("Website URL must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.WebsiteUrl));
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
