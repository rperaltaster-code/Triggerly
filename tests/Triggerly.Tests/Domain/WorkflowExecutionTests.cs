using FluentAssertions;
using Triggerly.Domain.Entities;
using Triggerly.Shared.Models;
using Xunit;

namespace Triggerly.Tests.Domain;

public class WorkflowExecutionTests
{
    [Fact]
    public void Create_ReturnsExecutionInPendingState()
    {
        var execution = WorkflowExecution.Create(Guid.NewGuid(), "wf-123", "tenant-1", "user-1", null);

        execution.Status.Should().Be(ExecutionStatus.Pending);
        execution.TemporalWorkflowId.Should().Be("wf-123");
        execution.InputData.Should().BeEmpty();
    }

    [Fact]
    public void Start_TransitionsToRunning()
    {
        var execution = WorkflowExecution.Create(Guid.NewGuid(), "wf-1", "tenant-1", null, null);
        execution.Start("run-abc");

        execution.Status.Should().Be(ExecutionStatus.Running);
        execution.TemporalRunId.Should().Be("run-abc");
    }

    [Fact]
    public void RequestApproval_TransitionsToWaitingApproval()
    {
        var execution = WorkflowExecution.Create(Guid.NewGuid(), "wf-1", "tenant-1", null, null);
        execution.Start("run-1");
        execution.RequestApproval();

        execution.Status.Should().Be(ExecutionStatus.WaitingApproval);
    }

    [Fact]
    public void Approve_WhenWaitingApproval_TransitionsToApproved()
    {
        var execution = WorkflowExecution.Create(Guid.NewGuid(), "wf-1", "tenant-1", null, null);
        execution.Start("run-1");
        execution.RequestApproval();
        execution.Approve();

        execution.Status.Should().Be(ExecutionStatus.Approved);
    }

    [Fact]
    public void Approve_NotWaitingApproval_Throws()
    {
        var execution = WorkflowExecution.Create(Guid.NewGuid(), "wf-1", "tenant-1", null, null);
        execution.Start("run-1");

        var act = () => execution.Approve();
        act.Should().Throw<InvalidOperationException>().WithMessage("*awaiting approval*");
    }

    [Fact]
    public void Reject_SetsStatusAndErrorMessage()
    {
        var execution = WorkflowExecution.Create(Guid.NewGuid(), "wf-1", "tenant-1", null, null);
        execution.Start("run-1");
        execution.RequestApproval();
        execution.Reject("Budget exceeded");

        execution.Status.Should().Be(ExecutionStatus.Rejected);
        execution.ErrorMessage.Should().Be("Budget exceeded");
        execution.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_SetsStatusAndOutputData()
    {
        var execution = WorkflowExecution.Create(Guid.NewGuid(), "wf-1", "tenant-1", null, null);
        execution.Start("run-1");
        execution.Complete(new Dictionary<string, object> { ["result"] = "ok" });

        execution.Status.Should().Be(ExecutionStatus.Completed);
        execution.OutputData.Should().ContainKey("result");
        execution.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Fail_SetsStatusAndErrorMessage()
    {
        var execution = WorkflowExecution.Create(Guid.NewGuid(), "wf-1", "tenant-1", null, null);
        execution.Start("run-1");
        execution.Fail("Timeout error");

        execution.Status.Should().Be(ExecutionStatus.Failed);
        execution.ErrorMessage.Should().Be("Timeout error");
    }

    [Fact]
    public void Cancel_SetsStatusCancelled()
    {
        var execution = WorkflowExecution.Create(Guid.NewGuid(), "wf-1", "tenant-1", null, null);
        execution.Start("run-1");
        execution.Cancel();

        execution.Status.Should().Be(ExecutionStatus.Cancelled);
        execution.CompletedAt.Should().NotBeNull();
    }
}
