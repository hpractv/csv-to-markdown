namespace CsvToMarkdown;

/// <summary>
/// Options for <see cref="CsvConverter.Convert"/>.
/// </summary>
public sealed class CsvConvertOptions
{
    /// <summary>
    /// When false (default), conversion fails if the resolved output path already exists.
    /// </summary>
    public bool OverwriteExisting { get; init; }
}
