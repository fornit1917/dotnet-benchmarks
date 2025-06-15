using BenchmarkDotNet.Attributes;
using Npgsql;

namespace MyBenchmarks;

[MemoryDiagnoser]
public class NpgsqlParamsBenchmark
{
    private readonly NpgsqlDataSource _dataSource = NpgsqlDataSource.Create("Host=localhost;Username=test_user;Password=12345;Database=test_db");

    private const string PositionalCommandText = @"
        INSERT INTO test_table_2 (data_1, data_2, data_3, data_4, data_5) 
        VALUES ($1, $2, $3, $4, $5)
    ";

    private const string NamedParamsCommandText = @"
        INSERT INTO test_table_2 (data_1, data_2, data_3, data_4, data_5) 
        VALUES (@value_1, @value_2, @value_3, @value_4, @value_5)
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
                new("value_1", Guid.NewGuid().ToString()),
                new("value_2", Guid.NewGuid().ToString()),
                new("value_3", Guid.NewGuid().ToString()),
                new("value_4", Guid.NewGuid().ToString()),
                new("value_5", Guid.NewGuid().ToString()),
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
                new() { Value = Guid.NewGuid().ToString() },  // $3
                new() { Value = Guid.NewGuid().ToString() },  // $4
                new() { Value = Guid.NewGuid().ToString() },  // $5
            }
        };
        cmd.ExecuteNonQuery();
    }
}
