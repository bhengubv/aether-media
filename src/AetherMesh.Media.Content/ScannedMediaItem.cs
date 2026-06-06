// SPDX-License-Identifier: MIT

using AetherMesh.Media.Core.Models;

namespace AetherMesh.Media.Content;

/// <summary>
/// The result of scanning a single local file.
/// Pairs a <see cref="MediaContent"/> descriptor (content-addressed, mesh-ready)
/// with the absolute file path it was produced from.
///
/// The path is a Desktop/local concern — it is never gossiped over the mesh.
/// It exists here so the UI layer can open the file for metadata editing,
/// subtitle lookup, or direct playback without a reverse hash lookup.
/// </summary>
public sealed record ScannedMediaItem(
    /// <summary>Content descriptor — use this for library storage and mesh distribution.</summary>
    MediaContent Content,

    /// <summary>Absolute path to the source file on the local filesystem.</summary>
    string FilePath);
