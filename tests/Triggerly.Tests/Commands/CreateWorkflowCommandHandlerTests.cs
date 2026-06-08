using FluentAssertions;
using Moq;
using Triggerly.Application.Commands.Workflows;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.Models;
using Xunit;

namespace Triggerly.Tests.Commands;

public class CreateWorkflowCommandHandlerTests
{
    private readonly Mock<IWorkflowRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly CreateWorkflowCommandHandler _handler;

    public CreateWorkflowCommandHandlerTests()
    {
        _handler = new CreateWorkflowCommandHandler(_repositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesWorkflowAndReturnsDto()
    {
        var command = new CreateWorkflowCommand(
            "Invoice Approval",
            "Automated invoice approval workflow",
            "tenant-1",
            "user-1",
            [
                new CreateWorkflowStepRequest("Validate Invoice", StepType.Action, 0, null, null),
                new CreateWorkflowStepRequest("Manager Approval", StepType.Approval, 1, null, "manager@company.com"),
                new CreateWorkflowStepRequest("Send Notification", StepType.Notification, 2, null, null)
            ]);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<WorkflowDefinition>(), default))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Invoice Approval");
        result.TenantId.Should().Be("tenant-1");
        result.Status.Should().Be(WorkflowStatus.Draft);
        result.Steps.Should().HaveCount(3);
        result.Steps.Should().BeInAscendingOrder(s => s.Order);
    }

    [Fact]
    public async Task Handle_WithNoSteps_CreatesWorkflowWithEmptySteps()
    {
        var command = new CreateWorkflowCommand("Empty Workflow", "", "tenant-1", "user-1", []);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<WorkflowDefinition>(), default))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Steps.Should().BeEmpty();
        result.Status.Should().Be(WorkflowStatus.Draft);
    }

    [Fact]
    public async Task Handle_PersistsWorkflow()
    {
        var command = new CreateWorkflowCommand("Test", "Desc", "tenant-1", "user-1", []);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await _handler.Handle(command, CancellationToken.None);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<WorkflowDefinition>(), default), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
