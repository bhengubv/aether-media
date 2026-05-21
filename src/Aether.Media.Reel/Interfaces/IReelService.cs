// SPDX-License-Identifier: MIT

namespace Aether.Media.Reel.Interfaces;

/// <summary>
/// Core Reel service — publish, discover, interact with Reels, and respond to
/// mesh-received content.
///
/// All video content is published and retrieved via the Aether content layer
/// (<c>IContentService</c>) so Reels are automatically available to mesh peers
/// without any centralised infrastructure.
/// </summary>
public interface IReelService
{
    // ── Publishing ──────────────────────────────────────────────────────────

    /// <summary>
    /// Publishes a new original Reel to the Aether content layer and announces
    /// it to mesh peers.
    /// </summary>
    /// <param name="videoFilePath">Absolute path to the encoded video file.</param>
    /// <param name="soundHash">
    /// Content hash of an existing sound track to associate, or <c>null</c> to
    /// use the video's own embedded audio.
    /// </param>
    /// <param name="hashtags">Hashtags without the '#' prefix, lower-case.</param>
    /// <param name="title">Optional display title.</param>
    Task<Reel> PublishAsync(
        string   videoFilePath,
        string?  soundHash,
        string[] hashtags,
        string   title = "",
        CancellationToken ct = default);

    /// <summary>
    /// Publishes a Duet response — the new video plays side-by-side with the original.
    /// </summary>
    Task<Reel> PublishDuetAsync(
        string videoFilePath,
        string sourceReelHash,
        string[] hashtags = default!,
        CancellationToken ct = default);

    /// <summary>
    /// Publishes a Stitch — clips the first 5 s of the source Reel then plays
    /// the new footage.
    /// </summary>
    Task<Reel> PublishStitchAsync(
        string videoFilePath,
        string sourceReelHash,
        string[] hashtags = default!,
        CancellationToken ct = default);

    // ── Retrieval ───────────────────────────────────────────────────────────

    /// <summary>Returns a Reel by content hash, or <c>null</c> if not in the local index.</summary>
    Task<Reel?> GetAsync(string contentHash, CancellationToken ct = default);

    /// <summary>Returns all Reels in the local index created by the given creator UHID.</summary>
    Task<IReadOnlyList<Reel>> GetByCreatorAsync(string creatorUhid, CancellationToken ct = default);

    // ── Interactions ────────────────────────────────────────────────────────

    /// <summary>Records a like for the given Reel (idempotent).</summary>
    Task LikeAsync(string contentHash, CancellationToken ct = default);

    /// <summary>Removes a previously recorded like (idempotent).</summary>
    Task UnlikeAsync(string contentHash, CancellationToken ct = default);

    /// <summary>Returns <c>true</c> if the local device has liked the Reel.</summary>
    Task<bool> IsLikedAsync(string contentHash, CancellationToken ct = default);

    /// <summary>Bookmarks the Reel locally (idempotent).</summary>
    Task BookmarkAsync(string contentHash, CancellationToken ct = default);

    /// <summary>Removes a bookmark (idempotent).</summary>
    Task UnbookmarkAsync(string contentHash, CancellationToken ct = default);

    /// <summary>Returns <c>true</c> if the local device has bookmarked the Reel.</summary>
    Task<bool> IsBookmarkedAsync(string contentHash, CancellationToken ct = default);

    // ── Comments ────────────────────────────────────────────────────────────

    /// <summary>Returns all locally-known comments for the Reel, ordered by creation time.</summary>
    Task<IReadOnlyList<ReelComment>> GetCommentsAsync(string contentHash, CancellationToken ct = default);

    /// <summary>
    /// Adds a comment and broadcasts it to connected peers.
    /// </summary>
    /// <param name="parentCommentId">
    /// ID of the comment being replied to, or <c>null</c> for a top-level comment.
    /// </param>
    Task<ReelComment> AddCommentAsync(
        string  contentHash,
        string  text,
        string? parentCommentId = null,
        CancellationToken ct = default);

    // ── Events ──────────────────────────────────────────────────────────────

    /// <summary>Fired when a Reel is successfully published by this node.</summary>
    event EventHandler<Reel>? ReelPublished;

    /// <summary>Fired when a new Reel announcement is received from a mesh peer.</summary>
    event EventHandler<Reel>? ReelReceived;

    /// <summary>Fired when a comment arrives from a mesh peer.</summary>
    event EventHandler<ReelComment>? CommentReceived;
}
