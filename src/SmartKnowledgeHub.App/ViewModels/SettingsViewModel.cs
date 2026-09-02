using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartKnowledgeHub.App.Services;
using SmartKnowledgeHub.Core.Data;
using SmartKnowledgeHub.Core.Search;
using SmartKnowledgeHub.Core.Services;

namespace SmartKnowledgeHub.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AppPaths _paths;
    private readonly SampleDataSeeder _seeder;
    private readonly DatabaseMaintenance _maintenance;
    private readonly ISearchService _search;
    private readonly IUserPrompt _prompt;

    public SettingsViewModel(
        AppPaths paths,
        SampleDataSeeder seeder,
        DatabaseMaintenance maintenance,
        ISearchService search,
        IUserPrompt prompt)
    {
        _paths = paths;
        _seeder = seeder;
        _maintenance = maintenance;
        _search = search;
        _prompt = prompt;
        Refresh();
    }

    [ObservableProperty] private string _dataFolder = string.Empty;
    [ObservableProperty] private string _databasePath = string.Empty;
    [ObservableProperty] private string _vaultFolder = string.Empty;
    [ObservableProperty] private string _embeddingName = string.Empty;
    [ObservableProperty] private string _onnxStatus = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;

    public void Refresh()
    {
        DataFolder = _paths.RootDirectory;
        DatabasePath = _paths.DatabasePath;
        VaultFolder = _paths.VaultDirectory;
        EmbeddingName = _search.EmbeddingName;
        OnnxStatus = _search.OptionalOnnxAvailable
            ? "minilm.onnx is present. This build still uses hashed TF-IDF unless ONNX Runtime is added later."
            : "Optional model not found (Assets/models/minilm.onnx). FTS5 + TF-IDF remain active.";
    }

    [RelayCommand]
    private void SeedSampleData()
    {
        var sampleDir = Path.Combine(AppContext.BaseDirectory, "Assets", "sample-notes");
        var result = _seeder.Seed(sampleDir);
        StatusText = result.Message;
        _prompt.Alert(result.Message, "Sample data");
        Refresh();
    }

    [RelayCommand]
    private void Vacuum()
    {
        _maintenance.Vacuum();
        StatusText = "VACUUM completed.";
        _prompt.Alert("The SQLite database was vacuumed.", "Maintenance");
    }

    [RelayCommand]
    private void Backup()
    {
        var dest = _maintenance.Backup();
        StatusText = "Backup written to " + dest;
        _prompt.Alert("Backup created:\n" + dest, "Maintenance");
    }

    [RelayCommand]
    private void RebuildSearch()
    {
        _search.RebuildAll();
        StatusText = "Search index rebuilt.";
        _prompt.Alert("FTS5 content is trigger-synced; the TF-IDF search index was rebuilt.", "Search");
    }

    [RelayCommand]
    private void OpenDataFolder()
    {
        _prompt.OpenInOs(_paths.RootDirectory);
    }
}
