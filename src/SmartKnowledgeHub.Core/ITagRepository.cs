using SmartKnowledgeHub.Core.Models;

namespace SmartKnowledgeHub.Core.Repositories;

public interface ITagRepository
{
    IReadOnlyList<Tag> GetAll();
    IReadOnlyList<string> GetNamesForNote(long noteId);
    void ReplaceNoteTags(long noteId, IEnumerable<string> tagNames);
}
