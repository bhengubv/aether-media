// SPDX-License-Identifier: MIT

using Aether.Media.Reel;
using Microsoft.Extensions.Logging;

namespace Aether.Media.Reel.Tests.Helpers;

/// <summary>
/// Subclass that routes persistence to a caller-supplied temp directory so tests
/// never touch the real AppData folder and run safely in parallel.
/// </summary>
internal sealed class IsolatedReelEngagementTracker(string tempDir, ILogger<ReelEngagementTracker>? logger = null)
    : ReelEngagementTracker(tempDir, logger);
