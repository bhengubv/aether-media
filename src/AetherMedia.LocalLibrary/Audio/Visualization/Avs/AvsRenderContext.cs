// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// Per-frame state shared between every effect in an AVS chain. Holds the
/// 8 named buffers AVS uses for cross-effect frame storage (the "Buffer
/// Save" effect writes here; subsequent effects can read).
/// </summary>
public sealed class AvsRenderContext
{
    /// <summary>Number of save buffers AVS supports.</summary>
    public const int BufferCount = 8;

    private readonly byte[]?[] _buffers = new byte[BufferCount][];

    public AvsRenderContext(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }

    /// <summary>Wall-clock seconds since preset start — used by NS-EEL scripts.</summary>
    public double TimeSeconds { get; set; }

    /// <summary>Frame index — used by NS-EEL scripts.</summary>
    public long FrameIndex { get; set; }

    /// <summary>Bass energy (Milkdrop-like 0..3 scale).</summary>
    public double Bass { get; set; }
    public double Mid { get; set; }
    public double Treb { get; set; }

    /// <summary>True when the frame is the first after a beat detection.</summary>
    public bool OnBeat { get; set; }

    /// <summary>Pull the saved buffer at slot (1..8), or null if not saved yet.</summary>
    public byte[]? GetBuffer(int slot)
    {
        if (slot < 1 || slot > BufferCount) return null;
        return _buffers[slot - 1];
    }

    /// <summary>Copy <paramref name="frame"/>'s pixels into save slot (1..8).</summary>
    public void SaveBuffer(int slot, RgbaFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (slot < 1 || slot > BufferCount)
            throw new ArgumentOutOfRangeException(nameof(slot));
        var buf = _buffers[slot - 1];
        if (buf is null || buf.Length != frame.Pixels.Length)
            _buffers[slot - 1] = buf = new byte[frame.Pixels.Length];
        Buffer.BlockCopy(frame.Pixels, 0, buf, 0, buf.Length);
    }
}
