// SPDX-License-Identifier: MIT
namespace AetherNet.Media.Core.Interfaces;

/// <summary>
/// A single piece of media content available on the Aether network.
/// Identified by its SHA-256 content hash.
/// </summary>
public interface IContentNode
{
    string ContentHash { get; }
    string Title       { get; }
    long   DurationMs  { get; }
    string Codec       { get; }
    string ContentType { get; }
    string CreatorUhid { get; }
    long   SizeBytes   { get; }
    long   CreatedAtMs { get; }
    string? ThumbnailHash { get; }
    IReadOnlyList<string> Tags { get; }

    bool IsVideo => ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
    bool IsAudio => ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);
    bool IsLive  => DurationMs == 0;
}
