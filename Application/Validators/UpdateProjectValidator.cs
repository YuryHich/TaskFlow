using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public class UpdateProjectValidator : AbstractValidator<UpdateProjectDto>
{
    public UpdateProjectValidator()
    {
        RuleFor(project => project.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(project => project.Description)
            .MaximumLength(1000);

        RuleFor(project => project.OwnerId)
            .Must(ownerId => !ownerId.HasValue || ownerId.Value != Guid.Empty)
            .WithMessage("OwnerId must be a valid GUID when provided.");
    }
}