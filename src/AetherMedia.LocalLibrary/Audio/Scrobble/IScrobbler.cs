// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Scrobble;

/// <summary>
/// Submits <see cref="ScrobbleEvent"/>s to a remote service (Last.fm,
/// Libre.fm, ListenBrainz, …). Implementations buffer offline events and
/// drain when the network comes back.
/// </summary>
public interface IScrobbler
{
    /// <summary>True when the scrobbler has valid credentials.</summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Announce the current track to <c>updateNowPlaying</c>. Optional — only
    /// scrobble itself is required for credit.
    /// </summary>
    Task UpdateNowPlayingAsync(ScrobbleEvent ev, CancellationToken ct = default);

    /// <summary>Record one finished play.</summary>
    Task ScrobbleAsync(ScrobbleEvent ev, CancellationToken ct = default);

    /// <summary>
    /// Drain any locally-buffered scrobbles. Called periodically; idempotent
    /// when the buffer is empty.
    /// </summary>
    Task FlushAsync(CancellationToken ct = default);
}
