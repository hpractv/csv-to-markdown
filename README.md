# CsvToMarkdown

Converts a CSV file to a GFM pipe-table Markdown file.

The first row of the CSV becomes the table header. Every subsequent row becomes a table body row. A footer line reporting the total number of data rows is appended at the end of the output file.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Build

```bash
dotnet build
```

## Run the CLI

```bash
dotnet run --project src/CsvToMarkdown.Cli -- <path/to/input.csv>
```

By default the output `.md` file is written alongside the input file, using the same basename with a `.md` extension:

```bash
dotnet run --project src/CsvToMarkdown.Cli -- data/employees.csv
# Writes: data/employees.md
```

You can supply an explicit output path as a second argument:

```bash
dotnet run --project src/CsvToMarkdown.Cli -- data/employees.csv out/employees.md
# Writes: out/employees.md
```

### Exit codes

| Code | Meaning |
| --- | --- |
| 0 | Conversion succeeded |
| 1 | Missing argument, file not found, or conversion error |

## Run tests

```bash
dotnet test
```

All tests should pass. After each run, inspectable `.md` output files are written to `artifacts/test-output/` in the repo root.

## Test artifacts

Every test run writes generated `.md` files to `artifacts/test-output/`. You can open these files after `dotnet test` to inspect the actual converter output:

| Artifact file | What it shows |
| --- | --- |
| `renderer-minimal-table.md` | Minimal two-column table from MarkdownRenderer unit test |
| `renderer-single-data-row.md` | Single data row rendering |
| `renderer-multi-row.md` | Multi-row table |
| `renderer-empty-field.md` | Table with an empty cell |
| `renderer-pipe-escape.md` | Cell value containing a pipe character |
| `integration-default-output-path.md` | End-to-end conversion using default output path |
| `integration-explicit-output-path.md` | End-to-end conversion using an explicit output path |
| `integration-quoted-comma-field.md` | Quoted comma inside a field rendered as one cell |
| `integration-missing-input-file.md` | Expected error message for a missing input file |
| `large_file_output.md` | Output from the 10,000-row streaming test |

The tests use `Contains` assertions rather than full-string golden comparisons, so the artifact files serve as human-readable output for manual review rather than as automated reference files.

## Refreshing artifacts after intentional output changes

If you change the Markdown output format (for example, the footer wording or column separator style), re-run `dotnet test`. The new `.md` files will overwrite the existing ones in `artifacts/test-output/`.

If any test assertions fail because of the intentional change, update the affected `Assert.Contains` calls in the relevant test files:

- `tests/CsvToMarkdown.Tests/MarkdownRendererTests.cs` -- unit tests for MarkdownRenderer
- `tests/CsvToMarkdown.Tests/CsvConverterIntegrationTests.cs` -- end-to-end integration tests
- `tests/CsvToMarkdown.Tests/LargeFileStreamingTests.cs` -- large-file streaming test

Run `dotnet test` again after updating the assertions to confirm everything passes.
