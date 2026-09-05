using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public class UpdateCommentValidator : AbstractValidator<UpdateCommentDto>
{
    public UpdateCommentValidator()
    {
        RuleFor(comment => comment.Content)
            .NotEmpty()
            .MaximumLength(2000);
    }
}