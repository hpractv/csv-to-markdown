using System.IO;

namespace CsvToMarkdown;

internal static class MarkdownRenderer
{
    /// <summary>
    /// Renders a streaming sequence of CSV rows as a GFM-style pipe table followed by
    /// a footer line reporting the number of data rows (header row not counted).
    /// The first element of <paramref name="rows"/> is treated as the header row.
    /// Pipe characters inside cell values are escaped as \| to preserve GFM validity.
    /// </summary>
    public static void Render(IEnumerable<string[]> rows, TextWriter writer)
    {
        bool headerWritten = false;
        int dataRowCount = 0;
        int columnCount = 0;

        foreach (var row in rows)
        {
            if (!headerWritten)
            {
                columnCount = row.Length;
                WriteRow(row, writer);
                WriteSeparator(columnCount, writer);
                headerWritten = true;
            }
            else
            {
                WriteRow(row, writer);
                dataRowCount++;
            }
        }

        writer.WriteLine();
        string rowWord = dataRowCount == 1 ? "data row" : "data rows";
        writer.WriteLine($"{dataRowCount} {rowWord}");
    }

    private static void WriteRow(string[] fields, TextWriter writer)
    {
        writer.Write('|');
        foreach (var field in fields)
        {
            writer.Write(' ');
            writer.Write(EscapePipes(field));
            writer.Write(" |");
        }
        writer.WriteLine();
    }

    private static void WriteSeparator(int columnCount, TextWriter writer)
    {
        writer.Write('|');
        for (int i = 0; i < columnCount; i++)
            writer.Write(" --- |");
        writer.WriteLine();
    }

    private static string EscapePipes(string value) =>
        value.Replace("|", @"\|");
}
