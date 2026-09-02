using SmartKnowledgeHub.Core.Models;
using SmartKnowledgeHub.Core.Search;

namespace SmartKnowledgeHub.Tests;

public sealed class SearchServiceTests
{
    [Fact]
    public void KeywordSearch_FindsNoteByBodyTerm()
    {
        using var hub = new TempHub();
        var notebookId = hub.Notebooks.Insert("Study");
        var id = hub.Notes.Insert(new Note
        {
            NotebookId = notebookId,
            Title = "Database lab",
            Body = "SQLite FTS5 virtual tables and porter stemming.",
            Tags = new List<string> { "sqlite" }
        });
        var note = hub.Notes.GetById(id)!;
        hub.Search.UpsertNote(note);

        var hits = hub.Search.KeywordSearch("fts5");
        Assert.Contains(hits, h => h.EntityType == "note" && h.EntityId == id);
        Assert.All(hits.Where(h => h.EntityType == "note"), h => Assert.Equal("FTS5 BM25", h.Source));
    }

    [Fact]
    public void KeywordSearch_EmptyQuery_ReturnsNone()
    {
        using var hub = new TempHub();
        Assert.Empty(hub.Search.KeywordSearch("   "));
        Assert.Empty(hub.Search.KeywordSearch("!!!"));
    }

    [Fact]
    public void SemanticSearch_RanksRelatedNotesAboveUnrelated()
    {
        using var hub = new TempHub();
        var notebookId = hub.Notebooks.Insert("Mixed");
        var sqliteId = hub.Notes.Insert(new Note
        {
            NotebookId = notebookId,
            Title = "Full text search in SQLite",
            Body = "FTS5 ranking with bm25 over notes and document tokens."
        });
        var cookingId = hub.Notes.Insert(new Note
        {
            NotebookId = notebookId,
            Title = "Masala tea recipe",
            Body = "Boil water, add tea leaves, ginger, cardamom and milk."
        });
        hub.Search.RebuildAll();

        var hits = hub.Search.SemanticSearch("database full text search ranking");
        Assert.NotEmpty(hits);
        var sqliteHit = hits.Single(h => h.EntityId == sqliteId && h.EntityType == "note");
        var cookingHit = hits.FirstOrDefault(h => h.EntityId == cookingId && h.EntityType == "note");
        Assert.True(cookingHit is null || sqliteHit.Score > cookingHit.Score);
    }

    [Fact]
    public void ToMatchQuery_QuotesTokens()
    {
        var q = SearchService.ToMatchQuery("hello world!!");
        Assert.Contains("\"hello\"", q);
        Assert.Contains("OR", q);
        Assert.Contains("\"world\"", q);
    }
}
