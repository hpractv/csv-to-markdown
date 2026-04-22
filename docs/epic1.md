# Epic: CSV → Markdown table (C#)

## Objective

Accept a CSV input path (streaming-friendly for large files). Emit a Markdown file: same basename as the input, extension `.md`. First row → table header; following rows → body. Append one final line reporting the row count in the table body.

Development includes running the test suite locally as you implement (after each meaningful change or vertical slice), not only before release.

## Functional requirements

1. Input: path to a CSV file (delimiter and quoting per normal CSV rules; handle empty cells and quoted fields).
2. Output: `{basename(input)}.md` alongside the input or at a caller-specified output path (choose one convention in code and document it).
3. Markdown: valid GFM-style pipe table with a header row; body row count equals parsed data rows after the header.
4. Footer: a single trailing line stating the total data rows (not including the header row).

## Testing (part of development; must ship with the feature)

- **During development:** Run `dotnet test` while building the feature—same as you would compile—so regressions surface immediately. Treat failing tests as blocking further work on that slice until fixed or expectations updated deliberately.
- **Project:** C# test project (xUnit or NUnit) references the converter library/API.
- **Persisted artifacts:** Every test run writes generated `.md` (and golden/expected files if used) under a fixed repo-relative folder, e.g. `artifacts/test-output/` or `TestResults/parser/`, so humans and the loop can open files after `dotnet test`.
- **Coverage:** At least—minimal table, multi-row, and one edge case (empty field and/or quoted comma).
- **Failure signal:** On mismatch, persisted actual output remains on disk for diffing.
- **Note in repo:** One short note (README section or test `README`) stating artifact path and how to refresh goldens when output is intentionally changed.

## Definition of Done

- [ ] Library or console entry point converts sample CSV to `.md` matching rules above.
- [ ] Large-file path does not load the entire CSV into memory unnecessarily (streaming or chunked read—justify in code if full read is acceptable for MVP).
- [ ] `dotnet test` is used throughout development and passes before the work is considered done; artifact folder contains inspectable `.md` outputs from the last run.
- [ ] Footer row count matches the number of data rows in the emitted table.

## Out of scope (unless explicitly added later)

- GUI, HTTP API, database.
- Non-CSV formats, Excel, or encoding detection beyond a documented default (e.g. UTF-8).

## Loop hints

- Prefer small vertical slices: parser → MD renderer → tests with golden/persisted files → run `dotnet test` → streaming if tests pass on small inputs.
- After each slice or significant edit, run tests before moving on so failures stay localized and cheap to fix.
- Stop when Definition of Done is satisfied; do not expand scope without updating this epic.
