using SmartKnowledgeHub.Core.Models;

namespace SmartKnowledgeHub.Core.Repositories;

public interface INoteRepository
{
    IReadOnlyList<Note> GetAll(bool includeArchived = false, long? notebookId = null);
    IReadOnlyList<Note> GetRecent(int take = 5);
    Note? GetById(long id);
    long Insert(Note note);
    void Update(Note note);
    void Delete(long id);
    void SetPinned(long id, bool isPinned);
    void SetArchived(long id, bool isArchived);
    int Count(bool includeArchived = true);
    int CountPinned();
    int CountArchived();
}
