using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

public class VikunjaService
{
    private HttpClient client;

    public VikunjaService(string uri, string token)
    {
        System.Console.WriteLine("Initializing Vikunja connection");
        client = new HttpClient();
        client.BaseAddress = new Uri(uri);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<string?> GetUser()
    {
        var response = await client.GetAsync("user");
        if (response.StatusCode != System.Net.HttpStatusCode.OK)
            return null;
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<UserSettings?> GetUserSettings()
    {
        var response = await client.GetAsync("user");
        if (response.StatusCode != System.Net.HttpStatusCode.OK)
            return null;
        string responseText = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(responseText);
        var root = document.RootElement;
        var settings = root.GetProperty("settings");
        return settings.Deserialize<UserSettings>();
    }

    public async Task<Project[]?> GetProjects()
    {
        var response = await client.GetAsync("projects");
        if (response.StatusCode != System.Net.HttpStatusCode.OK)
            return null;

        string responseText = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(responseText);
        var root = document.RootElement;
        var items = root.GetProperty("items");
        return items.Deserialize<Project[]>();
    }

    internal async Task<VikunjaTask[]?> GetTasks()
    {
        var response = await client.GetAsync("tasks");
        if (response.StatusCode != System.Net.HttpStatusCode.OK)
            return null;

        string responseText = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(responseText);
        var root = document.RootElement;
        var items = root.GetProperty("items");
        return items.Deserialize<VikunjaTask[]>();
    }

    internal async Task<bool> AddTask(TextMessageAnalysis analysis)
    {
        var task = new
        {
            title = analysis.Text,
            description = string.Empty,
            priority = analysis.Priority,
            due_date = analysis.Date,
        };

        var response = await client.PostAsJsonAsync($"projects/{analysis.ProjectId}/tasks", task);
        if (!response.IsSuccessStatusCode)
        {
            System.Console.WriteLine(await response.Content.ReadAsStringAsync());
            return false;
        }

        // string responseText = await response.Content.ReadAsStringAsync();

        // using var document = JsonDocument.Parse(responseText);
        // var root = document.RootElement;
        // var items = root.GetProperty("items");
        return true;
    }
}
