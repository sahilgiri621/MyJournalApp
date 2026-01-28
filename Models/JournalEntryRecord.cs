using SQLite;

namespace MyJournalApp.Models;

public class JournalEntryRecord
{
    [PrimaryKey]
    public Guid Id { get; set; }

    [Indexed(Unique = true)]
    public string EntryDate { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string PrimaryMood { get; set; } = string.Empty;
    public string SecondaryMoodsJson { get; set; } = "[]";
    public string TagsJson { get; set; } = "[]";
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

