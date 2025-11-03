using BenchmarkDotNet.Attributes;
using Npgsql;
using System.Text.Json;

namespace MyBenchmarks.JobsOneTableVsSplitTable;

[MemoryDiagnoser]
public class JobsOneTableVsSplitTableBenchmark
{
	[Params(JobCompletionMethod.Delete, JobCompletionMethod.UpdateStatus)]
	public JobCompletionMethod JobCompletionMethod { get; set; }

    private readonly NpgsqlDataSource _dataSource = NpgsqlDataSource.Create("Host=localhost;Username=test_user;Password=12345;Database=test_db");

	private readonly string JobParamSerialized = JsonSerializer.Serialize(new JobParam()
	{
		SomeStringParam = Guid.NewGuid().ToString(),
		SomeOtherStringParam = Guid.NewGuid().ToString(),
		SomeIntParam = 123
	});

    private const string InsertToFullCmd = @"
		INSERT INTO test_jobs_full
		(
			id,
			job_name,
			job_param,
			status,
			error,
			created_at,
			scheduled_start_at,
			started_count,
			next_job_id
		)
		VALUES
		(
			$1, $2, $3, $4, $5, $6, $7, $8, $9
		)
    ";

	private const string TakeToProcessingFromFullCmd = @"
		WITH 
			ready_jobs AS (
				SELECT id FROM test_jobs_full
				WHERE
					status = 1
					AND scheduled_start_at <= NOW()
				ORDER BY scheduled_start_at 
				LIMIT 1 
				FOR UPDATE SKIP LOCKED
			),
			updated AS (
				UPDATE test_jobs_full 
				SET
					status = 2,
					started_count = started_count + 1
				WHERE 
					id IN (SELECT id FROM ready_jobs)
				RETURNING id, job_name, job_param, started_count, next_job_id, scheduled_start_at	
			)
		SELECT id, job_name, job_param, started_count, next_job_id, scheduled_start_at
		FROM updated
		ORDER BY scheduled_start_at;
	";

	private const string DeleteFromFullCmd = @"DELETE FROM test_jobs_full WHERE id = $1";

	private const string UpdateStatusToCompletedInFullCmd = @"
		UPDATE test_jobs_full
		SET status = 3
		WHERE id = $1
	";

	private const string InsertToSplitReadOnlyCmd = @"
		INSERT INTO test_jobs_split_ro
		(
			id,
			job_name,
			job_param,
			created_at,
			next_job_id
		)
		VALUES
		(
			$1, $2, $3, $4, $5
		)
	";

	private const string InsertIntoSplitWritableCmd = @"
		INSERT INTO test_jobs_split_w
		(
			id,
			status,
			error,
			scheduled_start_at,
			started_count
		)
		VALUES
		(
			$1, $2, $3, $4, $5
		)
	";

	private const string TakeToProcessingFromSplitCmd = @"
		WITH 
			ready_jobs AS (
				SELECT id FROM test_jobs_split_w
				WHERE
					status = 1
					AND scheduled_start_at <= NOW()
				ORDER BY scheduled_start_at 
				LIMIT 1 
				FOR UPDATE SKIP LOCKED
			),
			updated AS (
				UPDATE test_jobs_split_w
				SET
					status = 2,
					started_count = started_count + 1
				WHERE 
					id IN (SELECT id FROM ready_jobs)
				RETURNING id, started_count, scheduled_start_at	
			)
		SELECT ro.id, ro.job_name, ro.job_param, updated.started_count, ro.next_job_id, updated.scheduled_start_at
		FROM updated
		INNER JOIN test_jobs_split_ro ro ON ro.id = updated.id;
	";

	private const string DeleteFromSplitReadOnlyCmd = "DELETE FROM test_jobs_split_ro WHERE id = $1";

    private const string DeleteFromSplitWritableCmd = "DELETE FROM test_jobs_split_w WHERE id = $1";

    private const string UpdateStatusToCompletedInSplitCmd = @"
		UPDATE test_jobs_split_w
		SET status = 3
		WHERE id = $1
	";

    [Benchmark]
    [IterationCount(20)]
    public void FullTable()
	{
		InsertToFull();
		var jobId = TakeToProcessingFromFull();
		if (!jobId.HasValue)
			throw new Exception("Not found ready to execute job");

		if (JobCompletionMethod == JobCompletionMethod.Delete)
			DeleteFromFull(jobId.Value);
		else
			UpdateStatusToCompletedInFull(jobId.Value);
	}

    [Benchmark]
    [IterationCount(20)]
    public void SplitTable()
    {
        InsertToSplit();
		var jobId = TakeBatchToProcessingFromSplit();
		if (!jobId.HasValue)
			throw new Exception("Not found ready to execute job");

		if (JobCompletionMethod == JobCompletionMethod.Delete)
			DeleteFromSplit(jobId.Value);
		else
			UpdateStatusToCompletedInSplit(jobId.Value);
	}

    private void InsertToFull()
	{
        using var conn = _dataSource.OpenConnection();

        var now = DateTime.UtcNow;

        using var cmd = new NpgsqlCommand(InsertToFullCmd, conn)
        {
            Parameters =
            {
                new() { Value = Guid.NewGuid() },
                new() { Value = "SomeJobName" },
                new() { Value = JobParamSerialized },
                new() { Value = 1 },
                new() { Value = DBNull.Value },
                new() { Value = now },
                new() { Value = now },
                new() { Value = 0 },
                new() { Value = Guid.NewGuid() },
            }
        };

        cmd.ExecuteNonQuery();
    }

	private Guid? TakeToProcessingFromFull()
	{
        using var conn = _dataSource.OpenConnection();
		using var cmd = new NpgsqlCommand(TakeToProcessingFromFullCmd, conn);
		using var dataReader = cmd.ExecuteReader();
		var job = dataReader.GetJob();
		return job?.Id;
    }

	private void DeleteFromFull(Guid id)
	{
        using var conn = _dataSource.OpenConnection();
		using var cmd = new NpgsqlCommand(DeleteFromFullCmd, conn)
		{
			Parameters =
			{
				new() { Value = id },
			}
		};
		cmd.ExecuteNonQuery();
    }

	private void UpdateStatusToCompletedInFull(Guid id)
	{
		using var comm = _dataSource.OpenConnection();
		using var cmd = new NpgsqlCommand(UpdateStatusToCompletedInFullCmd, comm)
		{
			Parameters =
			{
				new() { Value = id }
			}
		};
		cmd.ExecuteNonQuery();
	}

	private void InsertToSplit()
	{
        using var conn = _dataSource.OpenConnection();

        var now = DateTime.UtcNow;
        var jobId = Guid.NewGuid();

        using var batch = new NpgsqlBatch(conn);
        var insertToRo = new NpgsqlBatchCommand(InsertToSplitReadOnlyCmd)
        {
            Parameters =
            {
                new() { Value = jobId },
                new() { Value = "SomeJobName" },
                new() { Value = JobParamSerialized },
                new() { Value = now },
                new() { Value = Guid.NewGuid() },
            }
        };
        var insertToW = new NpgsqlBatchCommand(InsertIntoSplitWritableCmd)
        {
            Parameters =
            {
                new() { Value = jobId },
                new() { Value = 1 },
                new() { Value = DBNull.Value },
                new() { Value = now },
                new() { Value = 0 },
            }
        };
        batch.BatchCommands.Add(insertToRo);
        batch.BatchCommands.Add(insertToW);

        batch.ExecuteNonQuery();
    }

	private Guid? TakeBatchToProcessingFromSplit()
	{
        using var conn = _dataSource.OpenConnection();
        using var cmd = new NpgsqlCommand(TakeToProcessingFromSplitCmd, conn);
        using var dataReader = cmd.ExecuteReader();
        var job = dataReader.GetJob();
        return job?.Id;
    }

	private void DeleteFromSplit(Guid id)
	{
        using var conn = _dataSource.OpenConnection();
		using var batch = new NpgsqlBatch(conn);
		var deleteFromRoCmd = new NpgsqlBatchCommand(DeleteFromSplitReadOnlyCmd)
		{
			Parameters =
			{
				new() { Value = id }
			}
		};
		var deleteFromWCmd = new NpgsqlBatchCommand(DeleteFromSplitWritableCmd)
        {
            Parameters =
            {
                new() { Value = id }
            }
        };
		batch.BatchCommands.Add(deleteFromRoCmd);
		batch.BatchCommands.Add(deleteFromWCmd);
		batch.ExecuteNonQuery();
    }

    private void UpdateStatusToCompletedInSplit(Guid id)
    {
        using var comm = _dataSource.OpenConnection();
        using var cmd = new NpgsqlCommand(UpdateStatusToCompletedInSplitCmd, comm)
        {
            Parameters =
            {
                new() { Value = id }
            }
        };
        cmd.ExecuteNonQuery();
    }
}
