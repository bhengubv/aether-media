// SPDX-License-Identifier: MIT

using AetherMesh.Content;
using AetherMesh.Media.Reel;
using Microsoft.Extensions.Logging;

namespace AetherMesh.Media.Reel.Tests.Helpers;

/// <summary>
/// Subclass that routes persistence to a caller-supplied temp directory.
/// </summary>
internal sealed class IsolatedSoundLibrary(
    IContentService        content,
    string                 localUhid,
    string                 tempDir,
    ILogger<SoundLibrary>? logger = null)
    : SoundLibrary(content, localUhid, tempDir, logger);
