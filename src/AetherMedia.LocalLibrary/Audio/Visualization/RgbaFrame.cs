// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization;

/// <summary>
/// A mutable RGBA8888 frame buffer (row-major, top-left origin) sized for a
/// visualization renderer to draw into. Memory is owned by the buffer so a
/// renderer can hand it to any GPU/CPU upload path (SkiaSharp.Image,
/// Avalonia.WriteableBitmap, GL texture upload) without an extra copy.
/// </summary>
public sealed class RgbaFrame
{
    /// <summary>Allocate a fresh frame.</summary>
    public RgbaFrame(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        Width = width;
        Height = height;
        Pixels = new byte[width * height * 4];
    }

    /// <summary>Frame width in pixels.</summary>
    public int Width { get; }

    /// <summary>Frame height in pixels.</summary>
    public int Height { get; }

    /// <summary>Stride in bytes (always Width × 4 — no padding).</summary>
    public int Stride => Width * 4;

    /// <summary>Raw pixel data — RGBA8888.</summary>
    public byte[] Pixels { get; }

    /// <summary>Clear to <paramref name="rgba"/>.</summary>
    public void Clear(byte r, byte g, byte b, byte a)
    {
        var px = Pixels;
        for (var i = 0; i < px.Length; i += 4)
        {
            px[i] = r; px[i + 1] = g; px[i + 2] = b; px[i + 3] = a;
        }
    }

    /// <summary>Set one pixel (no bounds check — caller guarantees x/y in range).</summary>
    public void SetPixel(int x, int y, byte r, byte g, byte b, byte a)
    {
        var i = (y * Width + x) * 4;
        Pixels[i] = r;
        Pixels[i + 1] = g;
        Pixels[i + 2] = b;
        Pixels[i + 3] = a;
    }
}
