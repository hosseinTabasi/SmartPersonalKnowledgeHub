using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartKnowledgeHub.Core.Models;
using SmartKnowledgeHub.Core.Search;

namespace SmartKnowledgeHub.App.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly ISearchService _search;

    public SearchViewModel(ISearchService search)
    {
        _search = search;
        EmbeddingName = search.EmbeddingName;
        OptionalOnnxAvailable = search.OptionalOnnxAvailable;
    }

    public ObservableCollection<SearchHit> Results { get; } = new();

    [ObservableProperty] private string _query = string.Empty;
    [ObservableProperty] private bool _useRelatedMeaning;
    [ObservableProperty] private string _statusText = "Search notes, tasks and extracted file text.";
    [ObservableProperty] private string _embeddingName = string.Empty;
    [ObservableProperty] private bool _optionalOnnxAvailable;
    [ObservableProperty] private SearchHit? _selectedHit;

    public string ModeLabel => UseRelatedMeaning ? "Related meaning" : "Keywords (FTS5 / bm25)";

    partial void OnUseRelatedMeaningChanged(bool value)
    {
        OnPropertyChanged(nameof(ModeLabel));
        if (!string.IsNullOrWhiteSpace(Query))
        {
            RunSearch();
        }
    }

    [RelayCommand]
    private void RunSearch()
    {
        Results.Clear();
        IReadOnlyList<SearchHit> hits = UseRelatedMeaning
            ? _search.SemanticSearch(Query)
            : _search.KeywordSearch(Query);

        foreach (var hit in hits)
        {
            Results.Add(hit);
        }

        StatusText = Results.Count == 0
            ? "No matches."
            : $"{Results.Count} result(s) · {ModeLabel} · {_search.EmbeddingName}";
    }

    [RelayCommand]
    private void RebuildIndex()
    {
        _search.RebuildAll();
        StatusText = "Search index rebuilt from notes, tasks and files.";
        if (!string.IsNullOrWhiteSpace(Query))
        {
            RunSearch();
        }
    }
}
