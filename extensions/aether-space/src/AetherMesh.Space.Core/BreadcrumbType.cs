// SPDX-License-Identifier: MIT
namespace AetherMesh.Space.Core;

/// <summary>
/// Classifies the purpose of a <see cref="SpaceBreadcrumb"/> dropped into the
/// Aether Space layer.  The numeric value is carried on the wire.
/// </summary>
public enum BreadcrumbType
{
    /// <summary>General-purpose notice or announcement.</summary>
    Notice = 0,

    /// <summary>
    /// Emergency alert.  Recipients must flood this type beyond the normal
    /// 3-cell radius constraint.
    /// </summary>
    Emergency = 1,

    /// <summary>Commercial listing or advertisement anchored to a location.</summary>
    Commerce = 2,

    /// <summary>Scheduled or live event.</summary>
    Event = 3,

    /// <summary>Job posting anchored to a physical location.</summary>
    JobPosting = 4,
}
