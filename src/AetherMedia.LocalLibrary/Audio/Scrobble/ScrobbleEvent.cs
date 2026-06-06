// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Scrobble;

/// <summary>
/// One "track played" record submitted to a scrobbling service. Per Last.fm
/// guidance: scrobble at <c>StartedAt + min(duration/2, 4min)</c>.
/// </summary>
public sealed record ScrobbleEvent(
    string Artist,
    string Title,
    string? Album,
    DateTimeOffset StartedAtUtc,
    TimeSpan Duration);
