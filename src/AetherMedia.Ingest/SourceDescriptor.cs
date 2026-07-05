// SPDX-License-Identifier: MIT

namespace AetherMedia.Ingest;

/// <summary>
/// Describes an external source to ingest. The <see cref="Uri"/> is a pure runtime input — no
/// source is ever baked into the code.
/// </summary>
public sealed record SourceDescriptor
{
    /// <summary>The source location (runtime-supplied).</summary>
    public required Uri Uri { get; init; }

    /// <summary>Hint for the source kind; actual dispatch is by <see cref="ISourceAdapter.CanHandle"/>.</summary>
    public SourceKind Kind { get; init; } = SourceKind.Hls;

    /// <summary>Optional request headers to send when pulling the source.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>();

    /// <summary>Human-readable title advertised for the published mesh stream.</summary>
    public string Title { get; init; } = "Live";

    /// <summary>Searchable tags advertised for discovery.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}
