// SPDX-License-Identifier: MIT

namespace Aether.Media.Core;

/// <summary>
/// <see cref="INetworkPolicy"/> for desktop platforms and unit tests where metered
/// connections are not tracked.  Always reports an unmetered connection so that
/// seeding and mesh scanning are never suppressed by default on non-mobile targets.
/// </summary>
public sealed class NullNetworkPolicy : INetworkPolicy
{
    /// <summary>Shared singleton — allocation-free.</summary>
    public static readonly NullNetworkPolicy Instance = new();

    /// <inheritdoc/>
    /// <remarks>Always <c>false</c> — never metered.</remarks>
    public bool IsMetered => false;
}
