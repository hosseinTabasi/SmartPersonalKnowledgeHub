using SmartKnowledgeHub.Core.Models;

namespace SmartKnowledgeHub.Core.Repositories;

public interface ITaskRepository
{
    IReadOnlyList<TaskItem> GetAll(string? status = null);
    IReadOnlyList<TaskItem> GetDueSoon(int days = 7);
    TaskItem? GetById(long id);
    long Insert(TaskItem task);
    void Update(TaskItem task);
    void Delete(long id);
    int CountByStatus(string status);
}
