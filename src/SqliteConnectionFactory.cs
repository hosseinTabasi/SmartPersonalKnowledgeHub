using Microsoft.Data.Sqlite;

namespace SmartKnowledgeHub.Core.Data;

public sealed class SqliteConnectionFactory
{
    public SqliteConnectionFactory(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path is required.", nameof(databasePath));
        }

        DatabasePath = databasePath;
        var folder = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }
    }

    public string DatabasePath { get; }

    public SqliteConnection Open()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };

        var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            PRAGMA journal_mode=WAL;
            PRAGMA foreign_keys=ON;
            PRAGMA busy_timeout=5000;
            """;
        cmd.ExecuteNonQuery();
        return connection;
    }
}
