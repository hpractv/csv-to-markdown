namespace CsvToMarkdown.Api.Jobs;

public sealed record Job
{
    public required string Id { get; init; }

    public JobStatus Status { get; init; }

    public byte[]? ResultBytes { get; init; }

    public string? Error { get; init; }
}
