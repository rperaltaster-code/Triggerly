using FluentValidation;
using Triggerly.Application.Commands.Workflows;

namespace Triggerly.Application.Validators;

public class CreateWorkflowCommandValidator : AbstractValidator<CreateWorkflowCommand>
{
    public CreateWorkflowCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CreatedBy).NotEmpty();
        RuleForEach(x => x.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.Name).NotEmpty().MaximumLength(200);
            step.RuleFor(s => s.Order).GreaterThanOrEqualTo(0);
        });
    }
}

public class UpdateWorkflowCommandValidator : AbstractValidator<UpdateWorkflowCommand>
{
    public UpdateWorkflowCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}

public class TriggerWorkflowCommandValidator : AbstractValidator<TriggerWorkflowCommand>
{
    public TriggerWorkflowCommandValidator()
    {
        RuleFor(x => x.WorkflowId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
