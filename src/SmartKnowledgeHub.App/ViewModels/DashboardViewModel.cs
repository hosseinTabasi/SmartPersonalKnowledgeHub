using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartKnowledgeHub.Core.Models;
using SmartKnowledgeHub.Core.Services;

namespace SmartKnowledgeHub.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly DashboardService _dashboard;

    public DashboardViewModel(DashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [ObservableProperty] private int _noteCount;
    [ObservableProperty] private int _pinnedNoteCount;
    [ObservableProperty] private int _taskTodoCount;
    [ObservableProperty] private int _taskDoingCount;
    [ObservableProperty] private int _taskDoneCount;
    [ObservableProperty] private int _fileCount;
    [ObservableProperty] private int _dueSoonCount;
    [ObservableProperty] private string _statusText = "Ready.";

    public ObservableCollection<Note> RecentNotes { get; } = new();
    public ObservableCollection<TaskItem> DueTasks { get; } = new();

    [RelayCommand]
    public void Refresh()
    {
        var summary = _dashboard.GetSummary();
        NoteCount = summary.NoteCount;
        PinnedNoteCount = summary.PinnedNoteCount;
        TaskTodoCount = summary.TaskTodoCount;
        TaskDoingCount = summary.TaskDoingCount;
        TaskDoneCount = summary.TaskDoneCount;
        FileCount = summary.FileCount;
        DueSoonCount = summary.DueSoonCount;
        RecentNotes.Clear();
        foreach (var note in summary.RecentNotes)
        {
            RecentNotes.Add(note);
        }

        DueTasks.Clear();
        foreach (var task in summary.DueTasks)
        {
            DueTasks.Add(task);
        }

        StatusText = $"Updated {DateTime.Now:t}. Offline SQLite hub.";
    }
}
