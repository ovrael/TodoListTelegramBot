using Telegram.Bot;
using Telegram.Bot.Types;

public static class CommandList
{
    private static readonly string nullRespond = "Received null from Vikunja API";

    public static async Task OnProjects(
        TelegramBotClient bot,
        VikunjaService vikunja,
        Message message
    )
    {
        var projects = await vikunja.GetActiveProjects();
        string respond = nullRespond;
        if (projects is not null)
            respond =
                $"Active Vikunja projects I can see:\n\t• "
                + string.Join("\n\t• ", projects.Select(p => p.Title));

        await bot.SendMessage(message.Chat.Id, respond);
    }

    internal static async Task OnTasks( // newer will be on the bottom of list
        TelegramBotClient bot,
        VikunjaService vikunja,
        Message message
    )
    {
        var tasks = await vikunja.GetTasks();
        string respond = nullRespond;
        if (tasks is not null)
        {
            respond =
                $"Current tasks (older -> newer):\n\t• "
                + string.Join("\n\t• ", tasks.OrderBy(t => t.Created).Select(p => p.Title));
        }
        await bot.SendMessage(message.Chat.Id, respond);
    }
}
