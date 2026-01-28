using Markdig;

namespace MyJournalApp.Services;

public class MarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .DisableHtml()
            .Build();
    }

    public string ToHtml(string markdown)
    {
        var safeMarkdown = markdown ?? string.Empty;
        return Markdown.ToHtml(safeMarkdown, _pipeline);
    }
}

