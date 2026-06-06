// SPDX-License-Identifier: MIT

namespace AetherMesh.Media.Reel.Interfaces;

/// <summary>
/// Mesh-native discovery for the Reel platform — trending topics, sounds, and
/// full-text search across locally-indexed Reels.
///
/// Trending data is computed from gossipped peer aggregates: each node contributes
/// count increments to its neighbours, which propagate outward. No individual viewing
/// or engagement data is ever included — only aggregate counts per tag/sound.
/// </summary>
public interface IReelDiscovery
{
    // ── Trending ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the top trending hashtags on the local peer cluster, sorted by
    /// velocity (acceleration) then by raw count.
    /// </summary>
    Task<IReadOnlyList<TrendingHashtag>> GetTrendingHashtagsAsync(
        int count = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the top trending sounds on the local peer cluster, sorted by
    /// velocity then by raw use count.
    /// </summary>
    Task<IReadOnlyList<TrendingSound>> GetTrendingSoundsAsync(
        int count = 20,
        CancellationToken ct = default);

    // ── Search ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Searches the local Reel index by title, hashtag, or creator UHID.
    /// Returns results ranked by relevance then by score.
    /// </summary>
    Task<IReadOnlyList<Reel>> SearchAsync(
        string query,
        int    count = 20,
        CancellationToken ct = default);

    // ── Mesh integration ─────────────────────────────────────────────────────

    /// <summary>
    /// Announces a newly-published Reel to connected peers and increments the
    /// local hashtag and sound use counts used for trending computation.
    /// </summary>
    Task AnnounceReelAsync(Reel reel, CancellationToken ct = default);

    /// <summary>
    /// Merges a gossip payload received from a peer into the local trending index.
    /// Only aggregate counts are exchanged — no personal data.
    /// </summary>
    /// <param name="hashtagCounts">
    /// Map of hashtag → count increment observed by the sending peer.
    /// </param>
    /// <param name="soundCounts">
    /// Map of sound hash → use count increment observed by the sending peer.
    /// </param>
    Task MergeGossipAsync(
        IReadOnlyDictionary<string, long> hashtagCounts,
        IReadOnlyDictionary<string, long> soundCounts,
        CancellationToken ct = default);

    /// <summary>
    /// Builds and returns the current gossip payload this node should broadcast
    /// to its peers (hashtag and sound count deltas since last gossip round).
    /// </summary>
    Task<(IReadOnlyDictionary<string, long> HashtagCounts,
          IReadOnlyDictionary<string, long> SoundCounts)> BuildGossipPayloadAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Indexes a Reel received from a mesh peer so it participates in local search
    /// and feed scoring.
    /// </summary>
    Task IndexReelAsync(Reel reel, CancellationToken ct = default);
}
