using FluentAssertions;
using Triggerly.Domain.Entities;
using Triggerly.Shared.Models;
using Xunit;

namespace Triggerly.Tests.Domain;

public class AutomationRuleTests
{
    [Fact]
    public void Create_ValidParams_ReturnsEnabledRule()
    {
        var rule = AutomationRule.Create(
            "New Employee Rule", "Trigger onboarding",
            TriggerType.Event, "{}", Guid.NewGuid(), "tenant-1");

        rule.Name.Should().Be("New Employee Rule");
        rule.IsEnabled.Should().BeTrue();
        rule.ExecutionCount.Should().Be(0);
        rule.LastTriggeredAt.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var act = () => AutomationRule.Create("", "", TriggerType.Manual, "{}", Guid.NewGuid(), "tenant-1");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Disable_EnabledRule_DisablesIt()
    {
        var rule = AutomationRule.Create("R", "", TriggerType.Manual, "{}", Guid.NewGuid(), "tenant-1");
        rule.Disable();
        rule.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Enable_DisabledRule_EnablesIt()
    {
        var rule = AutomationRule.Create("R", "", TriggerType.Manual, "{}", Guid.NewGuid(), "tenant-1");
        rule.Disable();
        rule.Enable();
        rule.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void RecordTrigger_IncrementsCountAndSetsTimestamp()
    {
        var rule = AutomationRule.Create("R", "", TriggerType.Schedule, "{}", Guid.NewGuid(), "tenant-1");
        var before = DateTime.UtcNow;

        rule.RecordTrigger();

        rule.ExecutionCount.Should().Be(1);
        rule.LastTriggeredAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void RecordTrigger_CalledMultipleTimes_AccumulatesCount()
    {
        var rule = AutomationRule.Create("R", "", TriggerType.Webhook, "{}", Guid.NewGuid(), "tenant-1");

        rule.RecordTrigger();
        rule.RecordTrigger();
        rule.RecordTrigger();

        rule.ExecutionCount.Should().Be(3);
    }
}
