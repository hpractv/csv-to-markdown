using System.Text;

namespace CsvToMarkdown;

internal static class CsvParser
{
    /// <summary>
    /// Lazily streams rows from a CSV file. Each row is a string array where each
    /// element is one field value. Quoted fields may contain commas and embedded
    /// newlines. Two consecutive double-quotes inside a quoted field represent a
    /// literal double-quote character. Rows with fewer columns than the header are
    /// padded with empty strings; rows with MORE columns than the header throw an
    /// InvalidDataException so callers get a clear signal about malformed input.
    /// </summary>
    public static IEnumerable<string[]> ParseRows(string filePath)
    {
        using var reader = new StreamReader(filePath, Encoding.UTF8);

        int? headerColumnCount = null;

        // We read character-by-character via a state machine to handle quoted
        // fields that span multiple lines. But we still call ReadLine as a hint
        // for the common case and fall back to per-character reads for multi-line
        // quoted fields. To keep it simple and correct we use full char-level parsing.

        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;
        bool afterQuote = false; // we just saw a closing quote -- next char decides

        int ch;
        while ((ch = reader.Read()) != -1)
        {
            char c = (char)ch;

            if (inQuotes)
            {
                if (afterQuote)
                {
                    afterQuote = false;
                    if (c == '"')
                    {
                        // escaped double-quote inside quoted field
                        current.Append('"');
                    }
                    else if (c == ',')
                    {
                        fields.Add(current.ToString());
                        current.Clear();
                        inQuotes = false;
                    }
                    else if (c == '\r')
                    {
                        // swallow CR; LF will end the field/row
                        inQuotes = false;
                        fields.Add(current.ToString());
                        current.Clear();
                        // peek at next char: if \n, consume it
                        int next = reader.Peek();
                        if (next == '\n') reader.Read();
                        yield return FinalizeRow(fields, ref headerColumnCount);
                        fields.Clear();
                    }
                    else if (c == '\n')
                    {
                        inQuotes = false;
                        fields.Add(current.ToString());
                        current.Clear();
                        yield return FinalizeRow(fields, ref headerColumnCount);
                        fields.Clear();
                    }
                    else
                    {
                        // char after closing quote that isn't a recognised delimiter --
                        // treat as continuation (lenient; some exporters add spaces)
                        current.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        afterQuote = true;
                    }
                    else
                    {
                        // CR/LF inside a quoted field are embedded newlines -- keep them
                        current.Append(c);
                    }
                }
            }
            else
            {
                // not in quotes
                if (c == '"' && current.Length == 0)
                {
                    inQuotes = true;
                    afterQuote = false;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else if (c == '\r')
                {
                    int next = reader.Peek();
                    if (next == '\n') reader.Read();
                    fields.Add(current.ToString());
                    current.Clear();
                    if (fields.Count > 0 || current.Length > 0 || IsNonEmpty(fields))
                    {
                        yield return FinalizeRow(fields, ref headerColumnCount);
                    }
                    fields.Clear();
                }
                else if (c == '\n')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                    yield return FinalizeRow(fields, ref headerColumnCount);
                    fields.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        // flush the last row if the file does not end with a newline
        if (fields.Count > 0 || current.Length > 0)
        {
            fields.Add(current.ToString());
            yield return FinalizeRow(fields, ref headerColumnCount);
        }
    }

    private static bool IsNonEmpty(List<string> fields) =>
        fields.Count > 0;

    private static string[] FinalizeRow(List<string> fields, ref int? headerColumnCount)
    {
        if (headerColumnCount is null)
        {
            headerColumnCount = fields.Count;
        }
        else if (fields.Count > headerColumnCount)
        {
            throw new InvalidDataException(
                $"Row has {fields.Count} columns but the header has {headerColumnCount}.");
        }

        // Pad rows that are shorter than the header (e.g. trailing commas omitted)
        while (fields.Count < headerColumnCount)
            fields.Add(string.Empty);

        return fields.ToArray();
    }
}
