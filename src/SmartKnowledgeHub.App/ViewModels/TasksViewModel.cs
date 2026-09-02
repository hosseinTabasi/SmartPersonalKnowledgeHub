using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartKnowledgeHub.App.Services;
using SmartKnowledgeHub.Core.Models;
using SmartKnowledgeHub.Core.Repositories;
using SmartKnowledgeHub.Core.Search;

namespace SmartKnowledgeHub.App.ViewModels;

public partial class TasksViewModel : ObservableObject
{
    private readonly ITaskRepository _tasks;
    private readonly INoteRepository _notes;
    private readonly ISearchService _search;
    private readonly IUserPrompt _prompt;

    public TasksViewModel(ITaskRepository tasks, INoteRepository notes, ISearchService search, IUserPrompt prompt)
    {
        _tasks = tasks;
        _notes = notes;
        _search = search;
        _prompt = prompt;
        StatusOptions = TaskStatuses.All;
        PriorityOptions = new[]
        {
            new PriorityOption(TaskPriorities.High, "High"),
            new PriorityOption(TaskPriorities.Normal, "Normal"),
            new PriorityOption(TaskPriorities.Low, "Low")
        };
    }

    public string[] StatusOptions { get; }
    public PriorityOption[] PriorityOptions { get; }
    public ObservableCollection<TaskItem> Tasks { get; } = new();
    public ObservableCollection<NoteChoice> NoteChoices { get; } = new();

    [ObservableProperty] private string _statusFilter = string.Empty;
    [ObservableProperty] private TaskItem? _selectedTask;
    [ObservableProperty] private string _editorTitle = string.Empty;
    [ObservableProperty] private string _editorBody = string.Empty;
    [ObservableProperty] private DateTime? _editorDue;
    [ObservableProperty] private int _editorPriority = TaskPriorities.Normal;
    [ObservableProperty] private string _editorStatus = TaskStatuses.Todo;
    [ObservableProperty] private NoteChoice? _editorLinkedNote;
    [ObservableProperty] private string _statusText = "Select a task or create one.";
    [ObservableProperty] private bool _hasSelection;

    public record PriorityOption(int Value, string Label);
    public record NoteChoice(long? Id, string Title);

    partial void OnStatusFilterChanged(string value) => Reload();

    partial void OnSelectedTaskChanged(TaskItem? value)
    {
        HasSelection = value is not null;
        if (value is null)
        {
            return;
        }

        EditorTitle = value.Title;
        EditorBody = value.Body;
        EditorDue = value.DueUtc?.ToLocalTime();
        EditorPriority = value.Priority;
        EditorStatus = value.Status;
        EditorLinkedNote = NoteChoices.FirstOrDefault(n => n.Id == value.NoteId) ?? NoteChoices.FirstOrDefault();
    }

    public void Load()
    {
        var keep = SelectedTask?.Id;
        NoteChoices.Clear();
        NoteChoices.Add(new NoteChoice(null, "(no linked note)"));
        foreach (var note in _notes.GetAll(includeArchived: true))
        {
            NoteChoices.Add(new NoteChoice(note.Id, note.Title));
        }

        Reload();
        if (keep is long id)
        {
            SelectedTask = Tasks.FirstOrDefault(t => t.Id == id);
        }
    }

    private void Reload()
    {
        var keep = SelectedTask?.Id;
        Tasks.Clear();
        var filter = string.IsNullOrWhiteSpace(StatusFilter) ? null : StatusFilter;
        foreach (var task in _tasks.GetAll(filter))
        {
            Tasks.Add(task);
        }

        if (keep is long id)
        {
            SelectedTask = Tasks.FirstOrDefault(t => t.Id == id);
        }
    }

    [RelayCommand]
    private void FilterAll() => StatusFilter = string.Empty;

    [RelayCommand]
    private void FilterStatus(string? status) => StatusFilter = status ?? string.Empty;

    [RelayCommand]
    private void NewTask()
    {
        var task = new TaskItem { Title = "New task", Status = TaskStatuses.Todo, Priority = TaskPriorities.Normal };
        _tasks.Insert(task);
        _search.UpsertTask(task);
        Load();
        SelectedTask = Tasks.FirstOrDefault(t => t.Id == task.Id);
        StatusText = "Created a new task.";
    }

    [RelayCommand]
    private void SaveTask()
    {
        if (SelectedTask is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(EditorTitle))
        {
            _prompt.Alert("A title is required.", "Tasks");
            return;
        }

        SelectedTask.Title = EditorTitle.Trim();
        SelectedTask.Body = EditorBody ?? string.Empty;
        SelectedTask.Priority = EditorPriority;
        SelectedTask.Status = EditorStatus;
        SelectedTask.NoteId = EditorLinkedNote?.Id;
        SelectedTask.DueUtc = EditorDue?.ToUniversalTime();
        _tasks.Update(SelectedTask);
        var fresh = _tasks.GetById(SelectedTask.Id);
        if (fresh is not null)
        {
            _search.UpsertTask(fresh);
        }

        Load();
        StatusText = "Saved.";
    }

    [RelayCommand]
    private void DeleteTask()
    {
        if (SelectedTask is null)
        {
            return;
        }

        if (!_prompt.Confirm($"Delete task “{SelectedTask.Title}”?", "Tasks"))
        {
            return;
        }

        var id = SelectedTask.Id;
        _tasks.Delete(id);
        _search.Remove("task", id);
        SelectedTask = null;
        Load();
        StatusText = "Task deleted.";
    }

    [RelayCommand]
    private void MarkDone()
    {
        if (SelectedTask is null)
        {
            return;
        }

        EditorStatus = TaskStatuses.Done;
        SaveTask();
    }
}
