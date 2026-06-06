// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace AetherNet.Media.LocalLibrary.Models;

// ── Internal DTOs for the OpenSubtitles REST API v1 ──────────────────────────
// These are only used by SubtitleService and are not exposed in the public API.

internal sealed class OsSearchResponse
{
    [JsonPropertyName("total_count")] public int TotalCount { get; set; }
    [JsonPropertyName("data")]        public OsSubtitleItem[] Data { get; set; } = [];
}

internal sealed class OsSubtitleItem
{
    [JsonPropertyName("id")]         public string Id         { get; set; } = string.Empty;
    [JsonPropertyName("attributes")] public OsAttributes Attributes { get; set; } = new();
}

internal sealed class OsAttributes
{
    [JsonPropertyName("language")]        public string Language      { get; set; } = string.Empty;
    [JsonPropertyName("download_count")]  public int    DownloadCount { get; set; }
    [JsonPropertyName("moviehash_match")] public bool   MoviehashMatch { get; set; }
    [JsonPropertyName("ratings")]         public float  Ratings        { get; set; }
    [JsonPropertyName("release")]         public string Release        { get; set; } = string.Empty;
    [JsonPropertyName("files")]           public OsFile[] Files        { get; set; } = [];
    [JsonPropertyName("feature_details")] public OsFeatureDetails FeatureDetails { get; set; } = new();
}

internal sealed class OsFile
{
    [JsonPropertyName("file_id")]   public int    FileId   { get; set; }
    [JsonPropertyName("file_name")] public string FileName { get; set; } = string.Empty;
}

internal sealed class OsFeatureDetails
{
    [JsonPropertyName("movie_name")] public string MovieName { get; set; } = string.Empty;
    [JsonPropertyName("year")]       public int    Year      { get; set; }
}

internal sealed class OsDownloadRequest
{
    [JsonPropertyName("file_id")] public int FileId { get; set; }
}

internal sealed class OsDownloadResponse
{
    [JsonPropertyName("link")]      public string Link     { get; set; } = string.Empty;
    [JsonPropertyName("file_name")] public string FileName { get; set; } = string.Empty;
    [JsonPropertyName("remaining")] public int    Remaining { get; set; }
}
