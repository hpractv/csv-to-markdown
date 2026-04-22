using System.IO;

namespace CsvToMarkdown.Tests;

public class MarkdownRendererTests
{
    // Resolve the repo root by walking up from the test binary output directory.
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

    private static string Render(IEnumerable<string[]> rows)
    {
        using var sw = new StringWriter();
        MarkdownRenderer.Render(rows, sw);
        return sw.ToString();
    }

    private static void WriteArtifact(string testName, string content)
    {
        var path = Path.Combine(ArtifactDir, $"renderer-{testName}.md");
        File.WriteAllText(path, content, System.Text.Encoding.UTF8);
    }

    [Fact]
    public void MinimalTable_HeaderPlusTwoDataRows_ProducesValidGfmTable()
    {
        var rows = new[]
        {
            new[] { "Name", "Age" },
            new[] { "Alice", "30" },
            new[] { "Bob", "25" },
        };

        var output = Render(rows);
        WriteArtifact("minimal-table", output);

        Assert.Contains("| Name | Age |", output);
        Assert.Contains("| --- | --- |", output);
        Assert.Contains("| Alice | 30 |", output);
        Assert.Contains("| Bob | 25 |", output);
        Assert.Contains("2 data rows", output);
    }

    [Fact]
    public void SingleDataRow_FooterUsesSingular()
    {
        var rows = new[]
        {
            new[] { "Col1", "Col2" },
            new[] { "val1", "val2" },
        };

        var output = Render(rows);
        WriteArtifact("single-data-row", output);

        Assert.Contains("1 data row", output);
        Assert.DoesNotContain("1 data rows", output);
    }

    [Fact]
    public void EmptyField_RendersAsBlankCell()
    {
        var rows = new[]
        {
            new[] { "A", "B", "C" },
            new[] { "1", "", "3" },
        };

        var output = Render(rows);
        WriteArtifact("empty-field", output);

        // The empty field should produce a cell with only whitespace between pipes
        Assert.Contains("|  |", output);
        Assert.Contains("1 data row", output);
    }

    [Fact]
    public void PipeInCellValue_IsEscaped()
    {
        var rows = new[]
        {
            new[] { "Header" },
            new[] { "val|ue" },
        };

        var output = Render(rows);
        WriteArtifact("pipe-escape", output);

        Assert.Contains(@"val\|ue", output);
    }

    [Fact]
    public void MultiRow_FooterCountMatchesDataRows()
    {
        var header = new[] { "X", "Y" };
        var dataRows = Enumerable.Range(1, 10).Select(i => new[] { $"r{i}a", $"r{i}b" });
        var rows = new[] { header }.Concat(dataRows);

        var output = Render(rows);
        WriteArtifact("multi-row", output);

        Assert.Contains("10 data rows", output);
    }
}
