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
            .NotEmpty();
    }
}