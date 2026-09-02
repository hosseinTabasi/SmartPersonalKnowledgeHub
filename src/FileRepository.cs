using Microsoft.Data.Sqlite;
using SmartKnowledgeHub.Core.Data;
using SmartKnowledgeHub.Core.Models;

namespace SmartKnowledgeHub.Core.Repositories;

public sealed class FileRepository : IFileRepository
{
    private readonly SqliteConnectionFactory _factory;

    public FileRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public IReadOnlyList<FileRecord> GetAll()
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, OriginalPath, VaultPath, FileName, Extension, SizeBytes, TagsCsv, CreatedUtc
            FROM Files
            ORDER BY CreatedUtc DESC;
            """;
        var list = new List<FileRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(Map(reader));
        }

        return list;
    }

    public FileRecord? GetById(long id)
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, OriginalPath, VaultPath, FileName, Extension, SizeBytes, TagsCsv, CreatedUtc
            FROM Files
            WHERE Id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public long Insert(FileRecord file, string extractedText)
    {
        using var conn = _factory.Open();
        using var tx = conn.BeginTransaction();
        long id;
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO Files (OriginalPath, VaultPath, FileName, Extension, SizeBytes, TagsCsv, CreatedUtc)
                VALUES ($original, $vault, $name, $ext, $size, $tags, $utc);
                SELECT last_insert_rowid();
                """;
            cmd.Parameters.AddWithValue("$original", file.OriginalPath);
            cmd.Parameters.AddWithValue("$vault", (object?)file.VaultPath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$name", file.FileName);
            cmd.Parameters.AddWithValue("$ext", file.Extension ?? string.Empty);
            cmd.Parameters.AddWithValue("$size", file.SizeBytes);
            cmd.Parameters.AddWithValue("$tags", file.TagsCsv ?? string.Empty);
            cmd.Parameters.AddWithValue("$utc", Utc.Format(file.CreatedUtc == default ? DateTime.UtcNow : file.CreatedUtc));
            id = (long)cmd.ExecuteScalar()!;
        }

        using (var fts = conn.CreateCommand())
        {
            fts.Transaction = tx;
            fts.CommandText = """
                INSERT INTO FilesFts (rowid, FileName, ExtractedText, TagsCsv)
                VALUES ($id, $name, $text, $tags);
                """;
            fts.Parameters.AddWithValue("$id", id);
            fts.Parameters.AddWithValue("$name", file.FileName);
            fts.Parameters.AddWithValue("$text", extractedText ?? string.Empty);
            fts.Parameters.AddWithValue("$tags", file.TagsCsv ?? string.Empty);
            fts.ExecuteNonQuery();
        }

        tx.Commit();
        file.Id = id;
        return id;
    }

    public void UpdateTags(long id, string tagsCsv)
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Files SET TagsCsv = $tags WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$tags", tagsCsv ?? string.Empty);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();

        using var fts = conn.CreateCommand();
        fts.CommandText = """
            INSERT INTO FilesFts(FilesFts, rowid, FileName, ExtractedText, TagsCsv)
            SELECT 'delete', rowid, FileName, ExtractedText, TagsCsv FROM FilesFts WHERE rowid = $id;
            UPDATE FilesFts SET TagsCsv = $tags WHERE rowid = $id;
            """;
        fts.Parameters.AddWithValue("$id", id);
        fts.Parameters.AddWithValue("$tags", tagsCsv ?? string.Empty);
        try
        {
            fts.ExecuteNonQuery();
        }
        catch (SqliteException)
        {
            using var upd = conn.CreateCommand();
            upd.CommandText = "UPDATE FilesFts SET TagsCsv = $tags WHERE rowid = $id;";
            upd.Parameters.AddWithValue("$tags", tagsCsv ?? string.Empty);
            upd.Parameters.AddWithValue("$id", id);
            upd.ExecuteNonQuery();
        }
    }

    public void Delete(long id)
    {
        using var conn = _factory.Open();
        using var tx = conn.BeginTransaction();
        using (var fts = conn.CreateCommand())
        {
            fts.Transaction = tx;
            fts.CommandText = "DELETE FROM FilesFts WHERE rowid = $id;";
            fts.Parameters.AddWithValue("$id", id);
            fts.ExecuteNonQuery();
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM Files WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public int Count()
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Files;";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static FileRecord Map(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        OriginalPath = reader.GetString(1),
        VaultPath = reader.IsDBNull(2) ? null : reader.GetString(2),
        FileName = reader.GetString(3),
        Extension = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
        SizeBytes = reader.GetInt64(5),
        TagsCsv = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
        CreatedUtc = Utc.Parse(reader.GetString(7))
    };
}
