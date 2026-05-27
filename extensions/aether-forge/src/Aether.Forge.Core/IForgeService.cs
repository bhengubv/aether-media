// SPDX-License-Identifier: MIT
namespace Aether.Forge.Core;

/// <summary>
/// Provides operations for querying, caching, and fetching packages through
/// the Aether Forge distributed package-cache layer.
/// </summary>
public interface IForgeService
{
    // ── Observable ────────────────────────────────────────────────────────

    /// <summary>
    /// Hot observable that emits each new <see cref="ForgeEntry"/> as it is
    /// announced on the local mesh segment.
    /// </summary>
    IObservable<ForgeEntry> NewEntryAnnounced { get; }

    // ── Queries ────────────────────────────────────────────────────────────

    /// <summary>
    /// Looks up a package in the local Forge cache by its fully-qualified
    /// <paramref name="packageId"/> (e.g. <c>npm:react@18.2.0</c>).
    /// Returns <see langword="null"/> when the package is not cached.
    /// </summary>
    /// <param name="packageId">Ecosystem-prefixed package identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ForgeEntry?> QueryAsync(string packageId, CancellationToken ct = default);

    /// <summary>
    /// Returns aggregate statistics for the local Forge cache node.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<ForgeStats> GetStatsAsync(CancellationToken ct = default);

    // ── Mutations ──────────────────────────────────────────────────────────

    /// <summary>
    /// Stores a package payload in the local Forge cache and announces the
    /// new entry to neighbouring mesh nodes.
    /// </summary>
    /// <param name="packageId">
    /// Ecosystem-prefixed package identifier (e.g. <c>npm:react@18.2.0</c>).
    /// </param>
    /// <param name="content">Raw package byte stream.</param>
    /// <param name="contentHash">
    /// Pre-computed SHA-256 hex digest of <paramref name="content"/>.
    /// Implementations should verify this against the stored bytes.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created <see cref="ForgeEntry"/>.</returns>
    Task<ForgeEntry> CacheAsync(
        string packageId,
        Stream content,
        string contentHash,
        CancellationToken ct = default);

    /// <summary>
    /// Fetches the raw byte stream of a cached package by its
    /// <paramref name="contentHash"/>.
    /// Returns <see langword="null"/> when the content is not found locally.
    /// </summary>
    /// <param name="contentHash">SHA-256 hex digest of the package.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Stream?> FetchAsync(string contentHash, CancellationToken ct = default);
}
