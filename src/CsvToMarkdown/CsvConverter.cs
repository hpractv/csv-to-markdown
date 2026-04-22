using System.IO;
using System.Text;

namespace CsvToMarkdown;

public static class CsvConverter
{
    /// <summary>
    /// Converts a CSV file to a GFM Markdown pipe table and writes it to disk.
    /// If <paramref name="outputPath"/> is null or empty, the output file is placed
    /// in the same directory as the input with the extension replaced by .md.
    /// </summary>
    public static string Convert(string inputPath, string? outputPath = null)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"CSV file not found: {inputPath}", inputPath);

        var resolvedOutput = ResolveOutputPath(inputPath, outputPath);

        var rows = CsvParser.ParseRows(inputPath);
        using var writer = new StreamWriter(resolvedOutput, append: false, Encoding.UTF8);
        MarkdownRenderer.Render(rows, writer);
        return resolvedOutput;
    }

    private static string ResolveOutputPath(string inputPath, string? outputPath)
    {
        if (!string.IsNullOrEmpty(outputPath))
            return outputPath;

        var dir = Path.GetDirectoryName(Path.GetFullPath(inputPath)) ?? ".";
        var baseName = Path.GetFileNameWithoutExtension(inputPath);
        return Path.Combine(dir, baseName + ".md");
    }
}
