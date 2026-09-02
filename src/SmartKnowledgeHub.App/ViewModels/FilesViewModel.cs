using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartKnowledgeHub.App.Services;
using SmartKnowledgeHub.Core.Models;
using SmartKnowledgeHub.Core.Repositories;
using SmartKnowledgeHub.Core.Services;

namespace SmartKnowledgeHub.App.ViewModels;

public partial class FilesViewModel : ObservableObject
{
    private readonly IFileRepository _files;
    private readonly FileVaultService _vault;
    private readonly IUserPrompt _prompt;

    public FilesViewModel(IFileRepository files, FileVaultService vault, IUserPrompt prompt)
    {
        _files = files;
        _vault = vault;
        _prompt = prompt;
    }

    public ObservableCollection<FileRecord> Files { get; } = new();

    [ObservableProperty] private FileRecord? _selectedFile;
    [ObservableProperty] private bool _copyIntoVault = true;
    [ObservableProperty] private string _tagsCsv = string.Empty;
    [ObservableProperty] private string _statusText = "Register a local file. Text, Markdown and CSV can be indexed.";
    [ObservableProperty] private bool _hasSelection;

    partial void OnSelectedFileChanged(FileRecord? value)
    {
        HasSelection = value is not null;
        TagsCsv = value?.TagsCsv ?? string.Empty;
    }

    public void Load()
    {
        var keep = SelectedFile?.Id;
        Files.Clear();
        foreach (var file in _files.GetAll())
        {
            Files.Add(file);
        }

        if (keep is long id)
        {
            SelectedFile = Files.FirstOrDefault(f => f.Id == id);
        }
    }

    [RelayCommand]
    private void RegisterFile()
    {
        var path = _prompt.OpenFile("All files|*.*|Text|*.txt;*.md;*.csv");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var record = _vault.Register(path, TagsCsv, CopyIntoVault);
            Load();
            SelectedFile = Files.FirstOrDefault(f => f.Id == record.Id);
            StatusText = CopyIntoVault
                ? "Registered and copied into the local vault."
                : "Registered the original path (no vault copy).";
        }
        catch (Exception ex)
        {
            _prompt.Alert(ex.Message, "Files");
        }
    }

    [RelayCommand]
    private void SaveTags()
    {
        if (SelectedFile is null)
        {
            return;
        }

        _files.UpdateTags(SelectedFile.Id, TagsCsv ?? string.Empty);
        Load();
        StatusText = "Tags updated.";
    }

    [RelayCommand]
    private void OpenFile()
    {
        if (SelectedFile is null)
        {
            return;
        }

        _prompt.OpenInOs(SelectedFile.EffectivePath);
    }

    [RelayCommand]
    private void DeleteFile()
    {
        if (SelectedFile is null)
        {
            return;
        }

        var alsoVault = !string.IsNullOrWhiteSpace(SelectedFile.VaultPath)
            && _prompt.Confirm("Also delete the vault copy if it exists?", "Files");
        _vault.Delete(SelectedFile, alsoVault);
        SelectedFile = null;
        Load();
        StatusText = "File record removed.";
    }
}
