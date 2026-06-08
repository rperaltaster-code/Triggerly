using FluentAssertions;
using Triggerly.Domain.Entities;
using Triggerly.Shared.Models;
using Xunit;

namespace Triggerly.Tests.Domain;

public class WorkflowDefinitionTests
{
    [Fact]
    public void Create_ValidParams_ReturnsWorkflowInDraftStatus()
    {
        var wf = WorkflowDefinition.Create("Onboarding", "Employee onboarding", "tenant-1", "admin");

        wf.Name.Should().Be("Onboarding");
        wf.Status.Should().Be(WorkflowStatus.Draft);
        wf.Version.Should().Be(1);
        wf.TenantId.Should().Be("tenant-1");
        wf.Steps.Should().BeEmpty();
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var act = () => WorkflowDefinition.Create("", "Desc", "tenant-1", "user");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Activate_WithSteps_ChangesStatusAndIncrementsVersion()
    {
        var wf = WorkflowDefinition.Create("WF", "", "tenant-1", "user");
        wf.AddStep("Step 1", StepType.Action, 0, []);

        wf.Activate();

        wf.Status.Should().Be(WorkflowStatus.Active);
        wf.Version.Should().Be(2);
    }

    [Fact]
    public void Activate_WithNoSteps_Throws()
    {
        var wf = WorkflowDefinition.Create("WF", "", "tenant-1", "user");
        var act = () => wf.Activate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*steps*");
    }

    [Fact]
    public void Activate_ArchivedWorkflow_Throws()
    {
        var wf = WorkflowDefinition.Create("WF", "", "tenant-1", "user");
        wf.AddStep("S", StepType.Action, 0, []);
        wf.Activate();
        wf.Archive();

        var act = () => wf.Activate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*archived*");
    }

    [Fact]
    public void Deactivate_ActiveWorkflow_ChangesStatus()
    {
        var wf = WorkflowDefinition.Create("WF", "", "tenant-1", "user");
        wf.AddStep("S", StepType.Action, 0, []);
        wf.Activate();

        wf.Deactivate();

        wf.Status.Should().Be(WorkflowStatus.Inactive);
    }

    [Fact]
    public void Deactivate_DraftWorkflow_Throws()
    {
        var wf = WorkflowDefinition.Create("WF", "", "tenant-1", "user");
        var act = () => wf.Deactivate();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddStep_ToActiveWorkflow_AddsStep()
    {
        var wf = WorkflowDefinition.Create("WF", "", "tenant-1", "user");
        var step = wf.AddStep("Step A", StepType.Approval, 0, new Dictionary<string, object> { ["timeout"] = 3600 });

        wf.Steps.Should().HaveCount(1);
        step.Name.Should().Be("Step A");
        step.Type.Should().Be(StepType.Approval);
    }

    [Fact]
    public void AddStep_ToArchivedWorkflow_Throws()
    {
        var wf = WorkflowDefinition.Create("WF", "", "tenant-1", "user");
        wf.AddStep("S", StepType.Action, 0, []);
        wf.Activate();
        wf.Archive();

        var act = () => wf.AddStep("S2", StepType.Action, 1, []);
        act.Should().Throw<InvalidOperationException>().WithMessage("*archived*");
    }

    [Fact]
    public void RemoveStep_ExistingStep_RemovesIt()
    {
        var wf = WorkflowDefinition.Create("WF", "", "tenant-1", "user");
        var step = wf.AddStep("Step A", StepType.Action, 0, []);

        wf.RemoveStep(step.Id);

        wf.Steps.Should().BeEmpty();
    }

    [Fact]
    public void RemoveStep_NonExistentStep_Throws()
    {
        var wf = WorkflowDefinition.Create("WF", "", "tenant-1", "user");
        var act = () => wf.RemoveStep(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public void Create_EmitsDomainEvent()
    {
        var wf = WorkflowDefinition.Create("WF", "", "tenant-1", "user");
        wf.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void Activate_EmitsDomainEvent()
    {
        var wf = WorkflowDefinition.Create("WF", "", "tenant-1", "user");
        wf.AddStep("S", StepType.Action, 0, []);
        wf.ClearDomainEvents();

        wf.Activate();

        wf.DomainEvents.Should().HaveCount(1);
    }
}
