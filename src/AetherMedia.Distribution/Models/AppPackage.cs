// SPDX-License-Identifier: MIT

namespace AetherMedia.Distribution.Models;

/// <summary>
/// Describes an installable app package that can be distributed over the mesh
/// or downloaded from Cloudflare R2.
/// </summary>
public sealed record AppPackage
{
    /// <summary>Stable lowercase identifier — e.g. "aether-media", "slepton", "sdpkt".</summary>
    public required string AppId { get; init; }

    /// <summary>Human-readable display name.</summary>
    public required string Name { get; init; }

    /// <summary>Semantic version string — e.g. "1.0.0".</summary>
    public required string Version { get; init; }

    /// <summary>
    /// SHA-256 hex digest of the APK/installer bytes.
    /// Doubles as the <c>RootHash</c> when the package is published via
    /// <see cref="AetherNet.Content.IContentService"/>.
    /// </summary>
    public required string ContentHash { get; init; }

    /// <summary>Total installer size in bytes.</summary>
    public required long SizeBytes { get; init; }

    /// <summary>
    /// Cloudflare R2 URL of the <c>latest.json</c> version manifest for this app.
    /// Empty string when the package was discovered from a mesh peer rather than
    /// the Cloudflare catalogue.
    /// </summary>
    public required string CloudflareUrl { get; init; }

    /// <summary>One-sentence description shown in the "More Apps" screen.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Short feature tags — e.g. ["media", "streaming", "social"].</summary>
    public string[] Tags { get; init; } = [];

    /// <summary>MIME type of the installer binary.</summary>
    public string ContentType { get; init; } = "application/vnd.android.package-archive";

    /// <summary>Comma-joined tags for display.</summary>
    public string TagsDisplay => string.Join(" · ", Tags);
}
