using System.Globalization;
using System.Text.RegularExpressions;

public static partial class DueDateParser
{
    private static readonly TimeOnly DefaultTime = new(12, 0);

    private static readonly Dictionary<string, int> DayOffsets = new()
    {
        ["dzis"] = 0,
        ["dziś"] = 0,
        ["today"] = 0,
        ["jutro"] = 1,
        ["tomorrow"] = 1,
        ["pojutrze"] = 2,
    };

    private static readonly Dictionary<string, DayOfWeek> Weekdays = new()
    {
        ["pon"] = DayOfWeek.Monday,
        ["poniedzialek"] = DayOfWeek.Monday,
        ["poniedziałek"] = DayOfWeek.Monday,
        ["mon"] = DayOfWeek.Monday,
        ["wt"] = DayOfWeek.Tuesday,
        ["wtorek"] = DayOfWeek.Tuesday,
        ["tue"] = DayOfWeek.Tuesday,
        ["sr"] = DayOfWeek.Wednesday,
        ["śr"] = DayOfWeek.Wednesday,
        ["sroda"] = DayOfWeek.Wednesday,
        ["środa"] = DayOfWeek.Wednesday,
        ["wed"] = DayOfWeek.Wednesday,
        ["czw"] = DayOfWeek.Thursday,
        ["czwartek"] = DayOfWeek.Thursday,
        ["thu"] = DayOfWeek.Thursday,
        ["pt"] = DayOfWeek.Friday,
        ["piatek"] = DayOfWeek.Friday,
        ["piątek"] = DayOfWeek.Friday,
        ["fri"] = DayOfWeek.Friday,
        ["sob"] = DayOfWeek.Saturday,
        ["sobota"] = DayOfWeek.Saturday,
        ["sat"] = DayOfWeek.Saturday,
        ["nd"] = DayOfWeek.Sunday,
        ["ndz"] = DayOfWeek.Sunday,
        ["niedziela"] = DayOfWeek.Sunday,
        ["sun"] = DayOfWeek.Sunday,
    };

    private static readonly string[] FullDateFormats = ["yyyy-MM-dd", "d.M.yyyy"];

    // "tommorow-18:00" -> date = "tommorow", time = "18:00"
    [GeneratedRegex(@"^(?<date>.+?)(?:-(?<time>\d{1,2}:\d{2}))?$")]
    private static partial Regex DateTimeRegex();

    // "+3d", "+2w", "+1m"
    [GeneratedRegex(@"^\+(?<n>\d{1,3})(?<unit>[dwm])$")]
    private static partial Regex RelativeRegex();

    /// <summary>Returns null if text is no valid date.</summary>
    public static DateTimeOffset? Parse(string input)
    {
        var match = DateTimeRegex().Match(input.Trim().ToLowerInvariant());
        if (!match.Success)
            return null;

        var today = DateOnly.FromDateTime(DateTime.Now);
        var date = ParseDate(match.Groups["date"].Value, today);
        if (date is null)
            return null;

        var time = DefaultTime;
        if (
            match.Groups["time"].Success
            && !TimeOnly.TryParseExact(
                match.Groups["time"].Value,
                "H:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out time
            )
        )
            return null;

        // Offset for target date and not for "now" (summer/winter time)
        var local = date.Value.ToDateTime(time);
        return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local));
    }

    private static DateOnly? ParseDate(string text, DateOnly today)
    {
        if (DayOffsets.TryGetValue(text, out int offset))
            return today.AddDays(offset);

        if (Weekdays.TryGetValue(text, out var weekday))
        {
            int diff = ((int)weekday - (int)today.DayOfWeek + 7) % 7;
            return today.AddDays(diff == 0 ? 7 : diff);
        }

        var relative = RelativeRegex().Match(text);
        if (relative.Success)
        {
            int n = int.Parse(relative.Groups["n"].Value);
            return relative.Groups["unit"].Value switch
            {
                "d" => today.AddDays(n),
                "w" => today.AddDays(7 * n),
                _ => today.AddMonths(n),
            };
        }

        if (
            DateOnly.TryParseExact(
                text,
                FullDateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var full
            )
        )
            return full;

        // "24.12" without year: current year, or next if date is before current day
        if (
            DateOnly.TryParseExact(
                $"{text}.{today.Year}",
                "d.M.yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var noYear
            )
        )
            return noYear < today ? noYear.AddYears(1) : noYear;

        return null;
    }
}
