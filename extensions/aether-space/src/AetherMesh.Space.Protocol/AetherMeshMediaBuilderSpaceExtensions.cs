// SPDX-License-Identifier: MIT

using AetherMesh.Media.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AetherMesh.Space.Protocol;

/// <summary>
/// Extends <see cref="AetherMeshMediaBuilder"/> with aether-space capability registration.
///
/// <para>Usage:</para>
/// <code>
/// services.AddAetherMeshMedia(aether =>
///     aether
///         .AddContent()
///         .AddSocial()
///         .AddSpace());
/// </code>
/// </summary>
public static class AetherMeshMediaBuilderSpaceExtensions
{
    /// <summary>
    /// Registers aether-space services:
    /// <list type="bullet">
    ///   <item><see cref="SpaceBreadcrumbHandler"/> — handles incoming <c>SpaceBreadcrumb(40)</c>
    ///         packets, enforces the 3-cell geohash radius filter, prunes expired breadcrumbs,
    ///         and floods Emergency-type breadcrumbs to all reachable peers.</item>
    /// </list>
    ///
    /// <para>
    /// An <see cref="ISpaceService"/> implementation must be registered separately — either by
    /// a platform-specific package or directly by the host application — before the first
    /// breadcrumb operation is requested.
    /// </para>
    ///
    /// <para>
    /// Node capability announced during handshake: <c>aethermesh.space/v1</c>
    /// (see <see cref="SpaceCapabilityConstants.V1"/>).
    /// </para>
    /// </summary>
    public static AetherMeshMediaBuilder AddSpace(this AetherMeshMediaBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<SpaceBreadcrumbHandler>();

        return builder;
    }
}
