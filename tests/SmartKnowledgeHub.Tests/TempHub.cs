using SmartKnowledgeHub.Core.Data;
using SmartKnowledgeHub.Core.Embedding;
using SmartKnowledgeHub.Core.Repositories;
using SmartKnowledgeHub.Core.Search;
using SmartKnowledgeHub.Core.Services;

namespace SmartKnowledgeHub.Tests;

public sealed class TempHub : IDisposable
{
    public TempHub()
    {
        Root = Path.Combine(Path.GetTempPath(), "skh-tests-" + Guid.NewGuid().ToString("N"));
        Paths = new AppPaths(Root);
        Factory = new SqliteConnectionFactory(Paths.DatabasePath);
        DbInitializer.EnsureCreated(Factory);
        Tags = new TagRepository(Factory);
        Notebooks = new NotebookRepository(Factory);
        Notes = new NoteRepository(Factory, Tags);
        Tasks = new TaskRepository(Factory);
        Files = new FileRepository(Factory);
        Embedding = new HashedTfidfEmbeddingService();
        Search = new SearchService(Factory, Embedding, Notes, Tasks, Files, Paths.OnnxModelPath);
        Vault = new FileVaultService(Paths, Files, Search);
        Dashboard = new DashboardService(Notes, Tasks, Files);
        Maintenance = new DatabaseMaintenance(Factory, Paths);
        Seeder = new SampleDataSeeder(Notebooks, Notes, Tasks, Search, Vault, Paths);
    }

    public string Root { get; }
    public AppPaths Paths { get; }
    public SqliteConnectionFactory Factory { get; }
    public TagRepository Tags { get; }
    public NotebookRepository Notebooks { get; }
    public NoteRepository Notes { get; }
    public TaskRepository Tasks { get; }
    public FileRepository Files { get; }
    public HashedTfidfEmbeddingService Embedding { get; }
    public SearchService Search { get; }
    public FileVaultService Vault { get; }
    public DashboardService Dashboard { get; }
    public DatabaseMaintenance Maintenance { get; }
    public SampleDataSeeder Seeder { get; }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
        catch (IOException)
        {
            // Temp leftovers are acceptable on a locked file handle.
        }
    }
}
