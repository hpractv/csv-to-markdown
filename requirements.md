# Requirements: CSV to Markdown Table API (Async Jobs)

## Objective

Expose an HTTP API that accepts an uploaded CSV and produces the same Markdown table semantics as the library implementation:
- H1 title derived from the input filename (without extension)
- header row
- body rows
- trailing footer line with total data-row count

Conversion must run asynchronously: clients submit a file, receive a job id, poll status, then download the generated `.md` when complete. Server-side handling should remain streaming-friendly for large CSV inputs where practical.

## Functional Requirements

### Conversion Semantics

1. CSV parsing supports normal delimiter and quoting rules, including empty cells and quoted fields.
2. Markdown output is a valid GFM-style pipe table.
3. Body row count in the table matches parsed data rows after the header.
4. Output starts with an H1 from the input CSV file name (without extension). Replace `_` and `-` with spaces and insert spaces before capital letters in concatenated words (for example, `FilesInACSV.csv` -> `# Files In A CSV`).
5. Output includes one trailing footer line stating total data rows (excluding the header).

### HTTP API Contract

6. Upload endpoint accepts `multipart/form-data` (or documented equivalent) and returns a job id (optionally with initial status) without blocking until conversion finishes.
7. Status endpoint accepts a job id and returns processing state (`queued`, `running`, `succeeded`, `failed` or equivalent). Failed states must include a client-safe error summary.
8. Download endpoint returns generated Markdown only when the job has succeeded. Incomplete or failed jobs must not return success content as if complete.
9. Output naming follows source basename semantics where applicable, or the naming behavior is explicitly documented.

## Testing Expectations

1. Include a C# unit test project (xUnit or NUnit) covering converter logic with small fixtures.
2. Persist test artifacts under a fixed folder (`artifacts/test-output/`), including input/output pairs used for verification.
3. Include an automated large-file scenario with at least 10,000 data rows (plus header).
4. For the large-file scenario, persist both the generated input CSV and resulting Markdown output under the artifact folder for side-by-side inspection and regression checks.
5. Include API integration tests covering upload -> poll until terminal state -> download.
6. Integration coverage must validate status transitions and final Markdown/footer correctness for:
   - at least one large-file case, and
   - at least one small edge-case file.
7. Document the development workflow for routine validation where `dotnet test` runs the default full matrix (unit, large-file, and API integration coverage), plus any optional focused test commands for local iteration.

## Definition of Done

- [x] Async job API (upload, status, download or equivalent documented contract) is implemented and documented.
- [x] Status polling supports completion checks without a long-lived upload request.
- [x] Conversion behavior matches required title, table, and footer rules, with large-input handling that is streaming/chunked or otherwise justified.
- [x] Test workflow passes (`dotnet test` per documented process), and artifact output includes retained input/output pairs, including a 10k-row run.
- [x] Persisted large-file Markdown footer count matches emitted data-row count.
