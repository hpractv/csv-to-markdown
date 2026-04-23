# Epic: CSV → Markdown table (C#) — API + jobs

## Objective

Expose an **HTTP API** that accepts an uploaded CSV and produces the same Markdown table semantics as Epic 1 (header row, body rows, footer line with total data-row count). Conversion runs **asynchronously**: the client receives a job identifier, **polls status** on another endpoint, then **downloads** the `.md` when the job completes. Preserve **streaming-friendly** handling for large CSVs server-side where practical.

## Functional requirements

### Conversion (same semantics as library epic)

1. CSV parsing: delimiter and quoting per normal CSV rules; empty cells and quoted fields handled.
2. Markdown: valid GFM-style pipe table; body row count matches parsed data rows after the header.
3. Title: add a Markdown H1 from the input CSV file name (without extension). Transform rules: replace `_` and `-` with spaces, and insert spaces before capital letters in concatenated words. Examples: `FilesInACSV.csv` -> `# Files In A CSV`, `SomethingToBehold.csv` -> `# Something To Behold`.
4. Footer: one trailing line stating total data rows (excluding header).

### HTTP API

5. **Upload:** `multipart/form-data` (or documented equivalent) accepts the CSV file; response returns a **job id** (and optionally initial status). Does not block until conversion finishes.
6. **Status:** Request with job id returns processing state (e.g. queued / running / succeeded / failed) and, on failure, an error summary safe for clients.
7. **Download:** When succeeded, a separate request retrieves the generated Markdown (same basename semantics as source file name where applicable, or documented naming). Failed or incomplete jobs do not return success content as if complete.

## Testing (must ship with the feature)

- **Unit tests:** C# test project (xUnit or NUnit); converter logic covered with small fixtures; artifacts under a fixed folder (e.g. `artifacts/test-output/`).
- **Large-file scenario:** Automated coverage using **≥ 10,000 data rows** (plus header): generate or commit a large CSV fixture, run conversion (via API or shared service under test), **persist both the input CSV and output `.md`** under the artifact folder for side-by-side inspection and regression.
- **API tests:** Integration tests exercise upload → poll until terminal state → download; assert status transitions and final Markdown/footer correctness for at least the large-file case and one small edge-case file.
- **Development workflow:** **Running tests (including the large-file and API integration path) is part of routine development**. Use `dotnet test` as the default full-matrix command and document any optional focused commands used for local iteration.

## Definition of Done

- [x] Async job API: upload, status, download (or equivalent documented download contract) implemented and documented (OpenAPI/Swagger or README).
- [x] Status endpoint allows clients to poll until completion without holding a long-lived upload connection.
- [x] Conversion matches title + table + footer rules; streaming or chunked processing justified for large inputs.
- [x] `dotnet test` (per documented dev workflow) passes; **artifact directory contains retained input/output pairs**, including **10k-row** run artifacts for comparison.
- [x] Footer row count matches data rows in the emitted table for persisted large-file output.

## Out of scope (unless explicitly added later)

- GUI SPA (browser UI); native desktop app.
- Non-CSV formats, Excel.
- Encoding detection beyond a documented default (e.g. UTF-8).

## Loop hints

- Slice vertically: shared converter core → minimal API + in-memory job store → integration tests → persist artifacts → tighten large-file performance if needed.
- Stop when Definition of Done is satisfied; extend scope only by updating this epic.
