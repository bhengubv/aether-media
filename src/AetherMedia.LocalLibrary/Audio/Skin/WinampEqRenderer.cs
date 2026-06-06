// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Equalizer;
using AetherMedia.LocalLibrary.Audio.Visualization;

namespace AetherMedia.LocalLibrary.Audio.Skin;

/// <summary>
/// Paints the classic Winamp 2.x equaliser window into an <see cref="RgbaFrame"/>.
/// Uses <see cref="WinampSpriteAtlas"/> for sprites, <see cref="WinampEqWindowLayout"/>
/// for coordinates, and <see cref="IEqualizer.Bands"/> for the slider positions.
/// </summary>
public sealed class WinampEqRenderer : IVisualizationRenderer
{
    private readonly WinampSpriteAtlas _atlas;
    private readonly Func<bool> _isWindowActive;
    private readonly Func<IEqualizer> _equalizerProvider;

    public WinampEqRenderer(WinampSpriteAtlas atlas, Func<bool> isWindowActive, Func<IEqualizer> equalizerProvider)
    {
        _atlas = atlas ?? throw new ArgumentNullException(nameof(atlas));
        _isWindowActive = isWindowActive ?? throw new ArgumentNullException(nameof(isWindowActive));
        _equalizerProvider = equalizerProvider ?? throw new ArgumentNullException(nameof(equalizerProvider));
    }

    /// <inheritdoc/>
    public string DisplayName => $"Winamp EQ ({_atlas.Source.Name})";

    /// <inheritdoc/>
    public void Render(in VisualizationInputs inputs, RgbaFrame target)
    {
        ArgumentNullException.ThrowIfNull(target);

        // 1. Background + title bar.
        target.Clear(0, 0, 0, 0xFF);
        _atlas.Blit(WinampEqWindowLayout.Background, target, 0, 0);
        _atlas.Blit(_isWindowActive() ? WinampEqWindowLayout.TitleBarActive : WinampEqWindowLayout.TitleBarInactive, target, 0, 0);

        // 2. EQ band sliders. Map each band's gain (clamped to ±20 dB) onto
        // the documented 63-pixel slider travel; 0 dB sits dead-centre.
        var eq = _equalizerProvider();
        var bands = eq.Bands;
        var bandX = WinampEqWindowLayout.BandX;
        var travel = WinampEqWindowLayout.BandTravel;
        var topY = WinampEqWindowLayout.BandTopY;

        for (var i = 0; i < Math.Min(bands.Count, bandX.Count); i++)
        {
            var gainDb = bands[i].GainDb;
            var norm = Math.Clamp((gainDb + 20.0) / 40.0, 0.0, 1.0); // -20..+20 → 0..1
            var y = topY + (int)((1.0 - norm) * travel);

            // Draw a small rail underneath the thumb.
            _atlas.Blit(WinampEqWindowLayout.SliderRail, target, bandX[i] - 2, topY);
            // Slider thumb.
            _atlas.Blit(WinampEqWindowLayout.SliderThumb, target, bandX[i] - 5, y);
        }
    }
}
