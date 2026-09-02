# Testing report

**Project:** Smart Personal Knowledge Hub  
**Author:** Hossein Tabasi, M.Tech CSE, Shoolini University  
**Date of automated run:** 2 September 2026 (Asia/Calcutta)

## Environment for unit tests

| Item | Value |
| --- | --- |
| Host OS | Debian GNU/Linux 13 (x86_64) |
| SDK | .NET 8.0.424 |
| Test project | `tests/SmartKnowledgeHub.Tests` (`net8.0`) |
| Database | Temporary SQLite file per test fixture (`TempHub`) |
| Command | `dotnet test tests/SmartKnowledgeHub.Tests/SmartKnowledgeHub.Tests.csproj` |

The WPF project (`net8.0-windows`) was **not** executed on Linux. UI checks are listed under manual tests and must be performed on Windows.

## Automated results (this machine)

```
Passed!  - Failed:     0, Passed:    28, Skipped:     0, Total:    28
Duration: 198 ms
VSTest 17.11.1
```

No test counts were invented. The figures above are the `dotnet test` summary from this repository on the date shown.

### Test classes

| Class | What it covers |
| --- | --- |
| `DbInitializerTests` | Schema objects, idempotent `EnsureCreated`, WAL and foreign keys |
| `NoteRepositoryTests` | CRUD, pin/archive, tag replace |
| `TaskRepositoryTests` | Status filter, note link, due-soon window |
| `FileRepositoryTests` | Vault copy, binary skip, FTS delete |
| `TextExtractorTests` | Allowed extensions, markdown read, missing file |
| `SearchServiceTests` | FTS5 keyword hit, empty query, semantic ranking, MATCH quoting |
| `EmbeddingTests` | Self-cosine, related vs unrelated, missing ONNX, blob round-trip, factory fallback |
| `SeederAndMaintenanceTests` | 10 notes / 6 tasks / 3 files, dashboard, backup, VACUUM, AppPaths |

## What was not tested automatically

- WPF data binding, navigation and dialogs
- `Process.Start` “Open with OS”
- Actual ONNX Runtime inference (no model file is shipped)
- PDF or OCR pipelines (out of scope)
- Concurrent multi-process writers

## Manual test cases (Windows)

Perform these after F5 in Visual Studio 2022. Expected results are behavioural, not measured latency.

| ID | Steps | Expected |
| --- | --- | --- |
| M1 | Fresh install, start the app | Window opens; left rail shows Dashboard, Notes, Tasks, Files, Search, Settings; `hub.db` appears under LocalAppData. |
| M2 | Settings → Seed sample data | Message reports 10 notes, 6 tasks, 3 files; Dashboard counts update. |
| M3 | Seed a second time | Seeder refuses to duplicate; counts unchanged. |
| M4 | Notes: create, edit title/body/tags, Save | List refreshes; SQLite `Notes` and `NoteTags` rows update. |
| M5 | Pin and archive a note | Pinned notes sort first; archived hidden unless “Show archived” is on. |
| M6 | Delete a note | Row removed; FTS and SearchIndex no longer return it. |
| M7 | Tasks: filter Todo / Doing / Done | List matches status; Mark done moves the item. |
| M8 | Link a task to a note | Combo box stores `NoteId`; title appears when reopened. |
| M9 | Files: register `welcome.md` with vault copy | Record listed; copy exists under `vault\`; keyword search finds “welcome”. |
| M10 | Register a `.bin` or `.png` | Metadata stored; no extracted text; app does not crash. |
| M11 | Open file | Default Windows handler launches. Missing path shows a message. |
| M12 | Search keywords `fts5` | Hits include the FTS5 study note; source label “FTS5 BM25”. |
| M13 | Toggle Related meaning, query `database search ranking` | SQLite/FTS notes rank above the tea/shopping notes. No AUC is claimed. |
| M14 | Settings → VACUUM | Completes; app still opens the database. |
| M15 | Settings → Backup | Timestamped `hub-*.db` appears under `backups\`. |
| M16 | Close and reopen | Data persists; no login screen. |

## How to re-run

```text
dotnet test tests/SmartKnowledgeHub.Tests/SmartKnowledgeHub.Tests.csproj
```

On Windows, Test Explorer in Visual Studio 2022 runs the same assembly. Do not use `dotnet test SmartKnowledgeHub.sln` on Linux: MSBuild will attempt the WPF project and fail for lack of a Windows targeting pack.
