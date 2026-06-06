// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Ui;

/// <summary>One context-menu entry on the system tray icon.</summary>
public sealed record TrayMenuItem(string Id, string Label, bool IsEnabled = true);

/// <summary>
/// System-tray (notification area / menu bar) integration. Host shells
/// supply the actual icon + native context menu; this contract drives the
/// menu contents and routes click events back to the player.
/// </summary>
public interface ISystemTrayService
{
    /// <summary>Current menu items, in display order.</summary>
    IReadOnlyList<TrayMenuItem> MenuItems { get; }

    /// <summary>Replace the menu.</summary>
    void SetMenu(IReadOnlyList<TrayMenuItem> items);

    /// <summary>Replace the visible tooltip text on the tray icon.</summary>
    void SetTooltip(string tooltip);

    /// <summary>Current tooltip text.</summary>
    string Tooltip { get; }

    /// <summary>Fires when the user clicks one of the menu items.</summary>
    event EventHandler<TrayMenuItem>? MenuItemClicked;

    /// <summary>Host-supplied click dispatcher — call when a tray menu item is activated.</summary>
    void HandleMenuClick(string itemId);
}
