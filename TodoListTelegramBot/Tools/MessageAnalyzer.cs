using System.Text.RegularExpressions;

public static partial class TextMessageAnalyzer
{
    [GeneratedRegex(@"(?<=^|\s)#(?<value>\S+)")]
    private static partial Regex ProjectRegex();

    [GeneratedRegex(@"(?<=^|\s)@(?<value>\S+)")]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"(?<=^|\s)!(?<value>[1-5])(?=\s|$)")]
    private static partial Regex PriorityRegex();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultiSpaceRegex();

    public static TextMessageAnalysis Analyze(string text)
    {
        var analysis = new TextMessageAnalysis
        {
            ProjectName = Extract(ProjectRegex(), ref text),
            Date = Extract(DateRegex(), ref text),
            Priority = Extract(PriorityRegex(), ref text),
        };

        analysis.Text = MultiSpaceRegex().Replace(text, " ").Trim();
        analysis.IsValid = !string.IsNullOrWhiteSpace(analysis.Text);

        return analysis;
    }

    private static string? Extract(Regex regex, ref string text)
    {
        var match = regex.Match(text);
        if (!match.Success)
            return null;

        text = text.Remove(match.Index, match.Length);
        return match.Groups["value"].Value;
    }
}
