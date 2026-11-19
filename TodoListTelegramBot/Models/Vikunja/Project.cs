using System.Text.Json.Serialization;

public class Project
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("is_archived")]
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// Id of the parent project. 0 or null for a top-level project.
    /// </summary>
    [JsonPropertyName("parent_project_id")]
    public long? ParentProjectId { get; set; }

    public bool IsSubproject => ParentProjectId is > 0;
}
