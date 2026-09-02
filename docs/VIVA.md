# Viva talking points

**Candidate:** Hossein Tabasi  
**Project:** Smart Personal Knowledge Hub  
**Degree:** M.Tech Computer Science and Engineering, Shoolini University, 2026

Keep answers short. Point at the running Windows app or at SQLite when possible.

## One-minute pitch

This is an offline Windows desktop hub for a student’s notes, tasks and local files. Data lives in one SQLite file. Search is FTS5 with bm25. A second mode, Related meaning, uses hashed TF-IDF cosine similarity so the laptop does not need a downloaded neural model. The UI is WPF with MVVM. Business logic is in a `net8.0` library so unit tests run without Windows.

## Likely questions

### Why not Notion, OneNote or Obsidian?

Those products already exist. The coursework goal is to *build* an inspectable Windows client: MVVM, parameterised SQL, FTS5, and a local file the examiner can open. Privacy and zero subscription are extra reasons, not a claim that the hub replaces commercial PKB tools.

### Why split Core and App?

WPF requires `net8.0-windows`. The laboratory Linux image can still compile Core and run 28 xUnit tests against a temp database. That split is an engineering constraint, not an afterthought.

### How does FTS5 stay in sync?

`NotesFts` and `TasksFts` are content-synced virtual tables. Insert/update/delete triggers copy title and body. `FilesFts` is maintained in `FileRepository` because extracted text is not a column on `Files`.

### What is bm25 here?

A ranking function over FTS5 matches. SQLite exposes `bm25()`; lower scores are better in that function, so keyword results are ordered by that value. Cite Robertson and Zaragoza in the report, not a made-up accuracy percentage.

### What is “Related meaning” if there is no MiniLM file?

Tokens are hashed into a 256-dimension vector, weighted with TF-IDF from the local `SearchIndex` corpus, L2-normalised, then compared with cosine similarity. It is a classical IR baseline. Sentence-BERT is discussed as future work and as the reason an ONNX hook exists.

### What happens if `minilm.onnx` is missing?

`OnnxEmbeddingService.IsAvailable` is false. `EmbeddingFactory` returns hashed TF-IDF. The app starts. No installer downloads a model.

### Where is the database?

`%LocalAppData%\SmartKnowledgeHub\hub.db` (WAL mode). Show it in DB Browser for SQLite during the viva if the lab PC has it.

### How is MVVM applied?

Views are XAML UserControls. ViewModels use `ObservableObject`, `[ObservableProperty]` and `[RelayCommand]` from the MVVM Toolkit. Repositories are not referenced from code-behind. `App.xaml.cs` builds a small `IServiceProvider`. `MainWindow.xaml.cs` only calls `InitializeComponent`.

### What is out of scope?

Cloud sync, accounts, telemetry, a full Markdown WYSIWYG editor, PDF parsers, OCR, multi-user locking, and invented user-study statistics.

### How did you test?

28 automated tests on Core (0 failed on 2 September 2026). Manual cases M1–M16 on Windows, listed in `docs/TESTING.md`. Do not quote UI frame rates; none were measured.

### Show me the schema.

Walk through `DbInitializer.SchemaSql`: notebooks, notes, tags, note-tags, tasks, files, search index, three FTS5 tables, triggers.

### If I type SQL injection in the search box?

Commands use parameters. FTS MATCH input is stripped of punctuation and quoted as tokens joined with OR.

## Demo script (five minutes)

1. Start the app, Settings → Seed sample data.
2. Dashboard cards and due tasks.
3. Open the FTS5 note, change a tag, Save.
4. Tasks: filter Todo, mark one Done.
5. Search `fts5`, then switch to Related meaning with `full text search`.
6. Settings: show paths; optionally Backup.

## If something breaks live

- Empty search: rebuild index.
- Duplicate seed: expected refusal.
- Locked database: close a second instance.
- Missing file on Open: the original path was moved; that is honest behaviour.
