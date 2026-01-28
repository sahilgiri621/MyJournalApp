namespace MyJournalApp.Models;

public class JournalInsights
{
    public int TotalEntries { get; set; }
    public bool HasEntryForToday { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int MissedDays { get; set; }
    public int AvgWordsPerDay { get; set; }
    public List<CountItem> MoodCounts { get; set; } = new();
    public List<CountItem> TagCounts { get; set; } = new();
    public List<WordCountTrend> WordCountTrends { get; set; } = new();
}

public class CountItem
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class WordCountTrend
{
    public DateOnly Date { get; set; }
    public int WordCount { get; set; }
}

