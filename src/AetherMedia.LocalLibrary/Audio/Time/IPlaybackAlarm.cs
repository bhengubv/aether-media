// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Time;

/// <summary>
/// Wakes the player at a scheduled time — Winamp's alarm-clock plugin.
/// The subscriber to <see cref="Triggered"/> is responsible for actually
/// starting playback (and selecting which content to play).
/// </summary>
public interface IPlaybackAlarm : IDisposable
{
    /// <summary>The next scheduled trigger time, or null when no alarm is armed.</summary>
    DateTimeOffset? NextTrigger { get; }

    /// <summary>
    /// Schedule the alarm for <paramref name="when"/>. Replaces any
    /// existing schedule. Must be in the future.
    /// </summary>
    void ArmAt(DateTimeOffset when);

    /// <summary>Cancel the currently-armed alarm (no-op if none).</summary>
    void Cancel();

    /// <summary>Fires at the scheduled time. Subscriber starts playback.</summary>
    event EventHandler? Triggered;
}
