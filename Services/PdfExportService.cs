using System.IO;
using MyJournalApp.Models;
using Microsoft.Maui.Storage;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using PdfContainer = QuestPDF.Infrastructure.IContainer;

namespace MyJournalApp.Services;

public class PdfExportService
{
    public PdfExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<string> ExportEntriesAsync(IEnumerable<JournalEntry> entries, DateOnly startDate, DateOnly endDate)
    {
        var entryList = entries.OrderBy(entry => entry.EntryDate).ToList();
        var folder = Path.Combine(FileSystem.AppDataDirectory, "exports");
        Directory.CreateDirectory(folder);

        var fileName = $"MyJournalApp_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf";
        var path = Path.Combine(folder, fileName);

        await Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(36);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(text => text.FontSize(11));

                    page.Header()
                        .Text($"My Journal App entries {startDate:MMMM dd, yyyy} - {endDate:MMMM dd, yyyy}")
                        .SemiBold()
                        .FontSize(16);

                    page.Content().Column(column =>
                    {
                        foreach (var entry in entryList)
                        {
                            column.Item().Element(container => BuildEntry(container, entry));
                            column.Item().PaddingVertical(8).LineHorizontal(1);
                        }
                    });
                });
            }).GeneratePdf(path);
        });

        return path;
    }

    private static void BuildEntry(PdfContainer container, JournalEntry entry)
    {
        container.Column(column =>
        {
            column.Item().Text($"{entry.EntryDate:MMMM dd, yyyy} - {entry.Title}")
                .Bold()
                .FontSize(13);

            if (!string.IsNullOrWhiteSpace(entry.Category))
            {
                column.Item().Text($"Category: {entry.Category}");
            }

            var moodLine = string.IsNullOrWhiteSpace(entry.PrimaryMood)
                ? string.Empty
                : $"Mood: {entry.PrimaryMood}";

            if (entry.SecondaryMoods.Count > 0)
            {
                moodLine = $"{moodLine} (Secondary: {string.Join(", ", entry.SecondaryMoods)})";
            }

            if (!string.IsNullOrWhiteSpace(moodLine))
            {
                column.Item().Text(moodLine);
            }

            if (entry.Tags.Count > 0)
            {
                column.Item().Text($"Tags: {string.Join(", ", entry.Tags)}");
            }

            if (!string.IsNullOrWhiteSpace(entry.Content))
            {
                column.Item().PaddingTop(4).Text(entry.Content);
            }
        });
    }
}


