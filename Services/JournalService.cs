using System.Globalization;
using System.IO;
using System.Text.Json;
using MyJournalApp.Models;
using Microsoft.Maui.Storage;
using SQLite;

namespace MyJournalApp.Services;

/// <summary>
/// Service for managing journal entries in SQLite local storage.
/// Enforces one entry per entry date and system-generated timestamps.
/// </summary>
public class JournalService
{
    private const string DateFormat = "yyyy-MM-dd";
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private SQLiteAsyncConnection? _connection;
    private bool _initialized;

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        await EnsureInitializedAsync();
        return _connection!;
    }

    public async Task InitializeAsync()
    {
        await EnsureInitializedAsync();
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _initLock.WaitAsync();
        try
        {
            if (_initialized)
            {
                return;
            }

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MyJournalApp.db3");
            _connection = new SQLiteAsyncConnection(dbPath);
            await _connection.CreateTableAsync<JournalEntryRecord>();
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<JournalEntry> CreateEntryAsync(JournalEntry entry)
    {
        var connection = await GetConnectionAsync();
        var entryDateKey = ToDateKey(entry.EntryDate);

        if (await HasEntryForDateAsync(entry.EntryDate))
        {
            throw new InvalidOperationException("A journal entry already exists for this date. Only one entry per day is allowed.");
        }

        var now = DateTime.Now;
        var record = new JournalEntryRecord
        {
            Id = Guid.NewGuid(),
            Title = entry.Title?.Trim() ?? string.Empty,
            Content = entry.Content?.Trim() ?? string.Empty,
            EntryDate = entryDateKey,
            PrimaryMood = entry.PrimaryMood?.Trim() ?? string.Empty,
            SecondaryMoodsJson = SerializeList(NormalizeList(entry.SecondaryMoods, 2)),
            TagsJson = SerializeList(NormalizeList(entry.Tags, null)),
            Category = (entry.Category ?? string.Empty).Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await connection.InsertAsync(record);
        return ToModel(record);
    }

    public async Task<JournalEntry?> GetEntryAsync(Guid id)
    {
        var connection = await GetConnectionAsync();
        var record = await connection.Table<JournalEntryRecord>()
            .Where(e => e.Id == id)
            .FirstOrDefaultAsync();
        return record == null ? null : ToModel(record);
    }

    public async Task<JournalEntry?> GetEntryByDateAsync(DateOnly date)
    {
        var connection = await GetConnectionAsync();
        var dateKey = ToDateKey(date);
        var record = await connection.Table<JournalEntryRecord>()
            .Where(e => e.EntryDate == dateKey)
            .FirstOrDefaultAsync();
        return record == null ? null : ToModel(record);
    }

    public async Task<List<JournalEntry>> GetAllEntriesAsync()
    {
        var connection = await GetConnectionAsync();
        var records = await connection.Table<JournalEntryRecord>()
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync();
        return records.Select(ToModel).ToList();
    }

    public async Task<List<JournalEntry>> GetEntriesInRangeAsync(DateOnly? startDate, DateOnly? endDate)
    {
        var entries = await GetAllEntriesAsync();
        return entries
            .Where(entry =>
            {
                if (startDate.HasValue && entry.EntryDate < startDate.Value)
                {
                    return false;
                }

                if (endDate.HasValue && entry.EntryDate > endDate.Value)
                {
                    return false;
                }

                return true;
            })
            .ToList();
    }

    public async Task<JournalEntry> UpdateEntryAsync(JournalEntry entry)
    {
        var connection = await GetConnectionAsync();
        var record = await connection.Table<JournalEntryRecord>()
            .Where(e => e.Id == entry.Id)
            .FirstOrDefaultAsync();

        if (record == null)
        {
            throw new ArgumentException($"Journal entry with ID {entry.Id} not found.");
        }

        var entryDateKey = ToDateKey(entry.EntryDate);
        if (!string.Equals(record.EntryDate, entryDateKey, StringComparison.Ordinal))
        {
            var conflict = await connection.Table<JournalEntryRecord>()
                .Where(e => e.EntryDate == entryDateKey && e.Id != entry.Id)
                .FirstOrDefaultAsync();
            if (conflict != null)
            {
                throw new InvalidOperationException("A journal entry already exists for this date. Only one entry per day is allowed.");
            }
        }

        record.Title = entry.Title?.Trim() ?? string.Empty;
        record.Content = entry.Content?.Trim() ?? string.Empty;
        record.EntryDate = entryDateKey;
        record.PrimaryMood = entry.PrimaryMood?.Trim() ?? string.Empty;
        record.SecondaryMoodsJson = SerializeList(NormalizeList(entry.SecondaryMoods, 2));
        record.TagsJson = SerializeList(NormalizeList(entry.Tags, null));
        record.Category = (entry.Category ?? string.Empty).Trim();
        record.UpdatedAt = DateTime.Now;

        await connection.UpdateAsync(record);
        return ToModel(record);
    }

    public async Task<bool> DeleteEntryAsync(Guid id)
    {
        var connection = await GetConnectionAsync();
        var rows = await connection.DeleteAsync<JournalEntryRecord>(id);
        return rows > 0;
    }

    public async Task<int> GetTotalCountAsync()
    {
        var connection = await GetConnectionAsync();
        return await connection.Table<JournalEntryRecord>().CountAsync();
    }

    public async Task<bool> HasEntryForDateAsync(DateOnly date)
    {
        var connection = await GetConnectionAsync();
        var dateKey = ToDateKey(date);
        var record = await connection.Table<JournalEntryRecord>()
            .Where(e => e.EntryDate == dateKey)
            .FirstOrDefaultAsync();
        return record != null;
    }

    public async Task<bool> HasEntryForTodayAsync()
    {
        return await HasEntryForDateAsync(DateOnly.FromDateTime(DateTime.Today));
    }

    public async Task<JournalInsights> GetInsightsAsync()
    {
        var entries = await GetAllEntriesAsync();
        return BuildInsights(entries, null, null);
    }

    public async Task<JournalInsights> GetInsightsAsync(DateOnly? startDate, DateOnly? endDate)
    {
        var entries = await GetEntriesInRangeAsync(startDate, endDate);
        return BuildInsights(entries, startDate, endDate);
    }

    private static JournalInsights BuildInsights(List<JournalEntry> entries, DateOnly? startDate, DateOnly? endDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var insights = new JournalInsights
        {
            TotalEntries = entries.Count,
            HasEntryForToday = entries.Any(entry => entry.EntryDate == today)
        };

        var dateSet = entries
            .Select(entry => entry.EntryDate)
            .Distinct()
            .OrderBy(date => date)
            .ToList();

        if (dateSet.Count > 0)
        {
            var first = dateSet.First();
            var last = dateSet.Last();
            var totalDays = last.DayNumber - first.DayNumber + 1;
            insights.MissedDays = Math.Max(0, totalDays - dateSet.Count);
        }

        insights.CurrentStreak = CalculateCurrentStreak(dateSet, today);
        insights.LongestStreak = CalculateLongestStreak(dateSet);

        insights.MoodCounts = entries
            .SelectMany(entry => entry.SecondaryMoods
                .Concat(new[] { entry.PrimaryMood })
                .Where(mood => !string.IsNullOrWhiteSpace(mood)))
            .GroupBy(mood => mood, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CountItem { Label = group.Key, Count = group.Count() })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        insights.TagCounts = entries
            .SelectMany(entry => entry.Tags)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CountItem { Label = group.Key, Count = group.Count() })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        insights.WordCountTrends = entries
            .OrderBy(entry => entry.EntryDate)
            .TakeLast(10)
            .Select(entry => new WordCountTrend
            {
                Date = entry.EntryDate,
                WordCount = CountWords(entry.Content)
            })
            .ToList();

        var totalWords = entries.Sum(entry => CountWords(entry.Content));
        var effectiveStart = startDate ?? (dateSet.Count > 0 ? dateSet.First() : (DateOnly?)null);
        var effectiveEnd = endDate ?? (dateSet.Count > 0 ? dateSet.Last() : (DateOnly?)null);
        var daysForAverage = 0;

        if (effectiveStart.HasValue && effectiveEnd.HasValue)
        {
            daysForAverage = effectiveEnd.Value.DayNumber - effectiveStart.Value.DayNumber + 1;
        }
        else
        {
            daysForAverage = dateSet.Count;
        }

        insights.AvgWordsPerDay = daysForAverage > 0
            ? (int)Math.Round(totalWords / (double)daysForAverage)
            : 0;

        return insights;
    }

    private static int CalculateCurrentStreak(IReadOnlyCollection<DateOnly> dates, DateOnly today)
    {
        if (dates.Count == 0)
        {
            return 0;
        }

        var dateSet = new HashSet<DateOnly>(dates);
        var current = 0;
        var cursor = today;

        while (dateSet.Contains(cursor))
        {
            current++;
            cursor = cursor.AddDays(-1);
        }

        return current;
    }

    private static int CalculateLongestStreak(IReadOnlyList<DateOnly> dates)
    {
        if (dates.Count == 0)
        {
            return 0;
        }

        var longest = 1;
        var current = 1;

        for (var i = 1; i < dates.Count; i++)
        {
            if (dates[i].DayNumber == dates[i - 1].DayNumber + 1)
            {
                current++;
            }
            else
            {
                longest = Math.Max(longest, current);
                current = 1;
            }
        }

        return Math.Max(longest, current);
    }

    private static int CountWords(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 0;
        }

        return content
            .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Length;
    }

    private static string SerializeList(IEnumerable<string>? items)
    {
        var safeItems = items ?? Array.Empty<string>();
        return JsonSerializer.Serialize(safeItems);
    }

    private static List<string> DeserializeList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }

    private static List<string> NormalizeList(IEnumerable<string>? items, int? maxItems)
    {
        var normalized = (items ?? Array.Empty<string>())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (maxItems.HasValue && normalized.Count > maxItems.Value)
        {
            normalized = normalized.Take(maxItems.Value).ToList();
        }

        return normalized;
    }

    private static JournalEntry ToModel(JournalEntryRecord record)
    {
        return new JournalEntry
        {
            Id = record.Id,
            Title = record.Title,
            Content = record.Content,
            EntryDate = FromDateKey(record.EntryDate),
            PrimaryMood = record.PrimaryMood,
            SecondaryMoods = DeserializeList(record.SecondaryMoodsJson),
            Tags = DeserializeList(record.TagsJson),
            Category = record.Category,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }

    private static string ToDateKey(DateOnly date)
    {
        return date.ToString(DateFormat, CultureInfo.InvariantCulture);
    }

    private static DateOnly FromDateKey(string dateKey)
    {
        return DateOnly.ParseExact(dateKey, DateFormat, CultureInfo.InvariantCulture);
    }
}

