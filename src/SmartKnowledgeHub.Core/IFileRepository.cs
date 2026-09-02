using SmartKnowledgeHub.Core.Models;

namespace SmartKnowledgeHub.Core.Repositories;

public interface IFileRepository
{
    IReadOnlyList<FileRecord> GetAll();
    FileRecord? GetById(long id);
    long Insert(FileRecord file, string extractedText);
    void UpdateTags(long id, string tagsCsv);
    void Delete(long id);
    int Count();
}
