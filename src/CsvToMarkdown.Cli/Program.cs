using CsvToMarkdown;

const int ExitUsage = 1;
const int ExitInputNotFound = 2;
const int ExitConversionError = 3;
const int ExitOutputExistsNonInteractive = 4;
const int ExitCancelled = 5;

var positional = new List<string>();
var overwriteFromFlag = false;
foreach (var arg in args)
{
    if (arg is "--overwrite" or "-o")
        overwriteFromFlag = true;
    else
        positional.Add(arg);
}

if (positional.Count is < 1 or > 2)
{
    Console.Error.WriteLine("Usage: CsvToMarkdown.Cli [--overwrite|-o] <input.csv> [output.md]");
    return ExitUsage;
}

var inputPath = positional[0];
var outputPath = positional.Count == 2 ? positional[1] : null;

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Error: input file not found: {inputPath}");
    return ExitInputNotFound;
}

var resolvedOutput = CsvConverter.ResolveOutputPath(inputPath, outputPath);
var allowOverwrite = overwriteFromFlag;

if (File.Exists(resolvedOutput) && !allowOverwrite)
{
    if (Console.IsInputRedirected)
    {
        Console.Error.WriteLine($"Error: output file already exists: {resolvedOutput}");
        Console.Error.WriteLine("Use --overwrite or -o to replace it without prompting.");
        return ExitOutputExistsNonInteractive;
    }

    Console.Write($"Overwrite {resolvedOutput}? (y/N): ");
    var line = Console.ReadLine();
    allowOverwrite = line is not null &&
                     (line.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                      line.Equals("yes", StringComparison.OrdinalIgnoreCase));

    if (!allowOverwrite)
    {
        Console.Error.WriteLine("Cancelled.");
        return ExitCancelled;
    }
}

try
{
    var options = new CsvConvertOptions { OverwriteExisting = allowOverwrite };
    var resolved = CsvConverter.Convert(inputPath, outputPath, sourceFileName: null, options);
    Console.WriteLine($"Converted to: {resolved}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return ExitConversionError;
}
