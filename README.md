# CsvToMarkdown

Converts a CSV file to a Markdown document with an H1 title and a GFM pipe-table.

The output starts with an H1 derived from the input CSV filename (without extension): `\_` and `-` become spaces, and spaces are inserted between concatenated capitalized words (for example, `FilesInACSV.csv` becomes `# Files In A CSV`).

After the title, the first row of the CSV becomes the table header. Every subsequent row becomes a table body row. A footer line reporting the total number of data rows is appended at the end of the output file.

## Prerequisites

* .NET SDK 10.0 (TargetFramework: net10.0)
* `jq` (optional, used in the curl examples to parse JSON job ids/status)

## Build

```bash
dotnet build
```

## Run the CLI

```bash
dotnet run --project src/CsvToMarkdown.Cli -- \[--overwrite|-o] <path/to/input.csv> \[output.md]
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

If the resolved output file already exists, the CLI prompts `Overwrite ...? (y/N):` unless standard input is redirected (non-interactive), in which case it exits with an error and tells you to pass `--overwrite` or `-o`. With `--overwrite` / `-o`, the output file is replaced without prompting.

### Exit codes

|Code|Meaning|
|-|-|
|0|Conversion succeeded|
|1|Invalid usage (wrong arguments)|
|2|Input CSV not found|
|3|Conversion failed (including library errors)|
|4|Output file exists and stdin is redirected; use `--overwrite`|
|5|User declined overwrite at the prompt|

## Run the API

Start the API:

```bash
dotnet run --project src/CsvToMarkdown.Api
```

`dotnet run` uses `Properties/launchSettings.json`. Profiles set `ASPNETCORE\_ENVIRONMENT=Development` (Swagger enabled). The default `http` profile listens on `http://localhost:5085`:

* API base URL: `http://localhost:5085`
* Swagger UI: `http://localhost:5085/swagger`
* OpenAPI JSON: `http://localhost:5085/swagger/v1/swagger.json`

The `https` profile also binds `http://localhost:5085` plus `https://localhost:7042` if you need TLS locally.

### API contract

* `POST /jobs`: Upload a CSV as `multipart/form-data` in field `file`; returns `202 Accepted` with `id` and initial `status`.
* `GET /jobs/{id}`: Poll job status (`Queued`, `Running`, `Succeeded`, `Failed`) and error summary when present.
* `GET /jobs/{id}/result`: Download Markdown when complete (`text/markdown`).

### curl examples

Upload:

```bash
UPLOAD\_RESPONSE=$(curl -sS -X POST "http://localhost:5085/jobs" \\
  -F "file=@data/employees.csv;type=text/csv")
echo "$UPLOAD\_RESPONSE"
JOB\_ID=$(echo "$UPLOAD\_RESPONSE" | jq -r '.id')
```

Poll status until terminal:

```bash
while true; do
  STATUS\_RESPONSE=$(curl -sS "http://localhost:5085/jobs/$JOB\_ID")
  STATUS=$(echo "$STATUS\_RESPONSE" | jq -r '.status')
  echo "status=$STATUS"
  if \[ "$STATUS" = "Succeeded" ] || \[ "$STATUS" = "Failed" ]; then
    break
  fi
  sleep 1
done
```

Download result when succeeded:

```bash
curl -sS -f "http://localhost:5085/jobs/$JOB\_ID/result" -o "result-$JOB\_ID.md"
```

## Run tests

```bash
dotnet test
```

`dotnet test` is the routine full validation path and runs unit tests, large-file scenarios, and API integration coverage.

For faster local iteration, you can run only the API flow integration tests in the dedicated API test project:

```bash
dotnet test tests/CsvToMarkdown.Api.Tests/CsvToMarkdown.Api.Tests.csproj
```

Running the test suite writes inspectable outputs to `artifacts/test-output/`.

## Test artifacts

Every test run writes generated `.md` and `.csv` artifacts to `artifacts/test-output/`. You can open these files after `dotnet test` to inspect converter and API flow outputs:

|Artifact file|What it shows|
|-|-|
|`renderer-minimal-table.md`|Minimal two-column table from MarkdownRenderer unit test|
|`renderer-single-data-row.md`|Single data row rendering|
|`renderer-multi-row.md`|Multi-row table|
|`renderer-empty-field.md`|Table with an empty cell|
|`renderer-pipe-escape.md`|Cell value containing a pipe character|
|`integration-default-output-path.md`|End-to-end conversion using default output path|
|`integration-explicit-output-path.md`|End-to-end conversion using an explicit output path|
|`integration-quoted-comma-field.md`|Quoted comma inside a field rendered as one cell|
|`integration-missing-input-file.md`|Expected error message for a missing input file|
|`LargeFilesInACSV.csv`|Generated 50,000-row CSV input from the large-file streaming test|
|`large\_file\_output.md`|Output from the large-file streaming test (50,000 data rows)|
|`ApiSmallEdgeCaseInput.csv`|Upload source CSV for API small edge-case integration test|
|`api-small-edge-case-output.md`|Downloaded Markdown from API small edge-case integration test|
|`FilesInACSVLarge\_10000.csv`|Upload source CSV for API 10,000+ row integration test|
|`api-large-10000-output.md`|Downloaded Markdown from API 10,000+ row integration test|

The tests use `Contains` assertions rather than full-string golden comparisons, so the artifact files serve as human-readable output for manual review rather than as automated reference files.

## Refreshing artifacts after intentional output changes

If you change the Markdown output format (for example, the footer wording or column separator style), re-run `dotnet test`. The new `.md` files will overwrite the existing ones in `artifacts/test-output/`.

If any test assertions fail because of the intentional change, update the affected `Assert.Contains` calls in the relevant test files:

* `tests/CsvToMarkdown.Tests/MarkdownRendererTests.cs` -- unit tests for MarkdownRenderer
* `tests/CsvToMarkdown.Tests/CsvConverterIntegrationTests.cs` -- end-to-end integration tests
* `tests/CsvToMarkdown.Tests/LargeFileStreamingTests.cs` -- large-file streaming test
* `tests/CsvToMarkdown.Api.Tests/ApiJobFlowIntegrationTests.cs` -- API upload/poll/download flow; persists `api-small-edge-case-\*` and `api-large-10000-\*` under `artifacts/test-output/`

`tests/CsvToMarkdown.Tests/ApiIntegrationTests.cs` covers lightweight HTTP checks only; it does not write those `api-\*` artifacts or assert on the generated Markdown body.

Run `dotnet test` again after updating the assertions to confirm everything passes.







Copyright © 2026 GroundWorksDesign



This project is provided "AS IS", without warranty of any kind,

express or implied. Use at your own risk.



Commercial use, sale, or redistribution is not permitted without

explicit written permission from the copyright holder.



