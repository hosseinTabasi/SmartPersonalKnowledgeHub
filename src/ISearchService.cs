using SmartKnowledgeHub.Core.Models;

namespace SmartKnowledgeHub.Core.Search;

public interface ISearchService
{
    IReadOnlyList<SearchHit> KeywordSearch(string query, int take = 30);
    IReadOnlyList<SearchHit> SemanticSearch(string query, int take = 30);
    void UpsertNote(Note note);
    void UpsertTask(TaskItem task);
    void UpsertFile(FileRecord file, string extractedText);
    void Remove(string entityType, long entityId);
    void RebuildAll();
    string EmbeddingName { get; }
    bool OptionalOnnxAvailable { get; }
}
