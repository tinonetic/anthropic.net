namespace Anthropic.Net.Models.Files;

using System.Text.Json.Serialization;

/// <summary>Files API metadata.</summary>
public class FileMetadata
{
    /// <summary>Gets or sets the file id (use as file_id in content block sources).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the filename.</summary>
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    /// <summary>Gets or sets the MIME type.</summary>
    [JsonPropertyName("mime_type")]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>Gets or sets the size in bytes.</summary>
    [JsonPropertyName("size_bytes")]
    public long SizeBytes { get; set; }

    /// <summary>Gets or sets the creation time.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets a value indicating whether the file can be downloaded (only files created by tools).</summary>
    [JsonPropertyName("downloadable")]
    public bool? Downloadable { get; set; }
}
