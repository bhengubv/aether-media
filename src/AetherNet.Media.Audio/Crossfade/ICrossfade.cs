// SPDX-License-Identifier: MIT

namespace AetherNet.Media.Audio.Crossfade;

/// <summary>
/// Track-to-track transition controller. Players query this on the
/// <c>MediaEnded</c> event to decide how to blend into the next item.
/// </summary>
public interface ICrossfade
{
    /// <summary>Current transition mode.</summary>
    CrossfadeMode Mode { get; set; }

    /// <summary>
    /// Duration of the crossfade window when <see cref="Mode"/> is
    /// <see cref="CrossfadeMode.Crossfade"/>. Ignored for the other modes.
    /// Typical values are 2 000–8 000 ms; 4 000 ms feels natural for most material.
    /// </summary>
    int FadeDurationMs { get; set; }

    /// <summary>
    /// Compute the gain ramp for a buffer of <paramref name="sampleCount"/> samples
    /// starting at <paramref name="positionMs"/> into the fade window. Returns
    /// gains in the range [0.0, 1.0] using an equal-power S-curve so the
    /// combined energy of A+B stays roughly constant through the transition.
    /// </summary>
    /// <param name="positionMs">Milliseconds elapsed since the fade began.</param>
    /// <param name="sampleCount">Number of samples to produce gains for.</param>
    /// <param name="sampleRateHz">Sample rate of the audio.</param>
    /// <param name="fadingOut"><c>true</c> for the outgoing track, <c>false</c> for the incoming.</param>
    /// <param name="gains">Destination span for the per-sample gain ramp.</param>
    void ComputeGainRamp(
        int positionMs,
        int sampleCount,
        int sampleRateHz,
        bool fadingOut,
        Span<float> gains);
}
