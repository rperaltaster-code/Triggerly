using FluentAssertions;
using Moq;
using Triggerly.Application.Commands.AutomationRules;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.Models;
using Xunit;

namespace Triggerly.Tests.Commands;

public class CreateAutomationRuleCommandHandlerTests
{
    private readonly Mock<IAutomationRuleRepository> _ruleRepo = new();
    private readonly Mock<IWorkflowRepository> _workflowRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CreateAutomationRuleCommandHandler _handler;

    public CreateAutomationRuleCommandHandlerTests()
    {
        _handler = new CreateAutomationRuleCommandHandler(_ruleRepo.Object, _workflowRepo.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesRule()
    {
        var workflow = WorkflowDefinition.Create("Workflow A", "", "tenant-1", "user");
        var command = new CreateAutomationRuleCommand(
            "Daily Report", "Send daily report",
            TriggerType.Schedule, "{\"cron\":\"0 9 * * *\"}",
            workflow.Id, "tenant-1");

        _workflowRepo.Setup(r => r.GetByIdAsync(workflow.Id, default)).ReturnsAsync(workflow);
        _ruleRepo.Setup(r => r.AddAsync(It.IsAny<AutomationRule>(), default)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Daily Report");
        result.IsEnabled.Should().BeTrue();
        result.TriggerType.Should().Be(TriggerType.Schedule);
    }

    [Fact]
    public async Task Handle_WorkflowNotFound_Throws()
    {
        var command = new CreateAutomationRuleCommand(
            "Rule", "", TriggerType.Manual, "{}", Guid.NewGuid(), "tenant-1");
        _workflowRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((WorkflowDefinition?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(command, default));
    }

    [Fact]
    public async Task Handle_CrossTenantWorkflow_ThrowsUnauthorized()
    {
        var workflow = WorkflowDefinition.Create("WF", "", "tenant-2", "user");
        var command = new CreateAutomationRuleCommand("Rule", "", TriggerType.Manual, "{}", workflow.Id, "tenant-1");
        _workflowRepo.Setup(r => r.GetByIdAsync(workflow.Id, default)).ReturnsAsync(workflow);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, default));
    }
}
