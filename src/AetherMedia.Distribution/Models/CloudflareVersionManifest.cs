// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace AetherMedia.Distribution.Models;

/// <summary>
/// JSON shape of <c>https://cdn.aethermedia.app/apps/{appId}/latest.json</c>.
///
/// Example:
/// <code>
/// {
///   "appId":       "aether-media",
///   "version":     "1.1.0",
///   "url":         "https://cdn.aethermedia.app/apps/aether-media/aether-media-1.1.0.apk",
///   "sha256":      "a3f2...",
///   "sizeBytes":   47185920,
///   "releaseNotes":"Watch-party latency improvements, new Nearby feed."
/// }
/// </code>
/// </summary>
internal sealed class CloudflareVersionManifest
{
    [JsonPropertyName("appId")]
    public string AppId { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = string.Empty;

    [JsonPropertyName("sizeBytes")]
    public long SizeBytes { get; set; }

    [JsonPropertyName("releaseNotes")]
    public string? ReleaseNotes { get; set; }
}
