// SPDX-License-Identifier: MIT

namespace AetherNet.Media.Reel;

/// <summary>
/// A reusable audio track in the Reel sound library.
///
/// A sound is stored as a raw audio chunk in <c>IContentService</c> and identified
/// by its <see cref="SoundHash"/>. Any Reel that references this hash will play the
/// same audio, enabling the "use this sound" discovery mechanic.
///
/// <see cref="UseCount"/> is an approximate gossipped aggregate — it cannot be
/// traced to individual listeners.
/// </summary>
public sealed record Sound(
    /// <summary>SHA-256 of the audio bytes — primary key.</summary>
    string SoundHash,

    /// <summary>Display title shown in the sound library and on Reels.</summary>
    string Title,

    /// <summary>Artist or creator name, if known.</summary>
    string? ArtistName,

    /// <summary>
    /// Content hash of the Reel this sound was originally extracted from, or
    /// <c>null</c> if the sound was uploaded directly.
    /// </summary>
    string? OriginalReelHash,

    /// <summary>Duration of the audio in milliseconds.</summary>
    long DurationMs,

    /// <summary>Gossipped approximate count of Reels currently using this sound.</summary>
    long UseCount
);
