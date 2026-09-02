namespace SmartKnowledgeHub.App.Services;

public interface IUserPrompt
{
    string? OpenFile(string filter);
    bool Confirm(string message, string title);
    void Alert(string message, string title);
    void OpenInOs(string path);
}
