# CsvToMarkdown

Converts a CSV file to a GFM pipe-table Markdown file.

The first row of the CSV becomes the table header. Every subsequent row becomes a table body row. A footer line reporting the total number of data rows is appended at the end of the output file.

## Prerequisites

- .NET SDK 10.0 (TargetFramework: net10.0)
- `jq` (optional, used in the curl examples to parse JSON job ids/status)

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

## Run the API

Start the API:

```bash
dotnet run --project src/CsvToMarkdown.Api
```

With the default Development launch profile, local URLs are:

- API base URL: `http://localhost:5085`
- Swagger UI: `http://localhost:5085/swagger`
- OpenAPI JSON: `http://localhost:5085/swagger/v1/swagger.json`

### API contract

- `POST /jobs`: Upload a CSV as `multipart/form-data` in field `file`; returns `202 Accepted` with `id` and initial `status`.
- `GET /jobs/{id}`: Poll job status (`Queued`, `Running`, `Succeeded`, `Failed`) and error summary when present.
- `GET /jobs/{id}/result`: Download Markdown when complete (`text/markdown`).

### curl examples

Upload:

```bash
UPLOAD_RESPONSE=$(curl -sS -X POST "http://localhost:5085/jobs" \
  -F "file=@data/employees.csv;type=text/csv")
echo "$UPLOAD_RESPONSE"
JOB_ID=$(echo "$UPLOAD_RESPONSE" | jq -r '.id')
```

Poll status until terminal:

```bash
while true; do
  STATUS_RESPONSE=$(curl -sS "http://localhost:5085/jobs/$JOB_ID")
  STATUS=$(echo "$STATUS_RESPONSE" | jq -r '.status')
  echo "status=$STATUS"
  if [ "$STATUS" = "Succeeded" ] || [ "$STATUS" = "Failed" ]; then
    break
  fi
  sleep 1
done
```

Download result when succeeded:

```bash
curl -sS -f "http://localhost:5085/jobs/$JOB_ID/result" -o "result-$JOB_ID.md"
```

## Run tests

```bash
dotnet test
```

Run API flow integration tests in the dedicated API test project:

```bash
dotnet test tests/CsvToMarkdown.Api.Tests/CsvToMarkdown.Api.Tests.csproj
```

Running the test suite writes inspectable outputs to `artifacts/test-output/`.

## Test artifacts

Every test run writes generated `.md` and `.csv` artifacts to `artifacts/test-output/`. You can open these files after `dotnet test` to inspect converter and API flow outputs:

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
| `large_file_output.md` | Output from the large-file streaming test (50,000 data rows) |
| `api-small-edge-case-input.csv` | Upload source CSV for API small edge-case integration test |
| `api-small-edge-case-output.md` | Downloaded Markdown from API small edge-case integration test |
| `api-large-10000-input.csv` | Upload source CSV for API 10,000+ row integration test |
| `api-large-10000-output.md` | Downloaded Markdown from API 10,000+ row integration test |

The tests use `Contains` assertions rather than full-string golden comparisons, so the artifact files serve as human-readable output for manual review rather than as automated reference files.

## Refreshing artifacts after intentional output changes

If you change the Markdown output format (for example, the footer wording or column separator style), re-run `dotnet test`. The new `.md` files will overwrite the existing ones in `artifacts/test-output/`.

If any test assertions fail because of the intentional change, update the affected `Assert.Contains` calls in the relevant test files:

- `tests/CsvToMarkdown.Tests/MarkdownRendererTests.cs` -- unit tests for MarkdownRenderer
- `tests/CsvToMarkdown.Tests/CsvConverterIntegrationTests.cs` -- end-to-end integration tests
- `tests/CsvToMarkdown.Tests/LargeFileStreamingTests.cs` -- large-file streaming test
- `tests/CsvToMarkdown.Tests/ApiIntegrationTests.cs` -- API integration tests
- `tests/CsvToMarkdown.Api.Tests/ApiJobFlowIntegrationTests.cs` -- API upload/poll/download artifact tests

Run `dotnet test` again after updating the assertions to confirm everything passes.
