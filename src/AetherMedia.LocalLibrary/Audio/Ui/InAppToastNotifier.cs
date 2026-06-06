// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Ui;

/// <summary>
/// Cross-platform fallback <see cref="IToastNotifier"/>. Raises
/// <see cref="ToastRequested"/> instead of touching the OS — the host shell
/// renders an in-app banner. Always available.
/// </summary>
public sealed class InAppToastNotifier : IToastNotifier
{
    /// <inheritdoc/>
    public bool IsAvailable => true;

    /// <summary>Fires for every <see cref="ShowAsync"/> call.</summary>
    public event EventHandler<ToastNotification>? ToastRequested;

    /// <inheritdoc/>
    public Task ShowAsync(ToastNotification toast, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(toast);
        ToastRequested?.Invoke(this, toast);
        return Task.CompletedTask;
    }
}
