// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization;

/// <summary>
/// Renders one audio frame into a <see cref="RgbaFrame"/>. The renderer
/// chooses how to use the available <see cref="VisualizationInputs"/> —
/// oscilloscope renderers read time-domain samples, bar / spectrum
/// renderers read the FFT result.
///
/// <para>
/// This is the Winamp <c>vis_*.dll</c> contract minus the per-plugin window
/// chrome — modern UIs own the surface lifecycle and call <see cref="Render"/>
/// each animation frame.
/// </para>
/// </summary>
public interface IVisualizationRenderer
{
    /// <summary>Human-readable name shown in a renderer picker.</summary>
    string DisplayName { get; }

    /// <summary>Draw one frame.</summary>
    void Render(in VisualizationInputs inputs, RgbaFrame target);
}
