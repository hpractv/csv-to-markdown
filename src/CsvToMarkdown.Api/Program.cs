using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CsvToMarkdown.Api.Jobs;
using CsvToMarkdown;

var builder = WebApplication.CreateBuilder(args);

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IJobStore, InMemoryJobStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapPost("/jobs", async (HttpRequest request, IJobStore jobStore, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
{
    if (!request.HasFormContentType ||
        string.IsNullOrWhiteSpace(request.ContentType) ||
        !request.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "Request must be multipart/form-data." });
    }

    var form = await request.ReadFormAsync(cancellationToken);
    var file = form.Files.GetFile("file");

    if (file is null)
    {
        return Results.BadRequest(new { error = "A CSV file is required in form field 'file'." });
    }

    if (file.Length == 0)
    {
        return Results.BadRequest(new { error = "Uploaded CSV file cannot be empty." });
    }

    if (!IsCsvUpload(file))
    {
        return Results.BadRequest(new { error = "Uploaded file must be a CSV." });
    }

    var tempInputPath = Path.Combine(Path.GetTempPath(), $"csv-to-markdown-{Guid.NewGuid():N}.csv");
    await using (var inputFile = File.Create(tempInputPath))
    await using (var uploadStream = file.OpenReadStream())
    {
        await uploadStream.CopyToAsync(inputFile, cancellationToken);
    }

    var job = jobStore.Create();
    var logger = loggerFactory.CreateLogger("CsvToMarkdown.Api.JobProcessor");

    _ = Task.Run(
        () => ProcessJobAsync(jobStore, logger, job.Id, tempInputPath, file.FileName),
        CancellationToken.None);

    return Results.Accepted($"/jobs/{job.Id}", new
    {
        id = job.Id,
        status = job.Status.ToString()
    });
});

app.MapGet("/jobs/{id}", (string id, IJobStore jobStore) =>
{
    if (!jobStore.TryGet(id, out var job) || job is null)
    {
        return Results.NotFound(new { error = $"Job '{id}' not found." });
    }

    return Results.Ok(new
    {
        id = job.Id,
        status = job.Status.ToString(),
        error = job.Error
    });
});

app.MapGet("/jobs/{id}/result", (string id, IJobStore jobStore) =>
{
    if (!jobStore.TryGet(id, out var job) || job is null)
    {
        return Results.NotFound(new { error = $"Job '{id}' not found." });
    }

    if (job.Status == JobStatus.Queued || job.Status == JobStatus.Running)
    {
        return Results.BadRequest(new { error = $"Job '{id}' is still in progress." });
    }

    if (job.Status == JobStatus.Failed)
    {
        return Results.BadRequest(new { error = $"Job '{id}' failed.", details = job.Error });
    }

    if (job.ResultBytes is null)
    {
        return Results.NotFound(new { error = "Result not found." });
    }

    return Results.Bytes(job.ResultBytes, "text/markdown", $"result-{id}.md");
});

app.Run();

static bool IsCsvUpload(IFormFile file)
{
    if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    if (string.IsNullOrWhiteSpace(file.ContentType))
    {
        return false;
    }

    return file.ContentType.Equals("text/csv", StringComparison.OrdinalIgnoreCase) ||
           file.ContentType.Equals("application/csv", StringComparison.OrdinalIgnoreCase) ||
           file.ContentType.Equals("application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase);
}

static async Task ProcessJobAsync(IJobStore jobStore, ILogger logger, string jobId, string inputPath, string sourceFileName)
{
    string? outputPath = null;

    try
    {
        if (!jobStore.TrySetRunning(jobId))
        {
            jobStore.TrySetFailed(jobId, "Job state update failed before processing started.");
            return;
        }

        outputPath = Path.Combine(Path.GetTempPath(), $"csv-to-markdown-{Guid.NewGuid():N}.md");
        CsvConverter.Convert(inputPath, outputPath, sourceFileName, new CsvConvertOptions { OverwriteExisting = true });
        var resultBytes = await File.ReadAllBytesAsync(outputPath);

        if (!jobStore.TrySetSucceeded(jobId, resultBytes))
        {
            jobStore.TrySetFailed(jobId, "Job state update failed after conversion.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "CSV conversion failed for job {JobId}", jobId);
        jobStore.TrySetFailed(jobId, "CSV conversion failed. Verify the uploaded file and try again.");
    }
    finally
    {
        TryDeleteTempFile(inputPath, logger, jobId);
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            TryDeleteTempFile(outputPath, logger, jobId);
        }
    }
}

static void TryDeleteTempFile(string path, ILogger logger, string jobId)
{
    try
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to delete temp file for job {JobId}", jobId);
    }
}

public partial class Program { }
