using CsvToMarkdown;

if (args.Length < 1 || args.Length > 2)
{
    Console.Error.WriteLine("Usage: CsvToMarkdown.Cli <input.csv> [output.md]");
    return 1;
}

var inputPath = args[0];
var outputPath = args.Length == 2 ? args[1] : null;

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Error: input file not found: {inputPath}");
    return 2;
}

try
{
    var resolvedOutput = CsvConverter.Convert(inputPath, outputPath);
    Console.WriteLine($"Converted to: {resolvedOutput}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 3;
}
