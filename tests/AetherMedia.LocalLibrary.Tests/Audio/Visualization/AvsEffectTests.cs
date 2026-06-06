// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using AetherMedia.LocalLibrary.Audio.Visualization;
using AetherMedia.LocalLibrary.Audio.Visualization.Avs;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Visualization;

public class AvsEffectTests
{
    [Fact]
    public void Invert_FlipsEveryColourChannel()
    {
        var fx = new AvsInvertEffect(BuildPayload(1));
        var frame = MakeFrame(4, 4, r: 10, g: 100, b: 200, a: 255);
        fx.Render(frame, new AvsRenderContext(4, 4), new VisualizationInputs());

        Assert.Equal(245, frame.Pixels[0]);
        Assert.Equal(155, frame.Pixels[1]);
        Assert.Equal(55,  frame.Pixels[2]);
    }

    [Fact]
    public void ClearScreen_FillsWithConfiguredColour()
    {
        // BGR: R=0xFF, G=0x80, B=0x00 → encoded as 0xFF8000
        var fx = new AvsClearScreenEffect(BuildPayload(1, 0xFF8000, 0, 0));
        var frame = MakeFrame(4, 4);
        fx.Render(frame, new AvsRenderContext(4, 4), new VisualizationInputs());

        Assert.Equal(0xFF, frame.Pixels[0]);
        Assert.Equal(0x80, frame.Pixels[1]);
        Assert.Equal(0x00, frame.Pixels[2]);
    }

    [Fact]
    public void Mirror_HorizontalCopiesLeftToRight()
    {
        var fx = new AvsMirrorEffect(BuildPayload(1, 0x01));
        var frame = MakeFrame(4, 1);
        // Left half: green; right half (to be overwritten): red.
        SetPixel(frame, 0, 0, 0, 255, 0);
        SetPixel(frame, 1, 0, 0, 255, 0);
        SetPixel(frame, 2, 0, 255, 0, 0);
        SetPixel(frame, 3, 0, 255, 0, 0);

        fx.Render(frame, new AvsRenderContext(4, 1), new VisualizationInputs());

        // After mirror: right side should equal mirrored left side (green).
        Assert.Equal(0, frame.Pixels[(0 * 4 + 3) * 4]);     // R of x=3
        Assert.Equal(255, frame.Pixels[(0 * 4 + 3) * 4 + 1]); // G of x=3
    }

    [Fact]
    public void Mosaic_BlocksOutTopLeftPixelAcrossNeighbours()
    {
        // Quality 0 → block size = (100 - 0) / 2 + 1 = 51 → the whole frame is
        // one block in a 4×4 image; every pixel becomes the top-left value.
        var fx = new AvsMosaicEffect(BuildPayload(1, 0));
        var frame = MakeFrame(4, 4);
        SetPixel(frame, 0, 0, 200, 100, 50);
        SetPixel(frame, 3, 3, 0, 0, 0);

        fx.Render(frame, new AvsRenderContext(4, 4), new VisualizationInputs());

        Assert.Equal(200, frame.Pixels[(3 * 4 + 3) * 4]);
        Assert.Equal(100, frame.Pixels[(3 * 4 + 3) * 4 + 1]);
        Assert.Equal(50,  frame.Pixels[(3 * 4 + 3) * 4 + 2]);
    }

    [Fact]
    public void Brightness_AddsDeltaPerChannel()
    {
        var fx = new AvsBrightnessEffect(BuildPayload(1, 0, 10, 20, 30, 0, 0));
        var frame = MakeFrame(2, 2, r: 50, g: 50, b: 50, a: 255);
        fx.Render(frame, new AvsRenderContext(2, 2), new VisualizationInputs());

        Assert.Equal(60,  frame.Pixels[0]);
        Assert.Equal(70,  frame.Pixels[1]);
        Assert.Equal(80,  frame.Pixels[2]);
    }

    [Fact]
    public void Fadeout_NudgesEveryChannelTowardTarget()
    {
        // Fade length 1 → step = 92; fade colour = black (0).
        var fx = new AvsFadeoutEffect(BuildPayload(1, 1, 0));
        var frame = MakeFrame(2, 2, r: 200, g: 200, b: 200, a: 255);
        fx.Render(frame, new AvsRenderContext(2, 2), new VisualizationInputs());

        Assert.InRange(frame.Pixels[0], 100, 110);
        Assert.InRange(frame.Pixels[1], 100, 110);
    }

    [Fact]
    public void BufferSave_RoundTripsFrameThroughSlot()
    {
        var ctx = new AvsRenderContext(4, 4);
        var frame = MakeFrame(4, 4, r: 200, g: 100, b: 50, a: 255);

        var save = new AvsBufferSaveEffect(BuildPayload(1, (int)AvsBufferSaveEffect.Operation.Save, 1, 0));
        var restore = new AvsBufferSaveEffect(BuildPayload(1, (int)AvsBufferSaveEffect.Operation.Restore, 1, 0));

        save.Render(frame, ctx, new VisualizationInputs());

        // Mutate the frame after saving.
        for (var i = 0; i < frame.Pixels.Length; i += 4)
        {
            frame.Pixels[i] = 0; frame.Pixels[i + 1] = 0; frame.Pixels[i + 2] = 0;
        }

        restore.Render(frame, ctx, new VisualizationInputs());
        Assert.Equal(200, frame.Pixels[0]);
        Assert.Equal(100, frame.Pixels[1]);
        Assert.Equal(50,  frame.Pixels[2]);
    }

    [Fact]
    public void Comment_PreservesText()
    {
        // Length-prefixed string: 5 bytes "hello".
        var payload = new byte[4 + 5];
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(0, 4), 5);
        "hello"u8.CopyTo(payload.AsSpan(4));
        var fx = new AvsCommentEffect(payload);
        Assert.Equal("hello", fx.Text);
    }

    [Fact]
    public void EffectChain_RunsEachEffectOnce_InOrder()
    {
        var ctx = new AvsRenderContext(4, 4);
        var frame = MakeFrame(4, 4);
        var invert = new AvsInvertEffect(BuildPayload(1));
        var clear  = new AvsClearScreenEffect(BuildPayload(1, 0x00FF00 /* green */, 0, 0));
        var chain  = new AvsEffectChain(new AvsEffect[] { invert, clear });
        chain.Render(frame, ctx, new VisualizationInputs());

        // Final pixel = green (clear wins because it runs last).
        Assert.Equal(0x00, frame.Pixels[0]);
        Assert.Equal(0xFF, frame.Pixels[1]);
        Assert.Equal(0x00, frame.Pixels[2]);
    }

    [Fact]
    public void Renderer_FromPreset_AppliesKnownEffect()
    {
        // Construct an AvsPreset whose chain contains a single Invert effect.
        var invertPayload = BuildPayload(1);
        var preset = new AvsPreset("0.2", ClearEveryFrame: false,
            EffectBlobs: new[] { new AvsEffectBlob(AvsTypeCode.Invert, invertPayload) });
        var renderer = new AvsRenderer(preset);
        var frame = MakeFrame(4, 4, r: 10, g: 200, b: 30, a: 255);

        renderer.Render(new VisualizationInputs(new float[16], new SpectrumFrame(new float[8], 1.0f, 44100), 44100, 1), frame);

        Assert.Equal(245, frame.Pixels[0]);
        Assert.Equal(55,  frame.Pixels[1]);
        Assert.Equal(225, frame.Pixels[2]);
    }

    [Fact]
    public void Factory_UnknownTypeCode_ReturnsNoopEffect()
    {
        var fx = AvsEffectFactory.Create(0xDEAD, ReadOnlySpan<byte>.Empty);
        Assert.IsType<AvsUnknownEffect>(fx);
        Assert.False(fx.IsEnabled);
    }

    private static RgbaFrame MakeFrame(int w, int h, byte r = 0, byte g = 0, byte b = 0, byte a = 0xFF)
    {
        var f = new RgbaFrame(w, h);
        for (var i = 0; i < f.Pixels.Length; i += 4)
        {
            f.Pixels[i] = r; f.Pixels[i + 1] = g; f.Pixels[i + 2] = b; f.Pixels[i + 3] = a;
        }
        return f;
    }

    private static void SetPixel(RgbaFrame f, int x, int y, byte r, byte g, byte b)
    {
        var i = (y * f.Width + x) * 4;
        f.Pixels[i] = r; f.Pixels[i + 1] = g; f.Pixels[i + 2] = b; f.Pixels[i + 3] = 0xFF;
    }

    /// <summary>Pack one or more int32 values little-endian into a payload byte array.</summary>
    private static byte[] BuildPayload(params int[] values)
    {
        var bytes = new byte[values.Length * 4];
        for (var i = 0; i < values.Length; i++)
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * 4, 4), values[i]);
        return bytes;
    }
}
