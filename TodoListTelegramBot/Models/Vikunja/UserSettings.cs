using System.Text.Json.Serialization;

public class UserSettings
{
    [JsonPropertyName("default_project_id")]
    public int DefaultProjectId { get; set; }
}
