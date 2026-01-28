using System.ComponentModel.DataAnnotations;

namespace MyJournalApp.Models;

public class JournalEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "Entry date is required")]
    public DateOnly EntryDate { get; set; }

    // ✅ NEW (Mood Tracking)
    [Required(ErrorMessage = "Primary mood is required")]
    public string PrimaryMood { get; set; } = string.Empty;

    // Up to two (we enforce in UI)
    public List<string> SecondaryMoods { get; set; } = new();

    // Tagging
    public List<string> Tags { get; set; } = new();

    // Category (user-defined or suggested)
    public string Category { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

