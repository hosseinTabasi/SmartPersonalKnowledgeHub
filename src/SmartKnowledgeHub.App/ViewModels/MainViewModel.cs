using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SmartKnowledgeHub.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel(
        DashboardViewModel dashboard,
        NotesViewModel notes,
        TasksViewModel tasks,
        FilesViewModel files,
        SearchViewModel search,
        SettingsViewModel settings)
    {
        Dashboard = dashboard;
        Notes = notes;
        Tasks = tasks;
        Files = files;
        Search = search;
        Settings = settings;
        CurrentViewModel = dashboard;
        dashboard.Refresh();
    }

    public DashboardViewModel Dashboard { get; }
    public NotesViewModel Notes { get; }
    public TasksViewModel Tasks { get; }
    public FilesViewModel Files { get; }
    public SearchViewModel Search { get; }
    public SettingsViewModel Settings { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDashboard))]
    [NotifyPropertyChangedFor(nameof(IsNotes))]
    [NotifyPropertyChangedFor(nameof(IsTasks))]
    [NotifyPropertyChangedFor(nameof(IsFiles))]
    [NotifyPropertyChangedFor(nameof(IsSearch))]
    [NotifyPropertyChangedFor(nameof(IsSettings))]
    private string _selectedNav = "Dashboard";

    [ObservableProperty]
    private object _currentViewModel;

    public bool IsDashboard => SelectedNav == "Dashboard";
    public bool IsNotes => SelectedNav == "Notes";
    public bool IsTasks => SelectedNav == "Tasks";
    public bool IsFiles => SelectedNav == "Files";
    public bool IsSearch => SelectedNav == "Search";
    public bool IsSettings => SelectedNav == "Settings";

    [RelayCommand]
    private void Navigate(string? page)
    {
        var key = string.IsNullOrWhiteSpace(page) ? "Dashboard" : page;
        SelectedNav = key;
        CurrentViewModel = key switch
        {
            "Notes" => Notes,
            "Tasks" => Tasks,
            "Files" => Files,
            "Search" => Search,
            "Settings" => Settings,
            _ => Dashboard
        };

        switch (CurrentViewModel)
        {
            case DashboardViewModel d:
                d.Refresh();
                break;
            case NotesViewModel n:
                n.Load();
                break;
            case TasksViewModel t:
                t.Load();
                break;
            case FilesViewModel f:
                f.Load();
                break;
            case SettingsViewModel s:
                s.Refresh();
                break;
        }
    }
}
