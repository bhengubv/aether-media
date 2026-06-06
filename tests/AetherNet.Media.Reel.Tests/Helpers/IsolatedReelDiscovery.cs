// SPDX-License-Identifier: MIT

using AetherNet.Media.Reel;
using Microsoft.Extensions.Logging;

namespace AetherNet.Media.Reel.Tests.Helpers;

/// <summary>
/// Subclass that routes persistence to a caller-supplied temp directory.
/// </summary>
internal sealed class IsolatedReelDiscovery(string tempDir, ILogger<ReelDiscovery>? logger = null)
    : ReelDiscovery(tempDir, logger);
