using System.IO;

namespace CsvToMarkdown.Tests;

public class CsvParserTests
{
    private static string WriteTempCsv(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content, System.Text.Encoding.UTF8);
        return path;
    }

    [Fact]
    public void ParseRows_MinimalTwoColumn_YieldsTwoRows()
    {
        var path = WriteTempCsv("a,b\n1,2");
        try
        {
            var rows = CsvParser.ParseRows(path).ToList();
            Assert.Equal(2, rows.Count);
            Assert.Equal(new[] { "a", "b" }, rows[0]);
            Assert.Equal(new[] { "1", "2" }, rows[1]);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ParseRows_QuotedComma_ParsedAsOneField()
    {
        var path = WriteTempCsv("a,\"b,c\"\n1,2");
        try
        {
            var rows = CsvParser.ParseRows(path).ToList();
            Assert.Equal(2, rows.Count);
            Assert.Equal(new[] { "a", "b,c" }, rows[0]);
            Assert.Equal(new[] { "1", "2" }, rows[1]);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ParseRows_EmptyMiddleField_YieldsEmptyString()
    {
        var path = WriteTempCsv("a,,b\n1,,2");
        try
        {
            var rows = CsvParser.ParseRows(path).ToList();
            Assert.Equal(2, rows.Count);
            Assert.Equal(new[] { "a", "", "b" }, rows[0]);
            Assert.Equal(new[] { "1", "", "2" }, rows[1]);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ParseRows_EscapedDoubleQuote_IncludedInField()
    {
        var path = WriteTempCsv("a,\"say \"\"hello\"\"\"\n1,2");
        try
        {
            var rows = CsvParser.ParseRows(path).ToList();
            Assert.Equal(new[] { "a", "say \"hello\"" }, rows[0]);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ParseRows_CrLfLineEndings_ParsedCorrectly()
    {
        var path = WriteTempCsv("a,b\r\n1,2\r\n3,4");
        try
        {
            var rows = CsvParser.ParseRows(path).ToList();
            Assert.Equal(3, rows.Count);
            Assert.Equal(new[] { "3", "4" }, rows[2]);
        }
        finally { File.Delete(path); }
    }
}
