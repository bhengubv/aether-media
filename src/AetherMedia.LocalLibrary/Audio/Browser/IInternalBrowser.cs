// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Browser;

/// <summary>
/// Opens an external URL — the modern replacement for Winamp's bundled
/// mini-browser. Embedding a full HTML engine inside the player belongs in
/// the desktop shell (WebView2 / WKWebView), not in the audio library, so the
/// default impl <see cref="SystemBrowser"/> delegates to the OS browser. UIs
/// that want an in-window webview register their own impl.
/// </summary>
public interface IInternalBrowser
{
    /// <summary>
    /// Open <paramref name="url"/>. Returns <c>true</c> when the launch was
    /// dispatched, <c>false</c> when no handler was available.
    /// </summary>
    bool Open(Uri url);
}
