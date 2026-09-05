using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public class UpdateTagValidator : AbstractValidator<UpdateTagDto>
{
    public UpdateTagValidator()
    {
        RuleFor(tag => tag.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}