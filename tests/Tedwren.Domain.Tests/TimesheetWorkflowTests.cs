using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Domain.Tests;

/// <summary>
/// Verifies the timesheet lifecycle rules (SUB-8): what can be submitted, approved, returned and corrected at
/// each state. A returned timesheet can be resubmitted; an approved one is terminal and cannot be corrected.
/// </summary>
public sealed class TimesheetWorkflowTests
{
    [Theory]
    [InlineData(TimesheetStatus.Draft, true)]
    [InlineData(TimesheetStatus.Returned, true)]
    [InlineData(TimesheetStatus.Submitted, false)]
    [InlineData(TimesheetStatus.Approved, false)]
    public void CanSubmit_OnlyFromDraftOrReturned(TimesheetStatus status, bool expected) =>
        Assert.Equal(expected, TimesheetWorkflow.CanSubmit(status));

    [Theory]
    [InlineData(TimesheetStatus.Submitted, true)]
    [InlineData(TimesheetStatus.Draft, false)]
    [InlineData(TimesheetStatus.Approved, false)]
    [InlineData(TimesheetStatus.Returned, false)]
    public void CanApprove_OnlyFromSubmitted(TimesheetStatus status, bool expected) =>
        Assert.Equal(expected, TimesheetWorkflow.CanApprove(status));

    [Theory]
    [InlineData(TimesheetStatus.Submitted, true)]
    [InlineData(TimesheetStatus.Draft, false)]
    [InlineData(TimesheetStatus.Approved, false)]
    public void CanReturn_OnlyFromSubmitted(TimesheetStatus status, bool expected) =>
        Assert.Equal(expected, TimesheetWorkflow.CanReturn(status));

    [Theory]
    [InlineData(TimesheetStatus.Draft, true)]
    [InlineData(TimesheetStatus.Submitted, true)]
    [InlineData(TimesheetStatus.Returned, true)]
    [InlineData(TimesheetStatus.Approved, false)]
    public void CanCorrect_UntilApproved(TimesheetStatus status, bool expected) =>
        Assert.Equal(expected, TimesheetWorkflow.CanCorrect(status));
}
