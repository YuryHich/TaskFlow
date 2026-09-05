using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public class CreateCommentValidator : AbstractValidator<CreateCommentDto>
{
    public CreateCommentValidator()
    {
        RuleFor(comment => comment.TaskId)
            .NotEmpty();

        RuleFor(comment => comment.AuthorId)
            .NotEmpty();

        RuleFor(comment => comment.Content)
            .NotEmpty()
            .MaximumLength(2000);
    }
}