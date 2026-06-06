// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Crossfade;

/// <summary>
/// Default <see cref="ICrossfade"/> using an equal-power sin/cos S-curve.
/// </summary>
public sealed class CrossfadeController : ICrossfade
{
    /// <inheritdoc/>
    public CrossfadeMode Mode { get; set; } = CrossfadeMode.Gapless;

    /// <inheritdoc/>
    public int FadeDurationMs { get; set; } = 4000;

    /// <inheritdoc/>
    public void ComputeGainRamp(
        int positionMs,
        int sampleCount,
        int sampleRateHz,
        bool fadingOut,
        Span<float> gains)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(positionMs);
        ArgumentOutOfRangeException.ThrowIfNegative(sampleCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRateHz);

        if (gains.Length < sampleCount)
            throw new ArgumentException(
                "Destination gain span is shorter than sampleCount.", nameof(gains));

        if (Mode != CrossfadeMode.Crossfade || FadeDurationMs <= 0)
        {
            gains[..sampleCount].Fill(fadingOut ? 0f : 1f);
            return;
        }

        var fadeDurationSamples = (int)((long)FadeDurationMs * sampleRateHz / 1000L);
        if (fadeDurationSamples <= 0)
        {
            gains[..sampleCount].Fill(fadingOut ? 0f : 1f);
            return;
        }

        var startSample = (int)((long)positionMs * sampleRateHz / 1000L);
        for (var i = 0; i < sampleCount; i++)
        {
            var s = startSample + i;
            if (s <= 0)               { gains[i] = fadingOut ? 1f : 0f; continue; }
            if (s >= fadeDurationSamples) { gains[i] = fadingOut ? 0f : 1f; continue; }

            var t = (double)s / fadeDurationSamples;
            gains[i] = fadingOut
                ? (float)Math.Cos(t * Math.PI / 2.0)
                : (float)Math.Sin(t * Math.PI / 2.0);
        }
    }
}
