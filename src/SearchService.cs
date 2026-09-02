using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using SmartKnowledgeHub.Core.Data;
using SmartKnowledgeHub.Core.Embedding;
using SmartKnowledgeHub.Core.Models;
using SmartKnowledgeHub.Core.Repositories;

namespace SmartKnowledgeHub.Core.Search;

public sealed class SearchService : ISearchService
{
    private static readonly Regex UnsafeFts = new(@"[^\p{L}\p{N}\s]+", RegexOptions.Compiled);

    private readonly SqliteConnectionFactory _factory;
    private readonly IEmbeddingService _embedding;
    private readonly INoteRepository _notes;
    private readonly ITaskRepository _tasks;
    private readonly IFileRepository _files;
    private readonly OnnxEmbeddingService _onnxProbe;

    public SearchService(
        SqliteConnectionFactory factory,
        IEmbeddingService embedding,
        INoteRepository notes,
        ITaskRepository tasks,
        IFileRepository files,
        string? onnxModelPath = null)
    {
        _factory = factory;
        _embedding = embedding;
        _notes = notes;
        _tasks = tasks;
        _files = files;
        _onnxProbe = new OnnxEmbeddingService(onnxModelPath ?? string.Empty);
    }

    public string EmbeddingName => _embedding.Name;
    public bool OptionalOnnxAvailable => _onnxProbe.IsAvailable;

    public IReadOnlyList<SearchHit> KeywordSearch(string query, int take = 30)
    {
        var match = ToMatchQuery(query);
        if (string.IsNullOrWhiteSpace(match))
        {
            return Array.Empty<SearchHit>();
        }

        var hits = new List<SearchHit>();
        using var conn = _factory.Open();
        hits.AddRange(QueryNotesFts(conn, match, take));
        hits.AddRange(QueryTasksFts(conn, match, take));
        hits.AddRange(QueryFilesFts(conn, match, take));
        return hits
            .OrderBy(h => h.Score)
            .ThenBy(h => h.Title, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .ToList();
    }

    public IReadOnlyList<SearchHit> SemanticSearch(string query, int take = 30)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<SearchHit>();
        }

        EnsureCorpusLoaded();
        var hashed = _embedding as HashedTfidfEmbeddingService;
        hashed?.ResetCorpus();
        var rows = LoadIndexRows();
        if (hashed is not null)
        {
            foreach (var row in rows)
            {
                hashed.AddDocumentToCorpus(row.Text);
            }
        }

        var queryVector = _embedding.Embed(query);
        var hits = new List<SearchHit>();
        foreach (var row in rows)
        {
            float[] docVector;
            if (row.Blob is { Length: > 0 })
            {
                docVector = HashedTfidfEmbeddingService.FromBlob(row.Blob, _embedding.Dimensions);
            }
            else
            {
                docVector = _embedding.Embed(row.Text);
            }

            var score = HashedTfidfEmbeddingService.Cosine(queryVector, docVector);
            if (score <= 0)
            {
                continue;
            }

            hits.Add(new SearchHit
            {
                EntityType = row.EntityType,
                EntityId = row.EntityId,
                Title = row.Title,
                Snippet = Snippet(row.Text, 180),
                Score = score,
                Source = "Related meaning (TF-IDF cosine)"
            });
        }

        return hits
            .OrderByDescending(h => h.Score)
            .Take(take)
            .ToList();
    }

    public void UpsertNote(Note note)
    {
        var tags = note.Tags.Count == 0 ? string.Empty : string.Join(' ', note.Tags);
        var text = $"{note.Title}\n{note.Body}\n{tags}\n{note.NotebookName}";
        Upsert("note", note.Id, text, note.Title);
    }

    public void UpsertTask(TaskItem task)
    {
        var text = $"{task.Title}\n{task.Body}\n{task.Status}";
        Upsert("task", task.Id, text, task.Title);
    }

    public void UpsertFile(FileRecord file, string extractedText)
    {
        var text = $"{file.FileName}\n{file.TagsCsv}\n{extractedText}";
        Upsert("file", file.Id, text, file.FileName);
    }

    public void Remove(string entityType, long entityId)
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM SearchIndex WHERE EntityType = $type AND EntityId = $id;";
        cmd.Parameters.AddWithValue("$type", entityType);
        cmd.Parameters.AddWithValue("$id", entityId);
        cmd.ExecuteNonQuery();
    }

    public void RebuildAll()
    {
        using (var conn = _factory.Open())
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM SearchIndex;";
            cmd.ExecuteNonQuery();
        }

        if (_embedding is HashedTfidfEmbeddingService hashed)
        {
            hashed.ResetCorpus();
            foreach (var note in _notes.GetAll(includeArchived: true))
            {
                hashed.AddDocumentToCorpus($"{note.Title}\n{note.Body}");
            }

            foreach (var task in _tasks.GetAll())
            {
                hashed.AddDocumentToCorpus($"{task.Title}\n{task.Body}");
            }

            foreach (var file in _files.GetAll())
            {
                var extracted = TextExtractor.Extract(file.EffectivePath);
                hashed.AddDocumentToCorpus($"{file.FileName}\n{extracted}");
            }
        }

        foreach (var note in _notes.GetAll(includeArchived: true))
        {
            UpsertNote(note);
        }

        foreach (var task in _tasks.GetAll())
        {
            UpsertTask(task);
        }

        foreach (var file in _files.GetAll())
        {
            var extracted = TextExtractor.Extract(file.EffectivePath);
            UpsertFile(file, extracted);
        }
    }

    private void Upsert(string entityType, long entityId, string text, string title)
    {
        var embedding = _embedding.Embed($"{title}\n{text}");
        var blob = HashedTfidfEmbeddingService.ToBlob(embedding);
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO SearchIndex (EntityType, EntityId, Text, EmbeddingBlob)
            VALUES ($type, $id, $text, $blob)
            ON CONFLICT(EntityType, EntityId)
            DO UPDATE SET Text = excluded.Text, EmbeddingBlob = excluded.EmbeddingBlob;
            """;
        cmd.Parameters.AddWithValue("$type", entityType);
        cmd.Parameters.AddWithValue("$id", entityId);
        cmd.Parameters.AddWithValue("$text", text);
        cmd.Parameters.AddWithValue("$blob", blob);
        cmd.ExecuteNonQuery();
    }

    private void EnsureCorpusLoaded()
    {
        if (_embedding is not HashedTfidfEmbeddingService)
        {
            return;
        }
    }

    private List<IndexRow> LoadIndexRows()
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT EntityType, EntityId, Text, EmbeddingBlob FROM SearchIndex;";
        var rows = new List<IndexRow>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(new IndexRow
            {
                EntityType = reader.GetString(0),
                EntityId = reader.GetInt64(1),
                Text = reader.GetString(2),
                Blob = reader.IsDBNull(3) ? null : (byte[])reader.GetValue(3),
                Title = FirstLine(reader.GetString(2))
            });
        }

        return rows;
    }

    private static List<SearchHit> QueryNotesFts(SqliteConnection conn, string match, int take)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT n.Id, n.Title, snippet(NotesFts, 1, '', '', '…', 12), bm25(NotesFts)
            FROM NotesFts
            JOIN Notes n ON n.Id = NotesFts.rowid
            WHERE NotesFts MATCH $q
            ORDER BY bm25(NotesFts)
            LIMIT $take;
            """;
        cmd.Parameters.AddWithValue("$q", match);
        cmd.Parameters.AddWithValue("$take", take);
        return ReadHits(cmd, "note", "FTS5 BM25");
    }

    private static List<SearchHit> QueryTasksFts(SqliteConnection conn, string match, int take)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT t.Id, t.Title, snippet(TasksFts, 1, '', '', '…', 12), bm25(TasksFts)
            FROM TasksFts
            JOIN Tasks t ON t.Id = TasksFts.rowid
            WHERE TasksFts MATCH $q
            ORDER BY bm25(TasksFts)
            LIMIT $take;
            """;
        cmd.Parameters.AddWithValue("$q", match);
        cmd.Parameters.AddWithValue("$take", take);
        return ReadHits(cmd, "task", "FTS5 BM25");
    }

    private static List<SearchHit> QueryFilesFts(SqliteConnection conn, string match, int take)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT f.Id, f.FileName, snippet(FilesFts, 1, '', '', '…', 12), bm25(FilesFts)
            FROM FilesFts
            JOIN Files f ON f.Id = FilesFts.rowid
            WHERE FilesFts MATCH $q
            ORDER BY bm25(FilesFts)
            LIMIT $take;
            """;
        cmd.Parameters.AddWithValue("$q", match);
        cmd.Parameters.AddWithValue("$take", take);
        return ReadHits(cmd, "file", "FTS5 BM25");
    }

    private static List<SearchHit> ReadHits(SqliteCommand cmd, string type, string source)
    {
        var list = new List<SearchHit>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new SearchHit
            {
                EntityType = type,
                EntityId = reader.GetInt64(0),
                Title = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Snippet = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Score = reader.IsDBNull(3) ? 0 : reader.GetDouble(3),
                Source = source
            });
        }

        return list;
    }

    public static string ToMatchQuery(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var cleaned = UnsafeFts.Replace(raw, " ");
        var tokens = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var token in tokens)
        {
            if (builder.Length > 0)
            {
                builder.Append(" OR ");
            }

            builder.Append('"').Append(token.Replace("\"", string.Empty)).Append('"');
        }

        return builder.ToString();
    }

    private static string FirstLine(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var idx = text.IndexOf('\n');
        return idx < 0 ? text.Trim() : text[..idx].Trim();
    }

    private static string Snippet(string text, int max)
    {
        var flat = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return flat.Length <= max ? flat : flat[..max] + "…";
    }

    private sealed class IndexRow
    {
        public string EntityType { get; set; } = string.Empty;
        public long EntityId { get; set; }
        public string Text { get; set; } = string.Empty;
        public byte[]? Blob { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}
