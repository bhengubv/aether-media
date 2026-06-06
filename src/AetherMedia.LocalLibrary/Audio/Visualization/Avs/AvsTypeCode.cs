// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// Documented AVS effect type codes from the Nullsoft AVS source. Each
/// constant matches the 32-bit integer prefix the parser reads from the
/// preset binary.
/// </summary>
public static class AvsTypeCode
{
    public const int EffectList               = unchecked((int)0xFFFFFFFE); // -2 unsigned
    public const int SimpleSpectrum           = 0x01;
    public const int DotPlane                 = 0x02;
    public const int OscilloscopeStar         = 0x03;
    public const int Fadeout                  = 0x04;
    public const int BlitterFeedback          = 0x05;
    public const int OnBeatClear              = 0x06;
    public const int Blur                     = 0x07;
    public const int BassSpin                 = 0x08;
    public const int MovingParticle           = 0x09;
    public const int RotoBlitter              = 0x0A;
    public const int SvpLoader                = 0x0B;
    public const int ColorFade                = 0x0C;
    public const int ContrastEnhancement      = 0x0D;
    public const int RotatingStars            = 0x0E;
    public const int Ring                     = 0x0F;
    public const int Movement                 = 0x10;
    public const int Scatter                  = 0x11;
    public const int DotGrid                  = 0x12;
    public const int BufferSave               = 0x13;
    public const int DotFountain              = 0x14;
    public const int Water                    = 0x15;
    public const int Comment                  = 0x16;
    public const int Brightness               = 0x17;
    public const int Interleave               = 0x18;
    public const int Grain                    = 0x19;
    public const int ClearScreen              = 0x1A;
    public const int Mirror                   = 0x1B;
    public const int Starfield                = 0x1C;
    public const int Text                     = 0x1D;
    public const int Bumpmap                  = 0x1E;
    public const int Mosaic                   = 0x1F;
    public const int WaterBump                = 0x20;
    public const int Avi                      = 0x21;
    public const int CustomBpm                = 0x22;
    public const int Picture                  = 0x23;
    public const int DynamicDistanceModifier  = 0x24;
    public const int SuperScope               = 0x25;
    public const int Invert                   = 0x26;
    public const int UniqueTone               = 0x27;
    public const int TimeDomainScope          = 0x28;
    public const int ChannelShift             = 0x29;
    public const int ColorReduction           = 0x2A;
    public const int MultiDelay               = 0x2B;
    public const int VideoDelay               = 0x2C;
    public const int DynamicMovement          = 0x2D;
    public const int Multiplier               = 0x2E;
    public const int Onetone                  = 0x2F;
}
