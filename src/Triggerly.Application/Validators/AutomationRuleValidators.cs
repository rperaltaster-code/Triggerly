using FluentValidation;
using Triggerly.Application.Commands.AutomationRules;

namespace Triggerly.Application.Validators;

public class CreateAutomationRuleCommandValidator : AbstractValidator<CreateAutomationRuleCommand>
{
    public CreateAutomationRuleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.WorkflowId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.TriggerConfig).NotEmpty();
    }
}
