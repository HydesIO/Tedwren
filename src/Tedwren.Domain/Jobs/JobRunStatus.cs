namespace Tedwren.Domain.Jobs;

/// <summary>The outcome state of a scheduled job run (SF-21, R12).</summary>
public enum JobRunStatus
{
    /// <summary>Started and not yet finished.</summary>
    Running = 0,

    /// <summary>Completed successfully.</summary>
    Succeeded = 1,

    /// <summary>Failed with an error.</summary>
    Failed = 2,
}
