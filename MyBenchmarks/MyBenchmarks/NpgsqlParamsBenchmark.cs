using BenchmarkDotNet.Attributes;
using Npgsql;

namespace MyBenchmarks;

[MemoryDiagnoser]
public class NpgsqlParamsBenchmark
{
    private readonly NpgsqlDataSource _dataSource = NpgsqlDataSource.Create("Host=localhost;Username=test_user;Password=12345;Database=test_db");

    private const string PositionalCommandText = @"
        INSERT INTO test_table (data_1, data_2) VALUES ($1, $2)
    ";

    private const string NamedParamsCommandText = @"
        INSERT INTO test_table(data_1, data_2) VALUES (@first_value, @second_value)
    ";

    [Benchmark(Baseline = true)]
    [IterationCount(10)]
    public void NamedParams()
    {
        using var conn = _dataSource.OpenConnection();
        var cmd = new NpgsqlCommand(NamedParamsCommandText, conn)
        {
            Parameters =
            {
                new("first_value", Guid.NewGuid().ToString()),
                new("second_value", Guid.NewGuid().ToString()),
            }
        };
        cmd.ExecuteNonQuery();
    }

    [Benchmark]
    [IterationCount(10)]
    public void PositionalParams()
    {
        using var conn = _dataSource.OpenConnection();
        var cmd = new NpgsqlCommand(PositionalCommandText, conn)
        {
            Parameters =
            {
                new() { Value = Guid.NewGuid().ToString() },  // $1
                new() { Value = Guid.NewGuid().ToString() },  // $2
            }
        };
        cmd.ExecuteNonQuery();
    }
}
