using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using CsvToMarkdown.Api;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CsvToMarkdown.Api.Tests;

public class ApiJobFlowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const int PollIntervalMs = 25;
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(10);
    private static readonly string ArtifactDir = FindArtifactDir();
    private readonly HttpClient _client;

    public ApiJobFlowIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UploadPollDownload_SmallEdgeCaseCsv_SucceedsWithExpectedMarkdown()
    {
        var csvFileName = "ApiSmallEdgeCaseInput.csv";
        var csvContent = string.Join(
            Environment.NewLine,
            [
                "Name,Notes,Score",
                "\"Alice\",\"Contains, comma\",95",
                "\"Bob\",\"\",",
                "\"Cara\",\"Uses | pipe\",88"
            ]);

        var (jobId, initialStatus) = await UploadCsvAsync(csvFileName, csvContent);
        Assert.Equal("Queued", initialStatus);

        var observedStatuses = await PollUntilTerminalStateAsync(jobId);
        Assert.Equal("Succeeded", observedStatuses[^1]);

        var markdown = await DownloadResultAsync(jobId);
        Assert.StartsWith("# Api Small Edge Case Input", markdown, StringComparison.Ordinal);
        Assert.Contains("| Name | Notes | Score |", markdown);
        Assert.Contains("| Alice | Contains, comma | 95 |", markdown);
        Assert.Contains(@"| Cara | Uses \| pipe | 88 |", markdown);
        Assert.Contains("3 data rows", markdown);

        WriteArtifact(csvFileName, csvContent);
        WriteArtifact("api-small-edge-case-output.md", markdown);
    }

    [Fact]
    public async Task UploadPollDownload_LargeCsvWithTenThousandRows_SucceedsAndFooterMatches()
    {
        const int dataRows = 10_000;
        var csvFileName = "FilesInACSVLarge_10000.csv";
        var csvContent = BuildLargeCsv(dataRows);

        var (jobId, initialStatus) = await UploadCsvAsync(csvFileName, csvContent);
        Assert.Equal("Queued", initialStatus);

        var observedStatuses = await PollUntilTerminalStateAsync(jobId);
        Assert.Equal("Succeeded", observedStatuses[^1]);

        var markdown = await DownloadResultAsync(jobId);
        Assert.StartsWith("# Files In A CSV Large 10000", markdown, StringComparison.Ordinal);
        Assert.Contains("| Id | Name | Score |", markdown);
        Assert.Contains("| 1 | User1 | 1 |", markdown);
        Assert.Contains($"| {dataRows} | User{dataRows} | 0 |", markdown);
        Assert.Contains($"{dataRows} data rows", markdown);

        var lines = markdown.Split(Environment.NewLine, StringSplitOptions.None);
        var separatorIndex = Array.FindIndex(lines, line => line.StartsWith("|", StringComparison.Ordinal) && line.Contains("---", StringComparison.Ordinal));
        Assert.True(separatorIndex >= 0, "Expected markdown separator row was not found.");

        var bodyRowCount = lines
            .Skip(separatorIndex + 1)
            .Count(line => line.StartsWith("|", StringComparison.Ordinal));
        Assert.Equal(dataRows, bodyRowCount);

        WriteArtifact(csvFileName, csvContent);
        WriteArtifact("api-large-10000-output.md", markdown);
    }

    private async Task<(string JobId, string Status)> UploadCsvAsync(string fileName, string csvContent)
    {
        using var multipart = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        multipart.Add(fileContent, "file", fileName);

        var response = await _client.PostAsync("/jobs", multipart);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JobSubmissionResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Id));
        Assert.False(string.IsNullOrWhiteSpace(payload.Status));

        return (payload.Id, payload.Status);
    }

    private async Task<List<string>> PollUntilTerminalStateAsync(string jobId)
    {
        var observedStatuses = new List<string>();
        var terminalStates = new HashSet<string>(StringComparer.Ordinal) { "Succeeded", "Failed" };
        var timeoutAt = DateTimeOffset.UtcNow + PollTimeout;

        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            var status = await GetJobStatusAsync(jobId);
            observedStatuses.Add(status.Status);

            if (terminalStates.Contains(status.Status))
            {
                Assert.NotEqual("Failed", status.Status);
                Assert.True(string.IsNullOrWhiteSpace(status.Error));
                return observedStatuses;
            }

            await Task.Delay(PollIntervalMs);
        }

        throw new TimeoutException($"Timed out waiting for job '{jobId}' to reach a terminal status. Last status: {observedStatuses.LastOrDefault()}");
    }

    private async Task<JobStatusResponse> GetJobStatusAsync(string jobId)
    {
        var response = await _client.GetAsync($"/jobs/{jobId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JobStatusResponse>();
        Assert.NotNull(payload);
        Assert.Equal(jobId, payload!.Id);
        Assert.False(string.IsNullOrWhiteSpace(payload.Status));

        return payload;
    }

    private async Task<string> DownloadResultAsync(string jobId)
    {
        var response = await _client.GetAsync($"/jobs/{jobId}/result");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/markdown", response.Content.Headers.ContentType?.MediaType);

        var markdown = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(markdown));
        return markdown;
    }

    private static string BuildLargeCsv(int dataRows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Id,Name,Score");
        for (int i = 1; i <= dataRows; i++)
        {
            builder.Append(i);
            builder.Append(",User");
            builder.Append(i);
            builder.Append(',');
            builder.Append(i % 100);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string FindArtifactDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles("*.slnx").Any())
        {
            dir = dir.Parent;
        }

        var root = dir?.FullName ?? AppContext.BaseDirectory;
        var artifactPath = Path.Combine(root, "artifacts", "test-output");
        Directory.CreateDirectory(artifactPath);
        return artifactPath;
    }

    private static void WriteArtifact(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(ArtifactDir, fileName), content, Encoding.UTF8);
    }

    private sealed record JobSubmissionResponse(string Id, string Status);
    private sealed record JobStatusResponse(string Id, string Status, string? Error);
}
