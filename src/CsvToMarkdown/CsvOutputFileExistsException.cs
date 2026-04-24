namespace CsvToMarkdown;

/// <summary>
/// Thrown when the resolved Markdown output path already exists and overwrite was not allowed.
/// </summary>
public sealed class CsvOutputFileExistsException : IOException
{
    public string OutputPath { get; }

    public CsvOutputFileExistsException(string outputPath)
        : base($"Output file already exists: {outputPath}")
    {
        OutputPath = outputPath;
    }
}
