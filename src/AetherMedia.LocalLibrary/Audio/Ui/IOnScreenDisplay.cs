// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Ui;

/// <summary>One on-screen-display message (track change, volume change, etc.).</summary>
public sealed record OsdMessage(
    string Text,
    string? SecondaryText,
    TimeSpan Duration);

/// <summary>
/// Shows transient HUD-style messages over the player. The Winamp OSD plugin
/// equivalent — track titles flash on screen when songs change. Host shells
/// subscribe to <see cref="MessageShown"/> and render the overlay.
/// </summary>
public interface IOnScreenDisplay
{
    /// <summary>Display a message. The previous message (if any) is replaced immediately.</summary>
    void Show(OsdMessage message);

    /// <summary>Currently visible message, or null when nothing is showing.</summary>
    OsdMessage? CurrentMessage { get; }

    /// <summary>Fires on every <see cref="Show"/>.</summary>
    event EventHandler<OsdMessage>? MessageShown;

    /// <summary>Fires when the message's duration elapses.</summary>
    event EventHandler? MessageDismissed;
}
