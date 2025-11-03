namespace MyBenchmarks.JobsOneTableVsSplitTable;

internal class JobExecutionModel
{
    public Guid Id { get; init; }
    public string JobName { get; init; } = string.Empty;
    public string? JobParam { get; init; }
    public int StartedCount { get; init; }
    public Guid? NextJobId { get; init; }
}
