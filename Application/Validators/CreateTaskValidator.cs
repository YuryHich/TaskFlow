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

        RuleFor(task => task.AssigneeId)
            .Must(assigneeId => !assigneeId.HasValue || assigneeId.Value != Guid.Empty)
            .WithMessage("AssigneeId must be a valid GUID when provided.");

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