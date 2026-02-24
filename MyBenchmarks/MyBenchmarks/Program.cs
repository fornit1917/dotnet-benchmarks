using BenchmarkDotNet.Running;
using MyBenchmarks.JobsOneTableVsSplitTable;

namespace MyBenchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        // BenchmarkRunner.Run<NpgsqlInsertBenchamrk>();
        // BenchmarkRunner.Run<NpgsqlParamsBenchmark>();
        // BenchmarkRunner.Run<MethodCallBenchmark>();
        // BenchmarkRunner.Run<JobsOneTableVsSplitTableBenchmark>();
        
        BenchmarkRunner.Run<ListVersusDictionaryBenchmark>();
    }
}
