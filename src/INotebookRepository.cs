using SmartKnowledgeHub.Core.Models;

namespace SmartKnowledgeHub.Core.Repositories;

public interface INotebookRepository
{
    IReadOnlyList<Notebook> GetAll();
    Notebook? GetById(long id);
    Notebook GetOrCreate(string name);
    long Insert(string name);
    void Rename(long id, string name);
    void Delete(long id);
}
