// SPDX-License-Identifier: MIT

namespace AetherMedia.Reel.Interfaces;

/// <summary>
/// The Reel sound library. Sounds are content-addressed audio chunks stored via
/// <c>IContentService</c> — they are permanent, hash-verified, and mesh-distributable.
///
/// Any Reel's audio can be extracted and added to the library, making it reusable
/// by other creators — the same mechanic that makes TikTok sounds go viral, but
/// without a centralised licensing deal or server.
/// </summary>
public interface ISoundLibrary
{
    // ── Discovery ───────────────────────────────────────────────────────────

    /// <summary>Returns the top trending sounds on the local peer cluster.</summary>
    Task<IReadOnlyList<Sound>> GetTrendingAsync(
        int count = 20,
        CancellationToken ct = default);

    /// <summary>Searches the local sound index by title or artist name.</summary>
    Task<IReadOnlyList<Sound>> SearchAsync(
        string query,
        int    count = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a <see cref="Sound"/> by its content hash, or <c>null</c> if not
    /// in the local index.
    /// </summary>
    Task<Sound?> GetAsync(string soundHash, CancellationToken ct = default);

    // ── Publishing ───────────────────────────────────────────────────────────

    /// <summary>
    /// Extracts the audio track from a video file, publishes it to
    /// <c>IContentService</c>, and registers it in the local sound library.
    /// </summary>
    /// <param name="videoFilePath">Path to the source video.</param>
    /// <param name="title">Display title for the sound.</param>
    /// <param name="artistName">Optional artist name.</param>
    /// <param name="originalReelHash">
    /// Content hash of the Reel the sound was extracted from, if applicable.
    /// </param>
    Task<Sound> ExtractAndPublishAsync(
        string  videoFilePath,
        string  title,
        string? artistName         = null,
        string? originalReelHash   = null,
        CancellationToken ct = default);

    /// <summary>
    /// Publishes a standalone audio file (MP3, AAC, FLAC, etc.) to the sound library.
    /// </summary>
    Task<Sound> PublishAudioFileAsync(
        string  audioFilePath,
        string  title,
        string? artistName = null,
        CancellationToken ct = default);

    // ── Playback ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the local file path to the audio chunk for playback, fetching
    /// it from mesh peers if not already cached.
    /// </summary>
    Task<string> GetLocalPathAsync(string soundHash, CancellationToken ct = default);
}
