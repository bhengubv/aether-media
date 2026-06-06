// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Hotkeys;

/// <summary>
/// A semantic command paired with the OS-level key combination that should
/// trigger it.
/// </summary>
/// <param name="Command">The semantic action.</param>
/// <param name="KeyCode">
/// Virtual-key code (<c>VK_*</c> on Windows; the cross-platform equivalent
/// where the host OS provides one). Examples: <c>0x6F</c> = numpad divide,
/// <c>0xB3</c> = VK_MEDIA_PLAY_PAUSE.
/// </param>
/// <param name="Modifiers">Required modifier keys.</param>
public sealed record HotkeyBinding(
    HotkeyCommand Command,
    int KeyCode,
    HotkeyModifiers Modifiers = HotkeyModifiers.None);
