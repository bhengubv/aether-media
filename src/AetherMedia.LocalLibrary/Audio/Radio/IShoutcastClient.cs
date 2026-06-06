// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Radio;

/// <summary>
/// Shoutcast / Icecast HTTP audio stream client. Performs the icy-metadata
/// handshake, exposes the announced station metadata, and yields a pure
/// audio byte stream (icy metadata blocks stripped) for the codec engine.
/// </summary>
public interface IShoutcastClient
{
    /// <summary>
    /// Connect to <paramref name="streamUrl"/>, performing the icy-metadata
    /// handshake. The returned <see cref="ShoutcastStreamMetadata"/> reflects
    /// the headers; the stream titles are updated as inline metadata blocks
    /// arrive in <see cref="OpenStreamAsync"/>.
    /// </summary>
    Task<ShoutcastStreamMetadata> ConnectAsync(Uri streamUrl, CancellationToken ct = default);

    /// <summary>
    /// Open a continuous audio stream from the connected URL. Inline icy
    /// metadata blocks are removed; <see cref="MetadataUpdated"/> fires for
    /// each <c>StreamTitle</c> change.
    /// </summary>
    Task<Stream> OpenStreamAsync(Uri streamUrl, CancellationToken ct = default);

    /// <summary>Fires whenever the inline icy metadata yields a new <c>StreamTitle</c>.</summary>
    event EventHandler<string>? MetadataUpdated;
}
