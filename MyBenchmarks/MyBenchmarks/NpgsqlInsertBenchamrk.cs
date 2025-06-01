using BenchmarkDotNet.Attributes;
using Npgsql;

namespace MyBenchmarks;

[MemoryDiagnoser]
public class NpgsqlInsertBenchamrk
{
    private readonly NpgsqlDataSource _dataSource;

    private const int Count = 10;

    private const string InsertCommandText = @"
        INSERT INTO test_table (data_1, data_2) VALUES ($1, $2)
    ";

    private const string CopyCommandText = @"
        COPY test_table (data_1, data_2) FROM STDIN (FORMAT BINARY)    
    ";

    public NpgsqlInsertBenchamrk()
    {
        _dataSource = NpgsqlDataSource.Create("Host=localhost;Username=test_user;Password=12345;Database=test_db");
    }

    [Benchmark]
    [IterationCount(10)]
    public void SeparatedQueries()
    {
        using var conn = _dataSource.OpenConnection();
        for (int i = 0; i < Count; i++)
        {
            var cmd = new NpgsqlCommand(InsertCommandText, conn)
            {
                Parameters =
                {
                    new() { Value = Guid.NewGuid().ToString() },
                    new() { Value = Guid.NewGuid().ToString() },
                }
            };
            cmd.ExecuteNonQuery();
        }
    }

    [Benchmark]
    [IterationCount(10)]
    public void SeparatedQueriesInTransaction()
    {
        using var conn = _dataSource.OpenConnection();
        using var transaction = conn.BeginTransaction();
        for (int i = 0; i < Count; i++)
        {
            var cmd = new NpgsqlCommand(InsertCommandText, conn, transaction)
            {
                Parameters =
                {
                    new() { Value = Guid.NewGuid().ToString() },
                    new() { Value = Guid.NewGuid().ToString() },
                }
            };
            cmd.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    [Benchmark]
    [IterationCount(10)]
    public void NpgsqlBatchCommand()
    {
        using var conn = _dataSource.OpenConnection();
        using var batch = new NpgsqlBatch(conn);
        for (int i = 0; i < Count; i++)
        {
            var cmd = new NpgsqlBatchCommand(InsertCommandText)
            {
                Parameters =
                {
                    new() { Value = Guid.NewGuid().ToString() },
                    new() { Value = Guid.NewGuid().ToString() },
                }
            };
            batch.BatchCommands.Add(cmd);
        }
        batch.ExecuteNonQuery();
    }

    [Benchmark]
    [IterationCount(10)]
    public void Copy()
    {
        using var conn = _dataSource.OpenConnection();
        using var writer = conn.BeginBinaryImport(CopyCommandText);
        for (int i = 0; i <= Count; i++)
        {
            writer.StartRow();
            writer.Write(Guid.NewGuid().ToString());
            writer.Write(Guid.NewGuid().ToString());
        }
        writer.Complete();
    }
}
