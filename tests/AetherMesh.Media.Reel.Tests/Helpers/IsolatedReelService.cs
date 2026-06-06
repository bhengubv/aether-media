// SPDX-License-Identifier: MIT

using AetherMesh.Content;
using AetherMesh.Media.Reel;
using AetherMesh.Media.Reel.Interfaces;
using Microsoft.Extensions.Logging;

namespace AetherMesh.Media.Reel.Tests.Helpers;

/// <summary>
/// Subclass that routes persistence to a caller-supplied temp directory.
/// </summary>
internal sealed class IsolatedReelService(
    IContentService       content,
    IReelDiscovery        discovery,
    string                localUhid,
    string                tempDir,
    ILogger<ReelService>? logger = null)
    : ReelService(content, discovery, localUhid, tempDir, logger);
