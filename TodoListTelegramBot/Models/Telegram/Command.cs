using Telegram.Bot;
using Telegram.Bot.Types;

public class Command
{
    public string CommandText { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Func<TelegramBotClient, VikunjaService, Message, Task> OnCommand;

    public Command(
        string cmd,
        string dsc,
        Func<TelegramBotClient, VikunjaService, Message, Task> ev
    )
    {
        CommandText = cmd;
        Description = dsc;
        OnCommand = ev;
    }

    public async Task Run(TelegramBotClient bot, VikunjaService vikunja, Message msg)
    {
        System.Console.WriteLine($"Running command {CommandText}");
        await OnCommand.Invoke(bot, vikunja, msg);
        System.Console.WriteLine($"Command end {CommandText}");
    }

    public BotCommand ToBotCommand()
    {
        return new BotCommand(CommandText, Description);
    }
}
