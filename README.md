# Smart Personal Knowledge Hub

A Windows desktop application for **notes, tasks, files and local search**. All data stays on the machine in a SQLite file under `%LocalAppData%\SmartKnowledgeHub`. There is no cloud account and no telemetry.

**Author:** Hossein Tabasi  
**Programme:** M.Tech Computer Science and Engineering  
**Institution:** Shoolini University  
**Year:** 2026  
**GitHub:** [hosseinTabasi](https://github.com/hosseinTabasi)

## What it does

- Notes with notebooks, tags, pin and archive
- Tasks with priority, status (Todo / Doing / Done), due dates and optional linked notes
- File registration (optional copy into a local vault); text extraction for `.txt`, `.md`, `.csv` only
- Keyword search via SQLite FTS5 and bm25
- “Related meaning” ranking via hashed TF-IDF cosine similarity (offline, no model download)
- Optional ONNX MiniLM **only if** you place `Assets/models/minilm.onnx` yourself; the app still runs when that file is missing
- Dashboard counts, sample-data seeder, VACUUM and SQLite backup

## Solution layout

| Project | Target | Role |
| --- | --- | --- |
| `src/SmartKnowledgeHub.Core` | `net8.0` | Models, SQLite, FTS5, search, embeddings, seeder |
| `src/SmartKnowledgeHub.App` | `net8.0-windows` | WPF + MVVM client |
| `tests/SmartKnowledgeHub.Tests` | `net8.0` | xUnit tests against a temp SQLite file |

Open `SmartKnowledgeHub.sln` in Visual Studio 2022.

## Run (Windows)

See [docs/VS2022_SETUP.md](docs/VS2022_SETUP.md) for the full Visual Studio 2022 walkthrough.

Short version:

1. Install Visual Studio 2022 with the **.NET desktop development** workload (.NET 8).
2. Clone or copy this folder.
3. Double-click `SmartKnowledgeHub.sln`.
4. Restore NuGet packages if prompted.
5. Set `SmartKnowledgeHub.App` as the startup project and press **F5**.

The first launch creates `%LocalAppData%\SmartKnowledgeHub\hub.db`. Use **Settings → Seed sample data** to insert demonstration notes, tasks and sample files.

## Tests

From a machine with the .NET 8 SDK:

```bash
dotnet test tests/SmartKnowledgeHub.Tests/SmartKnowledgeHub.Tests.csproj
```

WPF (`net8.0-windows`) will not compile on Linux. Domain tests live in Core and **do** run on Linux. Recorded results: [docs/TESTING.md](docs/TESTING.md).

## Screenshots

Capture guidance is in [docs/SCREENSHOTS.md](docs/SCREENSHOTS.md). Place PNG files in `screenshots/` after you run the WPF client on Windows. This repository does not ship fake UI images.

## Academic documents

- [docs/PROJECT_REPORT.md](docs/PROJECT_REPORT.md) — full project report
- [docs/VIVA.md](docs/VIVA.md) — viva talking points
- [docs/TESTING.md](docs/TESTING.md) — unit and manual tests
- [docs/VS2022_SETUP.md](docs/VS2022_SETUP.md) — lab PC setup

## Data location

| Item | Path |
| --- | --- |
| Database | `%LocalAppData%\SmartKnowledgeHub\hub.db` |
| Vault copies | `%LocalAppData%\SmartKnowledgeHub\vault\` |
| Backups | `%LocalAppData%\SmartKnowledgeHub\backups\` |

Inspect the database with any SQLite browser. Schema objects include `Notebooks`, `Notes`, `Tags`, `NoteTags`, `Tasks`, `Files`, FTS5 virtual tables and `SearchIndex`.

## License

MIT — see [LICENSE](LICENSE). Copyright (c) 2026 Hossein Tabasi.
