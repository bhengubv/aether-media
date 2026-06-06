// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Hotkeys;

/// <summary>
/// Modifier flags that compose with a key code to form a hotkey. Values map
/// to the Win32 <c>MOD_*</c> constants so a binding can be passed straight
/// to <c>RegisterHotKey</c>.
/// </summary>
[Flags]
public enum HotkeyModifiers
{
    None    = 0,
    Alt     = 1,
    Control = 2,
    Shift   = 4,
    Win     = 8,
}
