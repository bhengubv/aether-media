// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Loudness;

/// <summary>
/// ITU-R BS.1770 K-weighting pre-filter: a two-stage cascaded biquad
/// approximating the frequency response of the human auditory system at
/// moderate listening levels.
///
/// <para>
/// Stage 1 is a +4 dB high-shelf centred at ~1681 Hz (outer-ear head-related
/// transfer); stage 2 is a high-pass / "RLB" filter centred at ~38 Hz (low-end
/// hearing roll-off compensation).
/// </para>
/// <para>
/// Reference coefficients are specified at 48 kHz in the standard. For other
/// sample rates we de-warp the bilinear transform and recompute, preserving
/// the analogue-prototype response.
/// </para>
/// </summary>
internal sealed class KWeightingFilter
{
    private double _s1B0, _s1B1, _s1B2, _s1A1, _s1A2;
    private double _s2B0, _s2B1, _s2B2, _s2A1, _s2A2;

    private readonly double[] _s1X1, _s1X2, _s1Y1, _s1Y2;
    private readonly double[] _s2X1, _s2X2, _s2Y1, _s2Y2;

    public KWeightingFilter(int sampleRateHz, int channels)
    {
        ComputeCoefficients(sampleRateHz);
        _s1X1 = new double[channels];
        _s1X2 = new double[channels];
        _s1Y1 = new double[channels];
        _s1Y2 = new double[channels];
        _s2X1 = new double[channels];
        _s2X2 = new double[channels];
        _s2Y1 = new double[channels];
        _s2Y2 = new double[channels];
    }

    /// <summary>Apply the cascaded K-weighting filter to one sample.</summary>
    public double ProcessSample(double x, int channel)
    {
        var y1 = _s1B0 * x
               + _s1B1 * _s1X1[channel]
               + _s1B2 * _s1X2[channel]
               - _s1A1 * _s1Y1[channel]
               - _s1A2 * _s1Y2[channel];
        _s1X2[channel] = _s1X1[channel];
        _s1X1[channel] = x;
        _s1Y2[channel] = _s1Y1[channel];
        _s1Y1[channel] = y1;

        var y2 = _s2B0 * y1
               + _s2B1 * _s2X1[channel]
               + _s2B2 * _s2X2[channel]
               - _s2A1 * _s2Y1[channel]
               - _s2A2 * _s2Y2[channel];
        _s2X2[channel] = _s2X1[channel];
        _s2X1[channel] = y1;
        _s2Y2[channel] = _s2Y1[channel];
        _s2Y1[channel] = y2;
        return y2;
    }

    private void ComputeCoefficients(int sampleRateHz)
    {
        // Reference coefficients at fs = 48 kHz (ITU-R BS.1770-4 Annex 1)
        const double s1B0_48k = 1.53512485958697;
        const double s1B1_48k = -2.69169618940638;
        const double s1B2_48k = 1.19839281085285;
        const double s1A1_48k = -1.69065929318241;
        const double s1A2_48k = 0.73248077421585;
        const double s2B0_48k = 1.0;
        const double s2B1_48k = -2.0;
        const double s2B2_48k = 1.0;
        const double s2A1_48k = -1.99004745483398;
        const double s2A2_48k = 0.99007225036621;

        if (sampleRateHz == 48000)
        {
            _s1B0 = s1B0_48k; _s1B1 = s1B1_48k; _s1B2 = s1B2_48k;
            _s1A1 = s1A1_48k; _s1A2 = s1A2_48k;
            _s2B0 = s2B0_48k; _s2B1 = s2B1_48k; _s2B2 = s2B2_48k;
            _s2A1 = s2A1_48k; _s2A2 = s2A2_48k;
            return;
        }

        DeBilinearAndRewarp(s1B0_48k, s1B1_48k, s1B2_48k, s1A1_48k, s1A2_48k,
            48000.0, sampleRateHz,
            out _s1B0, out _s1B1, out _s1B2, out _s1A1, out _s1A2);

        DeBilinearAndRewarp(s2B0_48k, s2B1_48k, s2B2_48k, s2A1_48k, s2A2_48k,
            48000.0, sampleRateHz,
            out _s2B0, out _s2B1, out _s2B2, out _s2A1, out _s2A2);
    }

    private static void DeBilinearAndRewarp(
        double b0, double b1, double b2, double a1, double a2,
        double sourceFs, double targetFs,
        out double newB0, out double newB1, out double newB2,
        out double newA1, out double newA2)
    {
        var (zR1Re, zR1Im, zR2Re, zR2Im) = QuadraticRoots(b0, b1, b2);
        var (zP1Re, zP1Im, zP2Re, zP2Im) = QuadraticRoots(1.0, a1, a2);

        var (sR1Re, sR1Im) = InverseBilinear(zR1Re, zR1Im, sourceFs);
        var (sR2Re, sR2Im) = InverseBilinear(zR2Re, zR2Im, sourceFs);
        var (sP1Re, sP1Im) = InverseBilinear(zP1Re, zP1Im, sourceFs);
        var (sP2Re, sP2Im) = InverseBilinear(zP2Re, zP2Im, sourceFs);

        var (zR1ReN, zR1ImN) = ForwardBilinear(sR1Re, sR1Im, targetFs);
        var (zR2ReN, zR2ImN) = ForwardBilinear(sR2Re, sR2Im, targetFs);
        var (zP1ReN, zP1ImN) = ForwardBilinear(sP1Re, sP1Im, targetFs);
        var (zP2ReN, zP2ImN) = ForwardBilinear(sP2Re, sP2Im, targetFs);

        var num = PolyFromConjugatePair(zR1ReN, zR1ImN, zR2ReN, zR2ImN);
        var den = PolyFromConjugatePair(zP1ReN, zP1ImN, zP2ReN, zP2ImN);

        var sourceDc = (b0 + b1 + b2) / (1.0 + a1 + a2);
        var targetDc = (num[0] + num[1] + num[2]) / (den[0] + den[1] + den[2]);
        var k = sourceDc / targetDc;

        var a0 = den[0];
        newB0 = num[0] * k / a0;
        newB1 = num[1] * k / a0;
        newB2 = num[2] * k / a0;
        newA1 = den[1] / a0;
        newA2 = den[2] / a0;
    }

    private static (double, double, double, double) QuadraticRoots(double a, double b, double c)
    {
        var disc = b * b - 4.0 * a * c;
        if (disc >= 0)
        {
            var sq = Math.Sqrt(disc);
            return ((-b + sq) / (2 * a), 0.0, (-b - sq) / (2 * a), 0.0);
        }
        var sqI = Math.Sqrt(-disc);
        return (-b / (2 * a), sqI / (2 * a), -b / (2 * a), -sqI / (2 * a));
    }

    private static (double re, double im) InverseBilinear(double zRe, double zIm, double fs)
    {
        var numRe = zRe - 1.0; var numIm = zIm;
        var denRe = zRe + 1.0; var denIm = zIm;
        var denMag = denRe * denRe + denIm * denIm;
        var re = (numRe * denRe + numIm * denIm) / denMag;
        var im = (numIm * denRe - numRe * denIm) / denMag;
        return (2.0 * fs * re, 2.0 * fs * im);
    }

    private static (double re, double im) ForwardBilinear(double sRe, double sIm, double fs)
    {
        var numRe = 2.0 * fs + sRe; var numIm = sIm;
        var denRe = 2.0 * fs - sRe; var denIm = -sIm;
        var denMag = denRe * denRe + denIm * denIm;
        var re = (numRe * denRe + numIm * denIm) / denMag;
        var im = (numIm * denRe - numRe * denIm) / denMag;
        return (re, im);
    }

    private static double[] PolyFromConjugatePair(double r1Re, double r1Im, double r2Re, double r2Im)
    {
        var sumRe = r1Re + r2Re;
        var prodRe = r1Re * r2Re - r1Im * r2Im;
        return [1.0, -sumRe, prodRe];
    }
}
