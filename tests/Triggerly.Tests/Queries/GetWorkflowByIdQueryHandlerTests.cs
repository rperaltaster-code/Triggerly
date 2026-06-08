using FluentAssertions;
using Moq;
using Triggerly.Application.Queries.Workflows;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.Models;
using Xunit;

namespace Triggerly.Tests.Queries;

public class GetWorkflowByIdQueryHandlerTests
{
    private readonly Mock<IWorkflowRepository> _workflowRepo = new();
    private readonly Mock<IWorkflowExecutionRepository> _executionRepo = new();
    private readonly GetWorkflowByIdQueryHandler _handler;

    public GetWorkflowByIdQueryHandlerTests()
    {
        _handler = new GetWorkflowByIdQueryHandler(_workflowRepo.Object, _executionRepo.Object);
    }

    [Fact]
    public async Task Handle_ExistingWorkflow_ReturnsDto()
    {
        var workflow = WorkflowDefinition.Create("My Workflow", "Desc", "tenant-1", "user-1");
        workflow.AddStep("Step A", StepType.Action, 0, []);

        _workflowRepo.Setup(r => r.GetByIdWithStepsAsync(workflow.Id, default)).ReturnsAsync(workflow);

        var result = await _handler.Handle(new GetWorkflowByIdQuery(workflow.Id, "tenant-1"), default);

        result.Should().NotBeNull();
        result!.Name.Should().Be("My Workflow");
        result.Steps.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WorkflowNotFound_ReturnsNull()
    {
        _workflowRepo.Setup(r => r.GetByIdWithStepsAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((WorkflowDefinition?)null);

        var result = await _handler.Handle(new GetWorkflowByIdQuery(Guid.NewGuid(), "tenant-1"), default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WrongTenant_ReturnsNull()
    {
        var workflow = WorkflowDefinition.Create("WF", "", "tenant-2", "user");
        _workflowRepo.Setup(r => r.GetByIdWithStepsAsync(workflow.Id, default)).ReturnsAsync(workflow);

        var result = await _handler.Handle(new GetWorkflowByIdQuery(workflow.Id, "tenant-1"), default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_StepsAreOrderedAscending()
    {
        var workflow = WorkflowDefinition.Create("WF", "", "tenant-1", "user");
        workflow.AddStep("Step 3", StepType.Action, 2, []);
        workflow.AddStep("Step 1", StepType.Action, 0, []);
        workflow.AddStep("Step 2", StepType.Approval, 1, []);

        _workflowRepo.Setup(r => r.GetByIdWithStepsAsync(workflow.Id, default)).ReturnsAsync(workflow);

        var result = await _handler.Handle(new GetWorkflowByIdQuery(workflow.Id, "tenant-1"), default);

        result!.Steps.Should().BeInAscendingOrder(s => s.Order);
    }
}
