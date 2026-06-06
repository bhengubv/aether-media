// SPDX-License-Identifier: MIT

namespace AetherMedia.Reel;

/// <summary>
/// An audio track that is currently trending on the local peer cluster.
///
/// Like <see cref="TrendingHashtag"/>, counts are gossipped aggregates with no
/// individual listener data.
/// </summary>
public sealed record TrendingSound(
    /// <summary>Content hash of the audio chunk.</summary>
    string SoundHash,

    /// <summary>Display title of the sound.</summary>
    string Title,

    /// <summary>Artist or original creator name, if known.</summary>
    string? ArtistName,

    /// <summary>Approximate number of Reels using this sound in the last 24 hours.</summary>
    long UseCount24h,

    /// <summary>
    /// Growth velocity — ratio of UseCount24h to the previous 24-hour window.
    /// </summary>
    float Velocity
);
