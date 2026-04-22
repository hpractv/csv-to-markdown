namespace CsvToMarkdown.Api.Jobs;

public interface IJobStore
{
    Job Create();

    bool TryGet(string jobId, out Job? job);

    bool TrySetRunning(string jobId);

    bool TrySetSucceeded(string jobId, byte[] resultBytes);

    bool TrySetFailed(string jobId, string error);
}
