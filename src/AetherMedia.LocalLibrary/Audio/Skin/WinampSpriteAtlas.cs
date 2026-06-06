// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Visualization;

namespace AetherMedia.LocalLibrary.Audio.Skin;

/// <summary>
/// Decoded sprite atlas built from a <see cref="WinampClassicSkin"/>. Each
/// BMP in the skin is materialised as an <see cref="RgbaFrame"/> keyed by
/// its base name (case-insensitive). UI layers consume slices by name +
/// rectangle (<see cref="WinampSpriteSlice"/>).
/// </summary>
public sealed class WinampSpriteAtlas
{
    private readonly IReadOnlyDictionary<string, RgbaFrame> _sprites;

    private WinampSpriteAtlas(IReadOnlyDictionary<string, RgbaFrame> sprites, WinampClassicSkin source)
    {
        _sprites = sprites;
        Source = source;
    }

    /// <summary>The skin model this atlas was built from.</summary>
    public WinampClassicSkin Source { get; }

    /// <summary>All decoded sprite names, lower-cased.</summary>
    public IReadOnlyCollection<string> SpriteNames => _sprites.Keys.ToList();

    /// <summary>
    /// Decode every BMP in <paramref name="skin"/> into an RGBA frame. Skips
    /// any BMP that fails to decode (logged on the caller side via try/catch
    /// if needed) so a single corrupt sprite doesn't doom the whole skin.
    /// </summary>
    public static WinampSpriteAtlas FromSkin(WinampClassicSkin skin)
    {
        ArgumentNullException.ThrowIfNull(skin);
        var decoded = new Dictionary<string, RgbaFrame>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, bytes) in skin.Sprites)
        {
            try { decoded[name] = BmpDecoder.Decode(bytes); }
            catch (FormatException) { /* sprite unusable — fall through */ }
        }
        return new WinampSpriteAtlas(decoded, skin);
    }

    /// <summary>Try to fetch the full decoded sprite by name (extensionless).</summary>
    public RgbaFrame? TryGetSprite(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return _sprites.TryGetValue(name, out var f) ? f : null;
    }

    /// <summary>
    /// Copy the rectangle described by <paramref name="slice"/> from the
    /// referenced sprite into <paramref name="target"/> at <paramref name="destX"/>,
    /// <paramref name="destY"/>. Out-of-range pixels are clipped.
    /// </summary>
    public void Blit(WinampSpriteSlice slice, RgbaFrame target, int destX, int destY)
    {
        ArgumentNullException.ThrowIfNull(slice);
        ArgumentNullException.ThrowIfNull(target);

        var sprite = TryGetSprite(slice.SpriteName);
        if (sprite is null) return;

        for (var sy = 0; sy < slice.Height; sy++)
        {
            var py = destY + sy;
            var srcY = slice.Y + sy;
            if (py < 0 || py >= target.Height) continue;
            if (srcY < 0 || srcY >= sprite.Height) continue;

            for (var sx = 0; sx < slice.Width; sx++)
            {
                var px = destX + sx;
                var srcX = slice.X + sx;
                if (px < 0 || px >= target.Width) continue;
                if (srcX < 0 || srcX >= sprite.Width) continue;

                var si = (srcY * sprite.Width + srcX) * 4;
                target.SetPixel(px, py,
                    sprite.Pixels[si],
                    sprite.Pixels[si + 1],
                    sprite.Pixels[si + 2],
                    sprite.Pixels[si + 3]);
            }
        }
    }
}
