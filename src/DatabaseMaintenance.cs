using SmartKnowledgeHub.Core.Data;

namespace SmartKnowledgeHub.Core.Services;

public sealed class DatabaseMaintenance
{
    private readonly SqliteConnectionFactory _factory;
    private readonly AppPaths _paths;

    public DatabaseMaintenance(SqliteConnectionFactory factory, AppPaths paths)
    {
        _factory = factory;
        _paths = paths;
    }

    public void Vacuum()
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "VACUUM;";
        cmd.ExecuteNonQuery();
    }

    public string Backup()
    {
        Directory.CreateDirectory(_paths.BackupDirectory);
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var dest = Path.Combine(_paths.BackupDirectory, $"hub-{stamp}.db");
        File.Copy(_factory.DatabasePath, dest, overwrite: false);
        return dest;
    }
}
