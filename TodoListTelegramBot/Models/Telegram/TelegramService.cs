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
                "List current Vikunja tasks from oldest to newest",
                CommandList.OnTasks
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
        System.Console.WriteLine($"Analyzing message text, created to task for Vikunja");
        var analysis = TextMessageAnalyzer.Analyze(message.Text!);

        if (!analysis.IsValid)
        {
            await bot.SendMessage(message.Chat.Id, "Task title is empty.");
            return;
        }

        long projectId;
        if (analysis.ProjectName is null)
        {
            System.Console.WriteLine(
                "No project name in massage, set to default user project or 0"
            );
            // No #project in message, get default project id
            var settings = await vikunjaService.GetUserSettings();
            projectId = settings?.DefaultProjectId ?? 0;
        }
        else
        {
            var project = await vikunjaService.FindProjectByName(analysis.ProjectName);
            if (project is null)
            {
                System.Console.WriteLine(
                    $"Cannot find project with name {analysis.ProjectName}, set to default user project or 0"
                );
                var settings = await vikunjaService.GetUserSettings();
                projectId = settings?.DefaultProjectId ?? 0;
            }
            else
                projectId = project.Id;
        }

        System.Console.WriteLine($"Trying to add task to Project ({projectId})");

        if (await vikunjaService.AddTask(projectId, analysis.CreatePostTask()!) is null)
        {
            System.Console.WriteLine("Cannot create task!");
            await bot.SendMessage(
                message.Chat.Id,
                "FAIL (more in logs), nothing was sent to Vikunja."
            );
            return;
        }

        System.Console.WriteLine("Task created!");

        // await bot.SendMessage(
        //     message.Chat.Id,
        //     $"Analyzed message. Sent to Vikunja's board: {analysis.ProjectName}. Sets reaction to ({ReactionEmoji.OkHand})."
        // );

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
        if (message.Photo is not { Length: > 0 })
        {
            await ReplyFail(message, "Photo message without photos.");
            return;
        }

        var analysis = TextMessageAnalyzer.Analyze(message.Caption ?? string.Empty);
        if (!analysis.IsValid)
        {
            analysis.Text = $"Photo {DateTime.Now:yyyy-MM-dd HH:mm}";
            analysis.IsValid = true;
        }

        long projectId = await ResolveProjectId(analysis.ProjectName);
        var task = await vikunjaService.AddTask(projectId, analysis.CreatePostTask()!);
        if (task is null)
        {
            await ReplyFail(message, "Cannot create task for photo.");
            return;
        }

        // Take max resolution
        var photo = message.Photo.MaxBy(p => p.Width * p.Height)!;

        using var stream = new MemoryStream();
        await bot.GetInfoAndDownloadFile(photo, stream);
        stream.Position = 0; // requied, otherwise the file will be empty

        if (
            !await vikunjaService.AddAttachment(
                task.Id,
                stream,
                $"telegram_{message.Id}.jpg",
                "image/jpeg"
            )
        )
        {
            Console.WriteLine($"Task {task.Id} created, but attachment upload failed.");
            await bot.SendMessage(
                message.Chat.Id,
                $"Task \"{task.Title}\" created, but the photo was not uploaded (more in logs)."
            );
            await bot.SetMessageReaction(message.Chat.Id, message.Id, [ReactionEmoji.ThinkingFace]);
            return;
        }

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

    async Task<long> ResolveProjectId(string? projectName)
    {
        if (projectName is not null)
        {
            var project = await vikunjaService.FindProjectByName(projectName);
            if (project is not null)
                return project.Id;

            Console.WriteLine(
                $"Cannot find project with name {projectName}, using default project"
            );
        }

        var settings = await vikunjaService.GetUserSettings();
        return settings?.DefaultProjectId ?? 0;
    }

    async Task ReplyFail(Message message, string log)
    {
        Console.WriteLine(log);
        await bot.SendMessage(message.Chat.Id, "FAIL (more in logs), nothing was sent to Vikunja.");
        await bot.SetMessageReaction(message.Chat.Id, message.Id, [ReactionEmoji.ThinkingFace]);
    }
}
