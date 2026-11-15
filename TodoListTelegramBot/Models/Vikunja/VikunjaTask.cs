using System.Text.Json.Serialization;

public class VikunjaTask
{
    [JsonPropertyName("id")]
    public int Id { get; set; } = -1;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public string Created { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public int Priority { get; set; }
}
