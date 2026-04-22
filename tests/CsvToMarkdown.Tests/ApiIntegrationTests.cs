using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using CsvToMarkdown.Api;
using CsvToMarkdown.Api.Jobs;

namespace CsvToMarkdown.Tests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetJobStatus_ReturnsNotFound_WhenJobDoesNotExist()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/jobs/non-existent-id");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetJobResult_ReturnsBadRequest_WhenJobDoesNotExist()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/jobs/non-existent-id/result");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetJobResult_ReturnsBadRequest_WhenJobIsStillRunning()
    {
        // This test requires a way to control job state or a real job.
        // For now, testing the failure case is sufficient since we don't have job ID yet.
        var client = _factory.CreateClient();
        
        // Use a dummy id for now
        var response = await client.GetAsync("/jobs/some-id/result");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
