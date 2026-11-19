public class TextMessageAnalysis
{
    public string? ProjectName { get; set; }
    public string? SubprojectName { get; set; }
    public string? Date { get; set; }
    public string? Priority { get; set; }
    public string? Text { get; set; }

    public bool IsValid { get; set; }

    public PostVikunjaTask? CreatePostTask()
    {
        if (!IsValid)
            return null;

        return new PostVikunjaTask
        {
            Title = Text!,
            Priority = GetPriority(),
            DueDate = GetDueDate(),
        };
    }

    private int? GetPriority()
    {
        return Priority is null ? null : int.Parse(Priority);
    }

    private DateTimeOffset? GetDueDate()
    {
        DateTimeOffset? dueDate = null;
        if (Date is not null)
        {
            dueDate = DueDateParser.Parse(Date);
            if (dueDate is null)
            {
                System.Console.WriteLine($"Cannot convert date for PostTask: {Date}");
            }
        }
        return dueDate;
    }
}
