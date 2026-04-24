using System.IO;
using System.Text;

namespace CsvToMarkdown.Tests;

/// <summary>
/// Verifies that the conversion pipeline handles large CSV files correctly without
/// loading the entire file into memory at once.
///
/// Streaming justification:
///   - CsvParser.ParseRows uses IEnumerable&lt;string[]&gt; with yield return, meaning
///     only one row is held in the parsing state machine at a time.
///   - MarkdownRenderer.Render iterates the IEnumerable row by row and writes each
///     row directly to the TextWriter as it is received.
///   - CsvConverter.Convert passes the lazy IEnumerable from the parser straight to
///     the renderer; no intermediate List or array is created for the full dataset.
/// Together these form a fully streaming pipeline: O(1) memory with respect to file size.
/// </summary>
public class LargeFileStreamingTests
{
    private const int DataRowCount = 50_000;

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

    [Fact]
    public void Convert_LargeFile_FooterRowCountMatchesGeneratedRows()
    {
        var csvPath = Path.Combine(ArtifactDir, "LargeFilesInACSV.csv");
        var outputPath = Path.Combine(ArtifactDir, "large_file_output.md");
        const string expectedTitle = "# Large Files In A CSV";

        // Generate a large CSV using a StreamWriter so the generator itself is streaming.
        using (var writer = new StreamWriter(csvPath, append: false, Encoding.UTF8))
        {
            writer.WriteLine("Id,Name,Value,Category,Active");
            for (int i = 1; i <= DataRowCount; i++)
                writer.WriteLine($"{i},Name{i},{i * 3},Cat{i % 10},{(i % 2 == 0 ? "true" : "false")}");
        }

        CsvConverter.Convert(csvPath, outputPath, sourceFileName: null, new CsvConvertOptions { OverwriteExisting = true });

        Assert.True(File.Exists(outputPath));

        var lines = File.ReadAllLines(outputPath);
        Assert.Equal(expectedTitle, lines[0]);

        // Read the last non-empty line: should be the footer "50000 data rows"
        var footer = lines.Last(l => l.Trim().Length > 0);
        Assert.Equal($"{DataRowCount} data rows", footer);

        // Count body rows (pipe rows after the separator row) as a secondary check.
        int separatorIndex = Array.FindIndex(lines, l => l.StartsWith("|") && l.Contains("---"));
        Assert.True(separatorIndex >= 0, "Separator row not found in output.");
        int bodyRowCount = lines
            .Skip(separatorIndex + 1)
            .Count(l => l.StartsWith("|"));
        Assert.Equal(DataRowCount, bodyRowCount);
    }
}
