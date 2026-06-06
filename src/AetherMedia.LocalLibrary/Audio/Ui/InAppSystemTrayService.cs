// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Ui;

/// <summary>
/// Default <see cref="ISystemTrayService"/>. State + events only — host
/// shells (Avalonia <c>TrayIcon</c>, Cocoa <c>NSStatusItem</c>, etc.) wire
/// up the actual icon and call <see cref="HandleMenuClick"/> when an item
/// is activated.
/// </summary>
public sealed class InAppSystemTrayService : ISystemTrayService
{
    private readonly object _gate = new();
    private IReadOnlyList<TrayMenuItem> _items = Array.Empty<TrayMenuItem>();
    private string _tooltip = "AetherMedia";

    /// <inheritdoc/>
    public IReadOnlyList<TrayMenuItem> MenuItems
    {
        get { lock (_gate) return _items; }
    }

    /// <inheritdoc/>
    public string Tooltip
    {
        get { lock (_gate) return _tooltip; }
    }

    /// <inheritdoc/>
    public event EventHandler<TrayMenuItem>? MenuItemClicked;

    /// <inheritdoc/>
    public void SetMenu(IReadOnlyList<TrayMenuItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        lock (_gate) _items = items.ToList();
    }

    /// <inheritdoc/>
    public void SetTooltip(string tooltip)
    {
        ArgumentNullException.ThrowIfNull(tooltip);
        lock (_gate) _tooltip = tooltip;
    }

    /// <inheritdoc/>
    public void HandleMenuClick(string itemId)
    {
        ArgumentException.ThrowIfNullOrEmpty(itemId);
        TrayMenuItem? hit;
        lock (_gate) hit = _items.FirstOrDefault(i => i.Id == itemId);
        if (hit is not null) MenuItemClicked?.Invoke(this, hit);
    }
}
