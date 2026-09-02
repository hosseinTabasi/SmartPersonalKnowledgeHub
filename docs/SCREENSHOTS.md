# Screenshot checklist

Capture these images on **Windows** after running the WPF client. Save PNG files in the `screenshots/` folder using the names below. Do not invent or edit fake UI mock-ups into this repository.

Window size: approximately 1180×760, light Fluent theme, default scaling 100% or 125%.

| File | When to capture | What should be visible |
| --- | --- | --- |
| `01-dashboard.png` | After seeding sample data | Left rail with Dashboard selected; four count cards; recent notes; due tasks. |
| `02-notes-list.png` | Notes page | Notebook combo, note list with pinned items first, status text. |
| `03-note-editor.png` | A study note selected | Title, tags, body, pin/archive checkboxes, Save button. |
| `04-tasks.png` | Tasks page | Filter chips (All / Todo / Doing / Done) and the editor for a high-priority task. |
| `05-files.png` | Files page | `welcome.md` (or another vault copy) selected; original and vault paths; Open button. |
| `06-search-fts.png` | Search, keywords mode | Query `fts5`; result list with BM25 source labels. |
| `07-search-related.png` | Search, Related meaning | Toggle on; query about database search; cosine-style scores. |
| `08-settings.png` | Settings | Data folder, database path, embedding name, seed / VACUUM / backup buttons. |
| `09-empty-first-run.png` | Optional: before seeding | Zero counts; empty lists; still navigable. |

Caption suggestion for the report: “Figure N. Smart Personal Knowledge Hub — [page name] (author screenshot, Windows 11, 2026).”

The `screenshots/` directory is present in the tree so that files can be dropped in after the viva rehearsal.
