// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Visualization;

namespace AetherMedia.LocalLibrary.Audio.Skin;

/// <summary>
/// Player state snapshot consumed by <see cref="WinampMainWindowRenderer"/>.
/// </summary>
public sealed record WinampPlayerState(
    WinampPlaybackStatus Status,
    bool WindowActive,
    string? CurrentTrack,
    double PositionFraction,
    double VolumeFraction);

/// <summary>Coarse playback state — enough to pick which transport button is "pressed".</summary>
public enum WinampPlaybackStatus
{
    Stopped,
    Playing,
    Paused,
}

/// <summary>
/// Paints the classic Winamp main window into an <see cref="RgbaFrame"/>
/// using a decoded <see cref="WinampSpriteAtlas"/>. Implements the
/// <see cref="IVisualizationRenderer"/> contract so any UI host that already
/// blits a visualization framebuffer can show a Winamp skin too — that is
/// what "wired into the UI" means in practice for this library; the
/// Avalonia view in <c>AetherMedia.Desktop</c> already consumes
/// <see cref="RgbaFrame"/> output.
/// </summary>
public sealed class WinampMainWindowRenderer : IVisualizationRenderer
{
    private readonly WinampSpriteAtlas _atlas;
    private readonly Func<WinampPlayerState> _stateProvider;

    /// <summary>Construct with a fixed atlas and a callback that returns the live player state each frame.</summary>
    public WinampMainWindowRenderer(WinampSpriteAtlas atlas, Func<WinampPlayerState> stateProvider)
    {
        _atlas = atlas ?? throw new ArgumentNullException(nameof(atlas));
        _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
    }

    /// <inheritdoc/>
    public string DisplayName => $"Winamp Skin: {_atlas.Source.Name}";

    /// <inheritdoc/>
    public void Render(in VisualizationInputs inputs, RgbaFrame target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var state = _stateProvider();

        // 1. Background (main.bmp).
        target.Clear(0, 0, 0, 0xFF);
        _atlas.Blit(WinampMainWindowLayout.Background, target, 0, 0);

        // 2. Title bar — pick active vs inactive variant.
        var titleSlice = state.WindowActive
            ? WinampMainWindowLayout.TitleBarActive
            : WinampMainWindowLayout.TitleBarInactive;
        _atlas.Blit(titleSlice, target, WinampMainWindowLayout.TitleBarOrigin.X, WinampMainWindowLayout.TitleBarOrigin.Y);

        // 3. Transport buttons.
        var (bx, by) = WinampMainWindowLayout.ButtonsOrigin;
        var sliceList = new (WinampSpriteSlice Up, WinampSpriteSlice Down)[]
        {
            (WinampMainWindowLayout.ButtonPrevUp,  WinampMainWindowLayout.ButtonPrevDown),
            (WinampMainWindowLayout.ButtonPlayUp,  WinampMainWindowLayout.ButtonPlayDown),
            (WinampMainWindowLayout.ButtonPauseUp, WinampMainWindowLayout.ButtonPauseDown),
            (WinampMainWindowLayout.ButtonStopUp,  WinampMainWindowLayout.ButtonStopDown),
            (WinampMainWindowLayout.ButtonNextUp,  WinampMainWindowLayout.ButtonNextDown),
        };
        // Which button is pressed reflects current playback status.
        var pressedIndex = state.Status switch
        {
            WinampPlaybackStatus.Playing => 1,
            WinampPlaybackStatus.Paused  => 2,
            WinampPlaybackStatus.Stopped => 3,
            _ => -1,
        };
        var xCursor = bx;
        for (var i = 0; i < sliceList.Length; i++)
        {
            var (up, down) = sliceList[i];
            var slice = i == pressedIndex ? down : up;
            _atlas.Blit(slice, target, xCursor, by);
            xCursor += up.Width;
        }
    }
}
