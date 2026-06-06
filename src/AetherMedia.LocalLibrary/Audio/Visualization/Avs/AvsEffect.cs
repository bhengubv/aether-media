// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// Base class for every AVS effect runtime. Concrete subclasses parse their
/// binary parameter payload in the constructor and implement
/// <see cref="Render"/> to mutate the target frame.
/// </summary>
public abstract class AvsEffect
{
    /// <summary>True if the user enabled this effect at preset save time.</summary>
    public bool IsEnabled { get; protected set; } = true;

    /// <summary>Stable human-readable name for HUD / debugging.</summary>
    public abstract string DisplayName { get; }

    /// <summary>Run this effect for one frame.</summary>
    public abstract void Render(RgbaFrame target, AvsRenderContext context, in VisualizationInputs inputs);
}
