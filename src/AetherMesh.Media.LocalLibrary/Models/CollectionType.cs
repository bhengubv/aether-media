// SPDX-License-Identifier: MIT

namespace AetherMesh.Media.LocalLibrary.Models;

/// <summary>Whether a <see cref="MediaCollection"/> is hand-curated or rule-driven.</summary>
public enum CollectionType
{
    /// <summary>The user adds/removes items explicitly.  Order is preserved.</summary>
    Manual,

    /// <summary>Items are computed on-demand by evaluating a <see cref="SmartCollectionFilter"/>.</summary>
    Smart
}
