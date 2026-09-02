using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using SmartKnowledgeHub.App.Services;
using SmartKnowledgeHub.App.ViewModels;
using SmartKnowledgeHub.Core.Data;
using SmartKnowledgeHub.Core.Embedding;
using SmartKnowledgeHub.Core.Repositories;
using SmartKnowledgeHub.Core.Search;
using SmartKnowledgeHub.Core.Services;

namespace SmartKnowledgeHub.App;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandled;

        var paths = AppPaths.ForDefault();
        var factory = new SqliteConnectionFactory(paths.DatabasePath);
        DbInitializer.EnsureCreated(factory);
        var embedding = EmbeddingFactory.CreateDefault(paths.OnnxModelPath);

        var services = new ServiceCollection();
        services.AddSingleton(paths);
        services.AddSingleton(factory);
        services.AddSingleton<IEmbeddingService>(embedding);
        services.AddSingleton<IUserPrompt, WpfUserPrompt>();
        services.AddSingleton<ITagRepository, TagRepository>();
        services.AddSingleton<INotebookRepository, NotebookRepository>();
        services.AddSingleton<INoteRepository, NoteRepository>();
        services.AddSingleton<ITaskRepository, TaskRepository>();
        services.AddSingleton<IFileRepository, FileRepository>();
        services.AddSingleton<ISearchService>(sp => new SearchService(
            sp.GetRequiredService<SqliteConnectionFactory>(),
            sp.GetRequiredService<IEmbeddingService>(),
            sp.GetRequiredService<INoteRepository>(),
            sp.GetRequiredService<ITaskRepository>(),
            sp.GetRequiredService<IFileRepository>(),
            paths.OnnxModelPath));
        services.AddSingleton<FileVaultService>();
        services.AddSingleton<DashboardService>();
        services.AddSingleton<DatabaseMaintenance>();
        services.AddSingleton<SampleDataSeeder>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<NotesViewModel>();
        services.AddSingleton<TasksViewModel>();
        services.AddSingleton<FilesViewModel>();
        services.AddSingleton<SearchViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();
        Services = services.BuildServiceProvider();

        var window = new MainWindow
        {
            DataContext = Services.GetRequiredService<MainViewModel>()
        };
        window.Show();
    }

    private static void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        MessageBox.Show(args.Exception.Message, "Smart Personal Knowledge Hub", MessageBoxButton.OK, MessageBoxImage.Error);
        args.Handled = true;
    }
}
