namespace SmartKnowledgeHub.Core.Models;

public static class TaskStatuses
{
    public const string Todo = "Todo";
    public const string Doing = "Doing";
    public const string Done = "Done";

    public static readonly string[] All = { Todo, Doing, Done };
}

public static class TaskPriorities
{
    public const int Low = 0;
    public const int Normal = 1;
    public const int High = 2;

    public static string ToLabel(int value) => value switch
    {
        Low => "Low",
        High => "High",
        _ => "Normal"
    };
}

public sealed class TaskItem
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime? DueUtc { get; set; }
    public int Priority { get; set; } = TaskPriorities.Normal;
    public string Status { get; set; } = TaskStatuses.Todo;
    public long? NoteId { get; set; }
    public string? LinkedNoteTitle { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
