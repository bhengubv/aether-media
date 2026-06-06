// SPDX-License-Identifier: MIT

using AetherMesh.Media.Reel;
using Microsoft.Extensions.Logging;

namespace AetherMesh.Media.Reel.Tests.Helpers;

/// <summary>
/// Subclass that routes persistence to a caller-supplied temp directory.
/// </summary>
internal sealed class IsolatedReelDiscovery(string tempDir, ILogger<ReelDiscovery>? logger = null)
    : ReelDiscovery(tempDir, logger);
