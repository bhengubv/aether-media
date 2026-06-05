// SPDX-License-Identifier: MIT

using AetherMesh.Models;
using AetherMesh.Protocol;
using AetherMesh.Routing;

namespace Aether.Media.AI.Tests.Helpers;

/// <summary>
/// Minimal IRoutingService stub that records FindRouteAsync calls.
/// Returns null (no route found) by default; can be configured to throw.
/// </summary>
internal sealed class FakeRoutingService : IRoutingService
{
    /// <summary>UHIDs passed to FindRouteAsync, in call order.</summary>
    public List<string> FindRouteCalls { get; } = [];

    /// <summary>When true, FindRouteAsync throws <see cref="InvalidOperationException"/>.</summary>
    public bool ThrowOnFind { get; set; }

    public Task<RouteEntry?> FindRouteAsync(
        string destinationUhid,
        CancellationToken cancellationToken = default)
    {
        FindRouteCalls.Add(destinationUhid);

        if (ThrowOnFind)
            throw new InvalidOperationException("Simulated routing failure");

        return Task.FromResult<RouteEntry?>(null);
    }

    public RouteEntry? GetCachedRoute(string destinationUhid) => null;

    public IReadOnlyList<RouteEntry> GetAllRoutes() => Array.Empty<RouteEntry>();

    public Task HandleRouteRequestAsync(MeshPacket routeRequest,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task HandleRouteReplyAsync(MeshPacket routeReply,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task PruneAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
