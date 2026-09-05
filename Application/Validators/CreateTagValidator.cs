using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public class CreateTagValidator : AbstractValidator<CreateTagDto>
{
    public CreateTagValidator()
    {
        RuleFor(tag => tag.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}