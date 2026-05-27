// SPDX-License-Identifier: MIT
using System.Text.Json.Serialization;

namespace Aether.Forge.Core;

/// <summary>
/// Represents a single package cached in the Aether Forge distributed
/// package-cache layer.
/// </summary>
/// <param name="ContentHash">SHA-256 hex digest of the cached package bytes.</param>
/// <param name="PackageId">
/// Fully-qualified package identifier in the form
/// <c>ecosystem:name@version</c>, e.g. <c>npm:react@18.2.0</c>.
/// </param>
/// <param name="FetchedAtUtc">UTC timestamp of when the package was first cached.</param>
/// <param name="SizeBytes">Byte length of the cached package payload.</param>
/// <param name="DownloadCount">
/// Number of times this entry has been served from the local Forge cache.
/// </param>
public sealed record ForgeEntry(
    [property: JsonPropertyName("content_hash")]    string ContentHash,
    [property: JsonPropertyName("package_id")]      string PackageId,
    [property: JsonPropertyName("fetched_at_utc")]  DateTime FetchedAtUtc,
    [property: JsonPropertyName("size_bytes")]      long SizeBytes,
    [property: JsonPropertyName("download_count")]  int DownloadCount);
