// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Ui;

/// <summary>One toast notification request.</summary>
public sealed record ToastNotification(string Title, string? Body, string? ImagePath = null);

/// <summary>
/// Dispatches a toast notification through whatever channel the host
/// supports — Windows Action Center, macOS UserNotifications, libnotify,
/// or an in-app fallback. The cross-platform default
/// <see cref="InAppToastNotifier"/> raises an event for the host UI to
/// render an in-window banner.
/// </summary>
public interface IToastNotifier
{
    /// <summary>True if the underlying channel is available.</summary>
    bool IsAvailable { get; }

    /// <summary>Show the toast (fire-and-forget).</summary>
    Task ShowAsync(ToastNotification toast, CancellationToken ct = default);
}
