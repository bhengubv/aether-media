// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Crossfade;

/// <summary>Track-to-track transition controller.</summary>
public interface ICrossfade
{
    /// <summary>Current transition mode.</summary>
    CrossfadeMode Mode { get; set; }

    /// <summary>
    /// Duration of the crossfade window when <see cref="Mode"/> is
    /// <see cref="CrossfadeMode.Crossfade"/>. Typical: 2000–8000 ms.
    /// </summary>
    int FadeDurationMs { get; set; }

    /// <summary>
    /// Compute the gain ramp for a buffer using an equal-power sin/cos
    /// S-curve so combined energy of outgoing + incoming stays near 1.0.
    /// </summary>
    void ComputeGainRamp(
        int positionMs,
        int sampleCount,
        int sampleRateHz,
        bool fadingOut,
        Span<float> gains);
}
