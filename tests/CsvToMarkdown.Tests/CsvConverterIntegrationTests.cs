using System.IO;

namespace CsvToMarkdown.Tests;

public class CsvConverterIntegrationTests
{
    private static readonly string ArtifactDir = FindArtifactDir();

    private static string FindArtifactDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !dir.GetFiles("*.slnx").Any())
            dir = dir.Parent;
        var root = dir?.FullName ?? AppContext.BaseDirectory;
        var artifactDir = Path.Combine(root, "artifacts", "test-output");
        Directory.CreateDirectory(artifactDir);
        return artifactDir;
    }

    private static string WriteTempCsv(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".csv");
        File.WriteAllText(path, content, System.Text.Encoding.UTF8);
        return path;
    }

    private static string WriteTempCsvWithName(string fileName, string content, out string tempDir)
    {
        tempDir = Path.Combine(Path.GetTempPath(), "csv-to-markdown-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var path = Path.Combine(tempDir, fileName);
        File.WriteAllText(path, content, System.Text.Encoding.UTF8);
        return path;
    }

    private static void CopyToArtifacts(string sourcePath, string testName)
    {
        var dest = Path.Combine(ArtifactDir, $"integration-{testName}.md");
        File.Copy(sourcePath, dest, overwrite: true);
    }

    [Fact]
    public void Convert_DefaultOutputPath_WritesMarkdownAlongsideCsv()
    {
        var csvPath = WriteTempCsv("Name,Score\nAlice,95\nBob,87");
        var expectedMdPath = Path.ChangeExtension(csvPath, ".md");
        try
        {
            CsvConverter.Convert(csvPath);

            Assert.True(File.Exists(expectedMdPath));
            var content = File.ReadAllText(expectedMdPath);
            Assert.Contains("| Name | Score |", content);
            Assert.Contains("| --- | --- |", content);
            Assert.Contains("| Alice | 95 |", content);
            Assert.Contains("| Bob | 87 |", content);
            Assert.Contains("2 data rows", content);
            CopyToArtifacts(expectedMdPath, "default-output-path");
        }
        finally
        {
            File.Delete(csvPath);
            if (File.Exists(expectedMdPath)) File.Delete(expectedMdPath);
        }
    }

    [Fact]
    public void Convert_ExplicitOutputPath_WritesMarkdownAtSpecifiedPath()
    {
        var csvPath = WriteTempCsv("City,Country\nParis,France");
        var explicitOutput = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".md");
        try
        {
            CsvConverter.Convert(csvPath, explicitOutput);

            Assert.True(File.Exists(explicitOutput));
            var content = File.ReadAllText(explicitOutput);
            Assert.Contains("| City | Country |", content);
            Assert.Contains("| Paris | France |", content);
            Assert.Contains("1 data row", content);
            CopyToArtifacts(explicitOutput, "explicit-output-path");
        }
        finally
        {
            File.Delete(csvPath);
            if (File.Exists(explicitOutput)) File.Delete(explicitOutput);
        }
    }

    [Fact]
    public void Convert_MissingInputFile_ThrowsFileNotFoundException()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), "does-not-exist-xyzzy.csv");
        var ex = Assert.Throws<FileNotFoundException>(() => CsvConverter.Convert(missingPath));
        Assert.Contains(missingPath, ex.Message);

        // Write an artifact so humans can inspect the expected failure after test runs.
        var artifactPath = Path.Combine(ArtifactDir, "integration-missing-input-file.md");
        File.WriteAllText(artifactPath, $"Expected FileNotFoundException for path: {missingPath}\nMessage: {ex.Message}", System.Text.Encoding.UTF8);
    }

    [Fact]
    public void Convert_QuotedCommaInField_RenderedAsOneCell()
    {
        var csvPath = WriteTempCsv("Product,Description\nWidget,\"Small, round\"");
        var explicitOutput = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".md");
        try
        {
            CsvConverter.Convert(csvPath, explicitOutput);

            var content = File.ReadAllText(explicitOutput);
            Assert.Contains("| Small, round |", content);
            Assert.DoesNotContain("| Small |", content);
            CopyToArtifacts(explicitOutput, "quoted-comma-field");
        }
        finally
        {
            File.Delete(csvPath);
            if (File.Exists(explicitOutput)) File.Delete(explicitOutput);
        }
    }

    [Theory]
    [InlineData("FilesInACSV.csv", "# Files In A CSV")]
    [InlineData("SomethingToBehold.csv", "# Something To Behold")]
    [InlineData("employees-q1_summary.csv", "# employees q1 summary")]
    public void Convert_FileNameStem_WritesMarkdownH1TitleBeforeTable(string csvFileName, string expectedTitleLine)
    {
        string tempDir;
        var csvPath = WriteTempCsvWithName(csvFileName, "ColA,ColB\n1,2", out tempDir);
        var outputPath = Path.ChangeExtension(csvPath, ".md");

        try
        {
            CsvConverter.Convert(csvPath);

            var content = File.ReadAllText(outputPath);
            var lines = content.Split(Environment.NewLine, StringSplitOptions.None);

            Assert.Equal(expectedTitleLine, lines[0]);
            Assert.Contains("| ColA | ColB |", content);
            Assert.Contains("1 data row", content);

            CopyToArtifacts(outputPath, $"title-{Path.GetFileNameWithoutExtension(csvFileName)}");
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }
}
