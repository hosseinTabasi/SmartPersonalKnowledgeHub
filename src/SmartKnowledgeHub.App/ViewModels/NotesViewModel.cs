using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartKnowledgeHub.App.Services;
using SmartKnowledgeHub.Core.Models;
using SmartKnowledgeHub.Core.Repositories;
using SmartKnowledgeHub.Core.Search;

namespace SmartKnowledgeHub.App.ViewModels;

public partial class NotesViewModel : ObservableObject
{
    private readonly INotebookRepository _notebooks;
    private readonly INoteRepository _notes;
    private readonly ISearchService _search;
    private readonly IUserPrompt _prompt;

    public NotesViewModel(
        INotebookRepository notebooks,
        INoteRepository notes,
        ISearchService search,
        IUserPrompt prompt)
    {
        _notebooks = notebooks;
        _notes = notes;
        _search = search;
        _prompt = prompt;
    }

    public ObservableCollection<Notebook> Notebooks { get; } = new();
    public ObservableCollection<Note> Notes { get; } = new();

    [ObservableProperty] private Notebook? _selectedNotebook;
    [ObservableProperty] private Note? _selectedNote;
    [ObservableProperty] private string _filterText = string.Empty;
    [ObservableProperty] private bool _showArchived;
    [ObservableProperty] private string _editorTitle = string.Empty;
    [ObservableProperty] private string _editorBody = string.Empty;
    [ObservableProperty] private string _editorTags = string.Empty;
    [ObservableProperty] private Notebook? _editorNotebook;
    [ObservableProperty] private bool _editorPinned;
    [ObservableProperty] private bool _editorArchived;
    [ObservableProperty] private string _newNotebookName = string.Empty;
    [ObservableProperty] private string _statusText = "Select a note or create one.";
    [ObservableProperty] private bool _hasSelection;

    partial void OnSelectedNoteChanged(Note? value)
    {
        HasSelection = value is not null;
        if (value is null)
        {
            return;
        }

        EditorTitle = value.Title;
        EditorBody = value.Body;
        EditorTags = string.Join(", ", value.Tags);
        EditorPinned = value.IsPinned;
        EditorArchived = value.IsArchived;
        EditorNotebook = Notebooks.FirstOrDefault(n => n.Id == value.NotebookId) ?? Notebooks.FirstOrDefault();
    }

    partial void OnSelectedNotebookChanged(Notebook? value) => ReloadNotes();

    partial void OnFilterTextChanged(string value) => ReloadNotes();

    partial void OnShowArchivedChanged(bool value) => ReloadNotes();

    public void Load()
    {
        var selectedId = SelectedNotebook?.Id;
        var noteId = SelectedNote?.Id;
        Notebooks.Clear();
        foreach (var notebook in _notebooks.GetAll())
        {
            Notebooks.Add(notebook);
        }

        if (Notebooks.Count == 0)
        {
            _notebooks.Insert("Inbox");
            foreach (var notebook in _notebooks.GetAll())
            {
                Notebooks.Add(notebook);
            }
        }

        SelectedNotebook = Notebooks.FirstOrDefault(n => n.Id == selectedId);
        ReloadNotes();
        if (noteId is long id)
        {
            SelectedNote = Notes.FirstOrDefault(n => n.Id == id);
        }
    }

    private void ReloadNotes()
    {
        var keep = SelectedNote?.Id;
        Notes.Clear();
        IEnumerable<Note> items = _notes.GetAll(ShowArchived, SelectedNotebook?.Id);
        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            items = items.Where(n =>
                n.Title.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                n.Body.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                n.Tags.Any(t => t.Contains(FilterText, StringComparison.OrdinalIgnoreCase)));
        }

        foreach (var note in items)
        {
            Notes.Add(note);
        }

        if (keep is long id)
        {
            SelectedNote = Notes.FirstOrDefault(n => n.Id == id);
        }
    }

    [RelayCommand]
    private void NewNote()
    {
        var notebook = SelectedNotebook ?? EditorNotebook ?? Notebooks.FirstOrDefault();
        if (notebook is null)
        {
            StatusText = "Create a notebook first.";
            return;
        }

        var note = new Note
        {
            NotebookId = notebook.Id,
            Title = "Untitled note",
            Body = string.Empty,
            Tags = new List<string>()
        };
        _notes.Insert(note);
        _search.UpsertNote(_notes.GetById(note.Id)!);
        Load();
        SelectedNote = Notes.FirstOrDefault(n => n.Id == note.Id);
        StatusText = "Created a new note.";
    }

    [RelayCommand]
    private void SaveNote()
    {
        if (SelectedNote is null)
        {
            StatusText = "Nothing to save.";
            return;
        }

        if (string.IsNullOrWhiteSpace(EditorTitle))
        {
            _prompt.Alert("A title is required.", "Notes");
            return;
        }

        SelectedNote.Title = EditorTitle.Trim();
        SelectedNote.Body = EditorBody ?? string.Empty;
        SelectedNote.IsPinned = EditorPinned;
        SelectedNote.IsArchived = EditorArchived;
        SelectedNote.NotebookId = EditorNotebook?.Id ?? SelectedNote.NotebookId;
        SelectedNote.Tags = SplitTags(EditorTags);
        _notes.Update(SelectedNote);
        var fresh = _notes.GetById(SelectedNote.Id);
        if (fresh is not null)
        {
            _search.UpsertNote(fresh);
        }

        Load();
        SelectedNote = Notes.FirstOrDefault(n => n.Id == SelectedNote.Id) ?? SelectedNote;
        StatusText = "Saved.";
    }

    [RelayCommand]
    private void DeleteNote()
    {
        if (SelectedNote is null)
        {
            return;
        }

        if (!_prompt.Confirm($"Delete note “{SelectedNote.Title}”?", "Notes"))
        {
            return;
        }

        var id = SelectedNote.Id;
        _notes.Delete(id);
        _search.Remove("note", id);
        SelectedNote = null;
        Load();
        StatusText = "Note deleted.";
    }

    [RelayCommand]
    private void TogglePin()
    {
        if (SelectedNote is null)
        {
            return;
        }

        EditorPinned = !EditorPinned;
        SaveNote();
    }

    [RelayCommand]
    private void AddNotebook()
    {
        if (string.IsNullOrWhiteSpace(NewNotebookName))
        {
            return;
        }

        var created = _notebooks.GetOrCreate(NewNotebookName);
        NewNotebookName = string.Empty;
        Load();
        SelectedNotebook = Notebooks.FirstOrDefault(n => n.Id == created.Id);
        StatusText = $"Notebook “{created.Name}” is ready.";
    }

    private static List<string> SplitTags(string raw) =>
        (raw ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
