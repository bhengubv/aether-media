// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Hotkeys;

/// <summary>
/// Player-side semantic commands that can be bound to system-global key
/// combinations — the standard set Winamp shipped under
/// <c>Preferences ▸ Global Hotkeys</c>.
/// </summary>
public enum HotkeyCommand
{
    PlayPause,
    Stop,
    Next,
    Previous,
    VolumeUp,
    VolumeDown,
    Mute,
    SeekForward,
    SeekBackward,
    ShowHide,
}
