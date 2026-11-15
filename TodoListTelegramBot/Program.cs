using Microsoft.Extensions.Configuration;

internal class Program
{
    static VikunjaService? vikunja;
    static TelegramService? telegram;

    private static async Task Main(string[] args)
    {
        Console.WriteLine("TelegramBot - Vikunja Service started!");

        var config = new ConfigurationBuilder()
            .AddUserSecrets<Program>(optional: true) // local (dev)
            .AddEnvironmentVariables() // server / Docker / systemd
            .Build();

        string vikunjaToken =
            config["Vikunja:Token"] ?? throw new InvalidOperationException("Missing Vikunja:Token");
        string vikunjaUri =
            config["Vikunja:Uri"] ?? throw new InvalidOperationException("Missing Vikunja:Uri");
        string telegramToken =
            config["Telegram:Token"]
            ?? throw new InvalidOperationException("Missing Telegram:Token");

        var allowedUserIds = (config["Telegram:AllowedUserIds"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(long.Parse)
            .ToHashSet();

        if (allowedUserIds.Count == 0)
            throw new InvalidOperationException("Missing Telegram:AllowedUserIds");

        vikunja = new VikunjaService(vikunjaUri, vikunjaToken);

        var user = await vikunja.GetUser();
        if (user is null)
        {
            System.Console.WriteLine("Cannot start service, vikunja failed.");
            return;
        }

        telegram = await TelegramService.Create(telegramToken, vikunja, allowedUserIds);

        await Task.Delay(Timeout.Infinite);
    }
}
