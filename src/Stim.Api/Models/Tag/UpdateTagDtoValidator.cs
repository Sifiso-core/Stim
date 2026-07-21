using FluentValidation;

namespace Stim.Api.Models.Tag;

public class UpdateTagDtoValidator : AbstractValidator<CreateTagDto>
{
    public UpdateTagDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tag name is required.")
            .MaximumLength(30).WithMessage("Tag name must not exceed 30 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(300).WithMessage("Description must not exceed 300 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}