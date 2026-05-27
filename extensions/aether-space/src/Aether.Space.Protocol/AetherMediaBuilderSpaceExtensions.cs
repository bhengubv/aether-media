// SPDX-License-Identifier: MIT

using Aether.Media.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aether.Space.Protocol;

/// <summary>
/// Extends <see cref="AetherMediaBuilder"/> with aether-space capability registration.
///
/// <para>Usage:</para>
/// <code>
/// services.AddAetherMedia(aether =>
///     aether
///         .AddContent()
///         .AddSocial()
///         .AddSpace());
/// </code>
/// </summary>
public static class AetherMediaBuilderSpaceExtensions
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
    /// Node capability announced during handshake: <c>aether.space/v1</c>
    /// (see <see cref="SpaceCapabilityConstants.V1"/>).
    /// </para>
    /// </summary>
    public static AetherMediaBuilder AddSpace(this AetherMediaBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<SpaceBreadcrumbHandler>();

        return builder;
    }
}
