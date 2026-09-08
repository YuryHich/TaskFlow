using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public class CreateProjectValidator : AbstractValidator<CreateProjectDto>
{
    public CreateProjectValidator()
    {
        RuleFor(project => project.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(project => project.Description)
            .MaximumLength(1000);
    }
}