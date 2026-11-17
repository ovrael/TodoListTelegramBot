using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

public class VikunjaService
{
    // The bot is written for the Vikunja API v2
    private const string ApiPath = "api/v2/";

    private HttpClient client;

    /// <param name="serverUri">Vikunja server address without the API path, e.g. https://vikunja.example.com</param>
    public VikunjaService(string serverUri, string token)
    {
        System.Console.WriteLine("Initializing Vikunja connection");
        client = new HttpClient();
        client.BaseAddress = BuildApiUri(serverUri);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        System.Console.WriteLine($"Vikunja API address: {client.BaseAddress}");
    }

    private static Uri BuildApiUri(string serverUri)
    {
        var baseUri = new Uri(serverUri.TrimEnd('/') + "/", UriKind.Absolute);

        if (baseUri.AbsolutePath.Contains("/api/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Vikunja:Uri must be the server address without the API path (got '{serverUri}'). "
                    + $"The bot adds '/{ApiPath}' itself."
            );

        return new Uri(baseUri, ApiPath);
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

    public async Task<Project[]?> GetActiveProjects()
    {
        var projects = await GetProjects();
        if (projects is null)
            return null;

        return projects.Where(p => !p.IsArchived).ToArray();
    }

    public async Task<Project?> FindProjectByName(string name)
    {
        var projects = await GetActiveProjects();
        return projects?.FirstOrDefault(p =>
            name.Length == 1
                ? p.Title.StartsWith(name, StringComparison.OrdinalIgnoreCase)
                : string.Equals(p.Title, name, StringComparison.OrdinalIgnoreCase)
        );
    }

    internal async Task<Project?> CreateProject(string title)
    {
        var response = await client.PostAsJsonAsync("projects", new { title });
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(await response.Content.ReadAsStringAsync());
            return null;
        }
        return await response.Content.ReadFromJsonAsync<Project>();
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

    internal async Task<VikunjaTask?> AddTask(long projectId, PostVikunjaTask task)
    {
        var response = await client.PostAsJsonAsync($"projects/{projectId}/tasks", task);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(await response.Content.ReadAsStringAsync());
            return null;
        }
        return await response.Content.ReadFromJsonAsync<VikunjaTask>();
    }

    internal async Task<bool> AddAttachment(
        long taskId,
        Stream content,
        string fileName,
        string contentType
    )
    {
        using var form = new MultipartFormDataContent();
        var file = new StreamContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "files", fileName);

        var response = await client.PostAsync($"tasks/{taskId}/attachments", form);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(await response.Content.ReadAsStringAsync());
            return false;
        }
        return true;
    }
}
