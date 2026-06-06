// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Hotkeys;

/// <summary>
/// Registers system-wide key combinations and raises <see cref="HotkeyTriggered"/>
/// when one fires. Implementations are inherently platform-specific —
/// <see cref="WindowsGlobalHotkeyService"/> covers Windows; macOS / Linux
/// would use Carbon EventHotKey and X11 XGrabKey respectively.
/// </summary>
public interface IGlobalHotkeyService : IDisposable
{
    /// <summary>True if registration succeeded for at least one binding.</summary>
    bool IsActive { get; }

    /// <summary>
    /// Apply the given set of bindings. The implementation unregisters any
    /// previously-installed bindings first.
    /// </summary>
    void Register(IReadOnlyList<HotkeyBinding> bindings);

    /// <summary>Remove every binding currently installed.</summary>
    void UnregisterAll();

    /// <summary>Fires on the OS callback thread when a registered binding triggers.</summary>
    event EventHandler<HotkeyCommand>? HotkeyTriggered;
}
