using BenchmarkDotNet.Running;

namespace MyBenchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<NpgsqlInsertBenchamrk>();
    }
}
