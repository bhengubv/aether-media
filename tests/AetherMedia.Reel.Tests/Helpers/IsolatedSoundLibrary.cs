// SPDX-License-Identifier: MIT

using AetherNet.Content;
using AetherMedia.Reel;
using Microsoft.Extensions.Logging;

namespace AetherMedia.Reel.Tests.Helpers;

/// <summary>
/// Subclass that routes persistence to a caller-supplied temp directory.
/// </summary>
internal sealed class IsolatedSoundLibrary(
    IContentService        content,
    string                 localUhid,
    string                 tempDir,
    ILogger<SoundLibrary>? logger = null)
    : SoundLibrary(content, localUhid, tempDir, logger);
