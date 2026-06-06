// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Library;

/// <summary>
/// One "track played" event. Winamp's library counts a track once when the
/// listener crosses a configurable percentage of the duration — the host
/// player calls <c>RecordAsync</c> when that threshold is reached.
/// </summary>
/// <param name="FilePath">Absolute path of the played file.</param>
/// <param name="PlayedAtUtc">UTC timestamp of the play event.</param>
/// <param name="ListenedMs">Milliseconds the listener actually heard (≤ track duration).</param>
public sealed record PlayEvent(
    string FilePath,
    DateTimeOffset PlayedAtUtc,
    long ListenedMs);
