using System.Collections.Concurrent;

namespace CsvToMarkdown.Api.Jobs;

public sealed class InMemoryJobStore : IJobStore
{
    private readonly ConcurrentDictionary<string, Job> jobs = new();

    public Job Create()
    {
        var job = new Job
        {
            Id = Guid.NewGuid().ToString("N"),
            Status = JobStatus.Queued
        };

        jobs[job.Id] = job;
        return job;
    }

    public bool TryGet(string jobId, out Job? job)
    {
        return jobs.TryGetValue(jobId, out job);
    }

    public bool TrySetRunning(string jobId)
    {
        return TryUpdate(jobId, job => job with
        {
            Status = JobStatus.Running,
            ResultBytes = null,
            Error = null
        });
    }

    public bool TrySetSucceeded(string jobId, byte[] resultBytes)
    {
        ArgumentNullException.ThrowIfNull(resultBytes);

        return TryUpdate(jobId, job => job with
        {
            Status = JobStatus.Succeeded,
            ResultBytes = resultBytes,
            Error = null
        });
    }

    public bool TrySetFailed(string jobId, string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        return TryUpdate(jobId, job => job with
        {
            Status = JobStatus.Failed,
            ResultBytes = null,
            Error = error
        });
    }

    private bool TryUpdate(string jobId, Func<Job, Job> update)
    {
        while (jobs.TryGetValue(jobId, out var current))
        {
            var next = update(current);
            if (jobs.TryUpdate(jobId, next, current))
            {
                return true;
            }
        }

        return false;
    }
}
