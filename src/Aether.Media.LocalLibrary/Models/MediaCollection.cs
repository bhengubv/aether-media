// SPDX-License-Identifier: MIT

namespace Aether.Media.LocalLibrary.Models;

/// <summary>
/// A named group of media items.  Manual collections store an explicit ordered list of
/// content hashes; smart collections store a filter that is evaluated on demand.
/// </summary>
public sealed class MediaCollection
{
    /// <summary>Stable identifier (GUID without hyphens).</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public string         Name      { get; set; } = string.Empty;
    public CollectionType Type      { get; init; }
    public DateTime       CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime       UpdatedAt { get; set; }  = DateTime.UtcNow;

    /// <summary>
    /// Ordered content hashes for <see cref="CollectionType.Manual"/> collections.
    /// Empty for smart collections.
    /// </summary>
    public List<string> ContentHashes { get; set; } = [];

    /// <summary>
    /// Evaluation criteria for <see cref="CollectionType.Smart"/> collections.
    /// <c>null</c> for manual collections.
    /// </summary>
    public SmartCollectionFilter? Filter { get; set; }
}
