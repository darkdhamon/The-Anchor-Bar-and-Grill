using System.Text;

namespace Anchor.Web.Components.Site;

public static class HomepagePublicityText
{
    private const int DefaultPreviewLength = 180;

    public static IReadOnlyList<string> GetParagraphs(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var paragraphs = new List<string>();
        var currentParagraph = new StringBuilder();
        var normalizedValue = value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

        foreach (var rawLine in normalizedValue.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                FlushCurrentParagraph();
                continue;
            }

            if (currentParagraph.Length > 0)
            {
                currentParagraph.Append(' ');
            }

            currentParagraph.Append(line);
        }

        FlushCurrentParagraph();
        return paragraphs;

        void FlushCurrentParagraph()
        {
            if (currentParagraph.Length == 0)
            {
                return;
            }

            paragraphs.Add(currentParagraph.ToString());
            currentParagraph.Clear();
        }
    }

    public static string CreatePreview(string value, int maxLength = DefaultPreviewLength)
    {
        var flattenedText = string.Join(" ", GetParagraphs(value));
        if (flattenedText.Length <= maxLength)
        {
            return flattenedText;
        }

        var previewLength = Math.Max(0, maxLength - 3);
        return $"{flattenedText[..previewLength].TrimEnd()}...";
    }
}
