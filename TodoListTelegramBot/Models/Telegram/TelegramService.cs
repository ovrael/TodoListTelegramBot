using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public class TelegramService
{
    private readonly TelegramBotClient bot;

    private readonly Command[] commands = [];

    private readonly VikunjaService vikunjaService;
    private readonly IReadOnlySet<long> allowedUserIds;

    private TelegramService(
        string token,
        VikunjaService vikunjaService,
        IReadOnlySet<long> allowedUserIds
    )
    {
        System.Console.WriteLine("Initializing Telegram service");
        this.allowedUserIds = allowedUserIds;
        bot = new TelegramBotClient(token);
        commands = CreateCommands();
        bot.OnMessage += OnTelegramMessage;

        this.vikunjaService = vikunjaService;
    }

    public static async Task<TelegramService> Create(
        string token,
        VikunjaService vikunjaService,
        IReadOnlySet<long> allowedUserIds
    )
    {
        var service = new TelegramService(token, vikunjaService, allowedUserIds);
        await service.SetCommands();
        return service;
    }

    private async Task SetCommands()
    {
        try
        {
            await bot.SetMyCommands(
                commands.Select(c => c.ToBotCommand()),
                BotCommandScope.AllPrivateChats()
            );
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine(
                $"Cannot set commands for bot but they should work. Exception: {ex}"
            );
        }
    }

    bool IsAuthorized(Message message) =>
        message.Chat.Type == ChatType.Private
        && message.From is not null
        && allowedUserIds.Contains(message.From.Id);

    private Command[] CreateCommands()
    {
        return
        [
            new Command("/projects", "List current Vikunja projects", CommandList.OnProjects),
            new Command(
                "/tasks",
                "List current Vikunja tasks from newest to oldest",
                CommandList.OnTasksNewFirst
            ),
            new Command(
                "/tasks_newest",
                "List current Vikunja tasks from oldest to newest",
                CommandList.OnTasksOldFirst
            ),
        ];
    }

    async Task OnTelegramMessage(Message message, UpdateType updateType)
    {
        if (!IsAuthorized(message))
        {
            Console.WriteLine(
                $"Ignored message from unauthorized user {message.From?.Id} (chat type: {message.Chat.Type})"
            );
            return;
        }

        Console.WriteLine($"Handling message of type {message.Type}");
        switch (message.Type)
        {
            case MessageType.Text:
                await OnTextMessage(message);
                break;
            case MessageType.Photo:
                await OnPhotoMessage(message);
                break;
            default:
                await OnUnhandledMessage(message);
                break;
        }
    }

    async Task OnTextMessage(Message message)
    {
        string? text = message.Text;
        if (string.IsNullOrEmpty(text))
        {
            System.Console.WriteLine(
                $"Text of message with type text is null or empty, suspicious and cannot be handled. Sets reaction to ({ReactionEmoji.ThinkingFace})."
            );
            await bot.SendMessage(
                message.Chat.Id,
                $"FAIL (more in logs), nothing was sent to Vikunja."
            );
            await bot.SetMessageReaction(message.Chat.Id, message.Id, [ReactionEmoji.ThinkingFace]);
            return;
        }

        if (text.StartsWith('/'))
            await OnTextCommandMessage(message);
        else
            await OnTextTodoMessage(message);
    }

    async Task OnTextTodoMessage(Message message)
    {
        System.Console.WriteLine($"Analyzing message text");
        var analysis = TextMessageAnalyzer.Analyze(message.Text!);
        if (analysis is null)
        {
            System.Console.WriteLine(
                $"TextMessageAnalyzer returned null, suspicious and cannot be handled. Sets reaction to ({ReactionEmoji.ThinkingFace})."
            );
            await bot.SendMessage(
                message.Chat.Id,
                $"FAIL (more in logs), nothing was sent to Vikunja."
            );
            await bot.SetMessageReaction(message.Chat.Id, message.Id, [ReactionEmoji.ThinkingFace]);
            return;
        }

        await bot.SendMessage(
            message.Chat.Id,
            $"Analyzed message. Sent to Vikunja's board: [BOARD NAME]. Sets reaction to ({ReactionEmoji.OkHand})."
        );

        await bot.SetMessageReaction(message.Chat.Id, message.Id, [ReactionEmoji.OkHand]);
    }

    async Task OnTextCommandMessage(Message message)
    {
        System.Console.WriteLine($"Received command: {message.Text!}");

        var command = commands.FirstOrDefault(c => c.CommandText == message.Text!);
        if (command is null)
        {
            System.Console.WriteLine($"Unknown command: {command}");
            await bot.SendMessage(
                message.Chat.Id,
                $"Unknown command {message.Text!}. Sets reaction to ({ReactionEmoji.ClownFace})."
            );

            await bot.SetMessageReaction(message.Chat.Id, message.Id, [ReactionEmoji.ClownFace]);
            return;
        }

        await command.Run(bot, vikunjaService, message);
    }

    async Task OnPhotoMessage(Message message)
    {
        if (message.Photo is null)
        {
            System.Console.WriteLine(
                $"Photo of message with type photo is null, suspicious and cannot be handled. Sets reaction to ({ReactionEmoji.ThinkingFace})."
            );
            await bot.SendMessage(
                message.Chat.Id,
                $"FAIL (more in logs), nothing was sent to Vikunja."
            );
            await bot.SetMessageReaction(message.Chat.Id, message.Id, [ReactionEmoji.ThinkingFace]);
            return;
        }
        if (message.Photo.Length == 0)
        {
            System.Console.WriteLine(
                $"Photo of message with type photo doesn't contain photos, suspicious and cannot be handled. Sets reaction to ({ReactionEmoji.ThinkingFace})."
            );
            await bot.SendMessage(
                message.Chat.Id,
                $"FAIL (more in logs), nothing was sent to Vikunja."
            );

            await bot.SetMessageReaction(message.Chat.Id, message.Id, [ReactionEmoji.ThinkingFace]);
            return;
        }

        var photo = message.Photo.OrderByDescending(p => p.Width * p.Height).FirstOrDefault();
        if (photo is null)
        {
            System.Console.WriteLine(
                $"First photo of message with type photo is null, suspicious and cannot be handled. Sets reaction to ({ReactionEmoji.ThinkingFace})."
            );
            await bot.SendMessage(
                message.Chat.Id,
                $"FAIL (more in logs), nothing was sent to Vikunja."
            );
            await bot.SetMessageReaction(message.Chat.Id, message.Id, [ReactionEmoji.ThinkingFace]);
            return;
        }

        var photoFile = await bot.GetFile(photo.FileId);
        if (photoFile is null || string.IsNullOrEmpty(photoFile.FilePath))
        {
            System.Console.WriteLine(
                $"First photo of message with type photo cannot be send, suspicious and cannot be handled. Sets reaction to ({ReactionEmoji.ThinkingFace})."
            );

            await bot.SendMessage(
                message.Chat.Id,
                $"FAIL (more in logs), nothing was sent to Vikunja."
            );
            await bot.SetMessageReaction(message.Chat.Id, message.Id, [ReactionEmoji.ThinkingFace]);
            return;
        }

        using (MemoryStream stream = new MemoryStream())
        {
            await bot.DownloadFile(photoFile.FilePath, stream);
            await File.WriteAllBytesAsync("test.png", stream.GetBuffer());
        }

        await bot.SendMessage(
            message.Chat.Id,
            $"Photo message sent to Vikunja's board PHOTOS. Sets reaction to ({ReactionEmoji.OkHand})."
        );
        await bot.SetMessageReaction(message.Chat.Id, message.Id, [ReactionEmoji.OkHand]);
    }

    async Task OnUnhandledMessage(Message message)
    {
        Console.WriteLine(
            $"Can't recognize this message type ({message.Type}). I am not using Vikunja but sets reaction to this message ({ReactionEmoji.PileOfPoo}) to be changed later by YOU."
        );
        await bot.SendMessage(
            message.Chat.Id,
            $"Can't recognize this message type ({message.Type}). Nothing was sent to Vikunja."
        );

        await bot.SetMessageReaction(message.Chat.Id, message.Id, [ReactionEmoji.PileOfPoo]);
    }
}
