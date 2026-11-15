using System.Text.Json.Serialization;

/// <summary>
/// Request body for creating a new task in Vikunja.
/// The project is set in the URL (projects/{id}/tasks), so it is not part of the body.
/// Null values are not sent, so Vikunja uses its own defaults for them.
/// </summary>
public class PostVikunjaTask
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    /// <summary>
    /// 0 = unset, 1 = low, 2 = medium, 3 = high, 4 = urgent, 5 = do now.
    /// </summary>
    [JsonPropertyName("priority")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Priority { get; init; }

    [JsonPropertyName("due_date")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? DueDate { get; init; }
}
