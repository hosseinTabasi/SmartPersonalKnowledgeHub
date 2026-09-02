namespace SmartKnowledgeHub.Core.Data;

/// <summary>
/// Local data folder for the hub database and optional file vault.
/// Default root: %LocalAppData%/SmartKnowledgeHub on Windows.
/// </summary>
public sealed class AppPaths
{
    public const string FolderName = "SmartKnowledgeHub";
    public const string DatabaseFileName = "hub.db";

    public AppPaths(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("Root directory is required.", nameof(rootDirectory));
        }

        RootDirectory = rootDirectory;
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(VaultDirectory);
        Directory.CreateDirectory(BackupDirectory);
    }

    public string RootDirectory { get; }
    public string DatabasePath => Path.Combine(RootDirectory, DatabaseFileName);
    public string VaultDirectory => Path.Combine(RootDirectory, "vault");
    public string BackupDirectory => Path.Combine(RootDirectory, "backups");
    public string ModelsDirectory => Path.Combine(AppContext.BaseDirectory, "Assets", "models");
    public string OnnxModelPath => Path.Combine(ModelsDirectory, "minilm.onnx");

    public static AppPaths ForDefault()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(local))
        {
            local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        }

        return new AppPaths(Path.Combine(local, FolderName));
    }
}
