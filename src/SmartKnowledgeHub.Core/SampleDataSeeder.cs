using SmartKnowledgeHub.Core.Models;
using SmartKnowledgeHub.Core.Repositories;
using SmartKnowledgeHub.Core.Search;
using SmartKnowledgeHub.Core.Services;

namespace SmartKnowledgeHub.Core.Data;

public sealed class SampleDataSeeder
{
    private readonly INotebookRepository _notebooks;
    private readonly INoteRepository _notes;
    private readonly ITaskRepository _tasks;
    private readonly ISearchService _search;
    private readonly FileVaultService _vault;
    private readonly AppPaths _paths;

    public SampleDataSeeder(
        INotebookRepository notebooks,
        INoteRepository notes,
        ITaskRepository tasks,
        ISearchService search,
        FileVaultService vault,
        AppPaths paths)
    {
        _notebooks = notebooks;
        _notes = notes;
        _tasks = tasks;
        _search = search;
        _vault = vault;
        _paths = paths;
    }

    public SeedResult Seed(string? sampleNotesDirectory = null)
    {
        if (_notes.Count(includeArchived: true) > 0 || _tasks.GetAll().Count > 0)
        {
            return new SeedResult(false, "Sample data was not inserted because the database already contains notes or tasks.");
        }

        var study = _notebooks.Insert("Study");
        var research = _notebooks.Insert("Research");
        var personal = _notebooks.Insert("Personal");

        var notes = new List<Note>
        {
            Make(study, "Welcome to Smart Knowledge Hub", WelcomeBody, true, false, "intro", "hub"),
            Make(study, "SQLite FTS5 revision notes", FtsBody, true, false, "sqlite", "search", "exam"),
            Make(study, "MVVM checklist for the WPF client", MvvmBody, false, false, "wpf", "mvvm"),
            Make(study, "Operating systems: CPU scheduling", OsBody, false, false, "os", "exam"),
            Make(research, "Personal knowledge bases after Memex", ResearchBody, true, false, "pkb", "research"),
            Make(research, "Sentence embeddings for local search", EmbedBody, false, false, "nlp", "search"),
            Make(personal, "Weekly review template", ReviewBody, false, false, "habits"),
            Make(personal, "Campus library hours and quiet floors", CampusBody, false, false, "campus"),
            Make(study, "Git workflow reminder", GitBody, false, false, "git"),
            Make(personal, "Old shopping list", "Milk, lentils, notebooks. Archived on purpose.", false, true, "archive")
        };

        foreach (var note in notes)
        {
            _notes.Insert(note);
        }

        var ftsNote = notes[1];
        var pkbNote = notes[4];

        var today = DateTime.UtcNow.Date;
        var tasks = new[]
        {
            new TaskItem { Title = "Finish chapter 4 operating-system notes", Body = "Summarise scheduling algorithms with examples.", DueUtc = today.AddDays(2), Priority = TaskPriorities.High, Status = TaskStatuses.Todo, NoteId = notes[3].Id },
            new TaskItem { Title = "Prepare viva talking points", Body = "Cover MVVM, FTS5 and the offline embedding fallback.", DueUtc = today.AddDays(5), Priority = TaskPriorities.High, Status = TaskStatuses.Doing },
            new TaskItem { Title = "Backup thesis folder", Body = "Copy the latest draft into the hub vault.", DueUtc = today.AddDays(1), Priority = TaskPriorities.Normal, Status = TaskStatuses.Todo },
            new TaskItem { Title = "Tag older lecture notes", Body = "Add exam and sqlite tags.", Priority = TaskPriorities.Low, Status = TaskStatuses.Done },
            new TaskItem { Title = "Read FTS5 documentation again", Body = "bm25 ranking and contentless tables.", DueUtc = today.AddDays(3), Priority = TaskPriorities.Normal, Status = TaskStatuses.Doing, NoteId = ftsNote.Id },
            new TaskItem { Title = "Collect citations for the project report", Body = "Keep the locked IEEE reference list unchanged.", DueUtc = today.AddDays(6), Priority = TaskPriorities.High, Status = TaskStatuses.Todo, NoteId = pkbNote.Id }
        };

        foreach (var task in tasks)
        {
            _tasks.Insert(task);
        }

        var sampleDir = sampleNotesDirectory
            ?? Path.Combine(AppContext.BaseDirectory, "Assets", "sample-notes");
        Directory.CreateDirectory(sampleDir);

        var welcomePath = Path.Combine(sampleDir, "welcome.md");
        if (!File.Exists(welcomePath))
        {
            File.WriteAllText(welcomePath, WelcomeMarkdown);
        }

        var ftsPath = Path.Combine(sampleDir, "fts5-cheatsheet.md");
        if (!File.Exists(ftsPath))
        {
            File.WriteAllText(ftsPath, FtsCheatSheet);
        }

        var csvPath = Path.Combine(sampleDir, "reading-list.csv");
        if (!File.Exists(csvPath))
        {
            File.WriteAllText(csvPath, "title,author,status\nAs We May Think,Vannevar Bush,read\nPopcorn PKB,Davies et al.,read\nSQLite FTS5,Hipp,in-progress\n");
        }

        _vault.Register(welcomePath, "sample,welcome", copyIntoVault: true);
        _vault.Register(ftsPath, "sample,sqlite", copyIntoVault: true);
        _vault.Register(csvPath, "sample,csv", copyIntoVault: true);

        _search.RebuildAll();
        return new SeedResult(true, "Inserted 10 notes, 6 tasks and 3 file records, then rebuilt the search index.");
    }

    private static Note Make(long notebookId, string title, string body, bool pin, bool archive, params string[] tags) =>
        new()
        {
            NotebookId = notebookId,
            Title = title,
            Body = body,
            IsPinned = pin,
            IsArchived = archive,
            Tags = tags.ToList()
        };

    private const string WelcomeBody =
        "This desktop hub stores notes, tasks and file metadata in a local SQLite database. " +
        "Search uses FTS5 with bm25 ranking. Related meaning uses a hashed TF-IDF vector that runs offline on a student laptop. " +
        "An optional ONNX MiniLM file may be dropped into Assets/models; if it is absent the application still starts.";

    private const string FtsBody =
        "FTS5 is SQLite's full-text engine. Virtual tables NotesFts, TasksFts and FilesFts are kept in sync with content tables through triggers. " +
        "Queries should quote user tokens. Ranking uses bm25, a probabilistic retrieval function. " +
        "Porter stemming helps exam revision notes match stemmed query terms such as searching and search.";

    private const string MvvmBody =
        "Keep views thin. ViewModels expose ObservableProperty and RelayCommand members. " +
        "Repositories live in the Core library so unit tests can run without WPF. " +
        "Dependency injection is constructed once in the application startup class.";

    private const string OsBody =
        "CPU scheduling: FCFS, SJF, Round Robin and priority queues. " +
        "Context-switch cost matters. Compare average waiting time on paper before the viva. " +
        "Relate the idea of ranking ready processes to ranking search hits with bm25.";

    private const string ResearchBody =
        "Vannevar Bush described the Memex in 1945. Later personal knowledge bases such as Popcorn studied how people organise notes. " +
        "A student-built offline Windows client is justified by privacy, inspectable SQLite files and coursework constraints: no subscription and no cloud account.";

    private const string EmbedBody =
        "Sentence-BERT produces dense sentence embeddings, which is useful when a model file is available. " +
        "This project defaults to feature hashing plus TF-IDF cosine similarity so a laptop without a GPU still searches related meaning. " +
        "Do not invent latency or AUC numbers; report only what the unit tests measure.";

    private const string ReviewBody =
        "Weekly review: what did I finish, what is due, which notes should be pinned? " +
        "Use the dashboard counts and the due-task list. Archive notes that are no longer active.";

    private const string CampusBody =
        "Library quiet floors are best in the afternoon. Carry a local copy of notes because campus Wi-Fi is not required for this hub.";

    private const string GitBody =
        "Commit small, related changes. Never put secrets in the repository. The hub database lives under LocalAppData, not in git.";

    private const string WelcomeMarkdown =
        "# Welcome\n\n" +
        "Smart Personal Knowledge Hub keeps your notes, tasks and file paths on this computer.\n\n" +
        "- Notes support tags, notebooks, pin and archive.\n" +
        "- Tasks track Todo / Doing / Done with optional due dates.\n" +
        "- Search is FTS5 first; Related meaning is hashed TF-IDF cosine.\n";

    private const string FtsCheatSheet =
        "# FTS5 cheat sheet\n\n" +
        "MATCH queries, bm25 ranking, porter tokenizer, content-sync triggers.\n" +
        "User input is split into quoted tokens joined with OR.\n";

    public readonly record struct SeedResult(bool Inserted, string Message);
}
