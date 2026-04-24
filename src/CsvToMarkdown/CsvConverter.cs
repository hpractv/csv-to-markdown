using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CsvToMarkdown;

public static class CsvConverter
{
    private static readonly Regex LowerOrDigitToUpperBoundary = new(@"(?<=[a-z0-9])(?=[A-Z])", RegexOptions.Compiled);
    private static readonly Regex AcronymToWordBoundary = new(@"(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.Compiled);
    private static readonly Regex LeadingAcronymArticleBoundary = new(@"\bA([A-Z]{2,})\b", RegexOptions.Compiled);
    private static readonly Regex CollapsedWhitespace = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Converts a CSV file to a GFM Markdown pipe table and writes it to disk.
    /// If <paramref name="outputPath"/> is null or empty, the output file is placed
    /// in the same directory as the input with the extension replaced by .md.
    /// </summary>
    public static string Convert(
        string inputPath,
        string? outputPath = null,
        string? sourceFileName = null,
        CsvConvertOptions? options = null)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"CSV file not found: {inputPath}", inputPath);

        var resolvedOutput = ResolveOutputPath(inputPath, outputPath);
        var overwrite = options?.OverwriteExisting ?? false;
        if (File.Exists(resolvedOutput) && !overwrite)
            throw new CsvOutputFileExistsException(resolvedOutput);

        var title = BuildTitle(sourceFileName ?? inputPath);

        var rows = CsvParser.ParseRows(inputPath);
        using var writer = new StreamWriter(resolvedOutput, append: false, Encoding.UTF8);
        writer.WriteLine($"# {title}");
        writer.WriteLine();
        MarkdownRenderer.Render(rows, writer);
        return resolvedOutput;
    }

    private static string BuildTitle(string sourcePathOrName)
    {
        var stem = Path.GetFileNameWithoutExtension(sourcePathOrName);
        var separatorsNormalized = stem.Replace('_', ' ').Replace('-', ' ');
        var withAcronymBoundaries = AcronymToWordBoundary.Replace(separatorsNormalized, " ");
        var withWordBoundaries = LowerOrDigitToUpperBoundary.Replace(withAcronymBoundaries, " ");
        var withArticleBoundary = LeadingAcronymArticleBoundary.Replace(withWordBoundaries, "A $1");
        var normalizedWhitespace = CollapsedWhitespace.Replace(withArticleBoundary, " ").Trim();
        return string.IsNullOrWhiteSpace(normalizedWhitespace) ? "Untitled" : normalizedWhitespace;
    }

    /// <summary>
    /// Resolves the Markdown output path for an input CSV and optional explicit output path.
    /// </summary>
    public static string ResolveOutputPath(string inputPath, string? outputPath)
    {
        if (!string.IsNullOrEmpty(outputPath))
            return outputPath;

        var dir = Path.GetDirectoryName(Path.GetFullPath(inputPath)) ?? ".";
        var baseName = Path.GetFileNameWithoutExtension(inputPath);
        return Path.Combine(dir, baseName + ".md");
    }
}
