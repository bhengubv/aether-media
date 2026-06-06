// SPDX-License-Identifier: MIT

using AetherNet.Content;
using AetherNet.Media.Reel;
using Microsoft.Extensions.Logging;

namespace AetherNet.Media.Reel.Tests.Helpers;

/// <summary>
/// Subclass that routes persistence to a caller-supplied temp directory.
/// </summary>
internal sealed class IsolatedSoundLibrary(
    IContentService        content,
    string                 localUhid,
    string                 tempDir,
    ILogger<SoundLibrary>? logger = null)
    : SoundLibrary(content, localUhid, tempDir, logger);
