using Application.DTOs;
using FluentValidation;
using Domain.Models;

namespace Application.Validators;

public class CreateTaskValidator : AbstractValidator<CreateTaskDto>
{
    public CreateTaskValidator()
    {
        RuleFor(task => task.ProjectId)
            .NotEmpty();

        RuleFor(task => task.AssigneeIds)
            .Must(ids => ids is not null && ids.All(id => id != Guid.Empty))
            .WithMessage("AssigneeIds must not contain empty GUIDs.");

        RuleFor(task => task.AssigneeIds)
            .Must(ids => ids is not null && ids.Distinct().Count() == ids.Count)
            .WithMessage("AssigneeIds must not contain duplicates.");

        RuleFor(task => task.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(task => task.Description)
            .MaximumLength(2000);

        RuleFor(task => task.Status)
            .IsInEnum();

        RuleFor(task => task.Priority)
            .IsInEnum();

        RuleFor(task => task.Deadline)
            .Must(deadline => !deadline.HasValue || deadline.Value > DateTime.UtcNow)
            .WithMessage("Deadline must be in the future when provided.");
    }
}