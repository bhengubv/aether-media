// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Time;

/// <summary>
/// Stops playback after a configured delay — Winamp's <c>Stop after current
/// track</c> / sleep-timer feature. Implementations expose a single timer
/// (re-arming replaces the old schedule).
/// </summary>
public interface ISleepTimer : IDisposable
{
    /// <summary>True if a sleep timer is currently armed.</summary>
    bool IsArmed { get; }

    /// <summary>Time remaining on the armed timer, or null when not armed.</summary>
    TimeSpan? Remaining { get; }

    /// <summary>Arm a fresh sleep timer with the given delay.</summary>
    void Arm(TimeSpan delay);

    /// <summary>Cancel the currently-armed timer (no-op if none).</summary>
    void Cancel();

    /// <summary>Fires when the timer elapses. Subscriber stops the player.</summary>
    event EventHandler? Elapsed;
}
