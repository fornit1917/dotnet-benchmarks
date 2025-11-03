using System.Data.Common;

namespace MyBenchmarks.JobsOneTableVsSplitTable;

internal static class DataReaderExtensions
{
    public static JobExecutionModel? GetJob(this DbDataReader reader)
    {
        if (!reader.HasRows)
        {
            return null;
        }

        var hasRow = reader.Read();
        if (!hasRow)
        {
            return null;
        }

        var job = new JobExecutionModel
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")),
            JobName = reader.GetString(reader.GetOrdinal("job_name")),
            JobParam = reader.GetNullableString("job_param"),
            StartedCount = reader.GetInt32(reader.GetOrdinal("started_count")),
            NextJobId = reader.GetNullableGuid("next_job_id"),
        };
        return job;
    }

    public static string? GetNullableString(this DbDataReader reader, string columnName)
    {
        var index = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(index))
        {
            return null;
        }
        return reader.GetString(index);
    }

    public static DateTime? GetNullableDatetime(this DbDataReader reader, string columnName)
    {
        var index = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(index))
        {
            return null;
        }
        return reader.GetDateTime(index);
    }

    public static Guid? GetNullableGuid(this DbDataReader reader, string columnName)
    {
        var index = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(index))
        {
            return null;
        }
        return reader.GetGuid(index);
    }
}