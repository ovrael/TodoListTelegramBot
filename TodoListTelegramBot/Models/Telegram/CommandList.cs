using Telegram.Bot;
using Telegram.Bot.Types;

public static class CommandList
{
    public static async Task OnProjects(
        TelegramBotClient bot,
        VikunjaService vikunja,
        Message message
    )
    {
        var projects = await vikunja.GetProjects();
        string respond;
        if (projects is null)
            respond = "Received null from Vikunja API";
        else
            respond =
                $"Current Vikunja projects I can see:\n\t• "
                + string.Join("\n\t• ", projects.Select(p => p.Title));

        await bot.SendMessage(message.Chat.Id, respond);
    }

    internal static async Task OnTasksNewFirst( // newer will be higher on list
        TelegramBotClient bot,
        VikunjaService vikunja,
        Message message
    ) => await OnTasks(bot, vikunja, message, false);

    internal static async Task OnTasksOldFirst( // newer will be on the bottom of list
        TelegramBotClient bot,
        VikunjaService vikunja,
        Message message
    ) => await OnTasks(bot, vikunja, message, true);

    internal static async Task OnTasks(
        TelegramBotClient bot,
        VikunjaService vikunja,
        Message message,
        bool oldFirst
    )
    {
        var tasks = await vikunja.GetTasks();
        string respond = "Received null from Vikunja API";
        if (tasks is not null)
        {
            string direction;
            if (oldFirst)
            {
                direction = "older -> newer";
                tasks = tasks.OrderBy(t => t.Created).ToArray();
            }
            else
            {
                direction = "newer -> older";
                tasks = tasks.OrderByDescending(t => t.Created).ToArray();
            }

            respond =
                $"Current tasks ({direction}):\n\t• "
                + string.Join("\n\t• ", tasks.Select(p => p.Title));
        }
        await bot.SendMessage(message.Chat.Id, respond);
    }
}
