// SPDX-License-Identifier: MIT

using System.Text;
using AetherMedia.LocalLibrary.Audio.Visualization;
using AetherMedia.LocalLibrary.Audio.Visualization.Milkdrop;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Visualization;

public class MilkdropExtendedTests
{
    [Fact]
    public async Task Parser_ExtractsCustomShapeWithEquations()
    {
        const string milk = """
            [preset00]
            shape_1_enabled=1
            shape_1_num_inst=2
            shape_1_sides=6
            shape_1_x=0.25
            shape_1_y=0.5
            shape_1_rad=0.1
            shape_1_r=1
            shape_1_g=0.5
            shape_1_b=0
            shape_1_init1=t1=0
            shape_1_per_frame1=r=sin(time)*0.5+0.5
            """;
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(milk));
        var preset = await new MilkdropPresetParser().ParseAsync(ms);

        Assert.Single(preset.Shapes);
        var s = preset.Shapes[0];
        Assert.True(s.Enabled);
        Assert.Equal(2, s.Instances);
        Assert.Equal(6, s.Sides);
        Assert.Equal(0.25, s.X, 4);
        Assert.Single(s.InitEquations);
        Assert.Single(s.PerFrameEquations);
        Assert.Contains("sin(time)", s.PerFrameEquations[0]);
    }

    [Fact]
    public async Task Parser_ExtractsCustomWaveWithPerPointEqs()
    {
        const string milk = """
            [preset00]
            wave_1_enabled=1
            wave_1_samples=128
            wave_1_r=0.2
            wave_1_g=0.8
            wave_1_b=0.4
            wave_1_per_point1=x=sample; y=0.5+value1*0.4
            """;
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(milk));
        var preset = await new MilkdropPresetParser().ParseAsync(ms);

        Assert.Single(preset.Waves);
        var w = preset.Waves[0];
        Assert.True(w.Enabled);
        Assert.Equal(128, w.Samples);
        Assert.Equal(0.8, w.G, 4);
        Assert.Single(w.PerPointEquations);
    }

    [Fact]
    public async Task Parser_ConcatenatesShaderLinesByIndex()
    {
        const string milk = """
            [preset00]
            warp_1=`shader_body
            warp_2=`{
            warp_3=`  ret = tex2D(sampler_main, uv);
            warp_4=`}
            """;
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(milk));
        var preset = await new MilkdropPresetParser().ParseAsync(ms);

        Assert.NotNull(preset.WarpShader);
        Assert.Contains("shader_body", preset.WarpShader);
        Assert.Contains("tex2D(sampler_main", preset.WarpShader);
    }

    [Fact]
    public async Task Evaluator_PerPixelEquations_AreApplied()
    {
        const string milk = """
            [preset00]
            zoom=1
            per_pixel_1=zoom = 1 + rad
            """;
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(milk));
        var preset = await new MilkdropPresetParser().ParseAsync(ms);
        var ev = new MilkdropEvaluator(preset);
        ev.EvaluateFrame(0, 0, 60.0, 0, 0, 0);

        Assert.True(ev.HasPerPixel);
        var centre = ev.EvaluatePerPixel(rad: 0, ang: 0, x: 0.5, y: 0.5);
        var edge   = ev.EvaluatePerPixel(rad: 0.7, ang: 0, x: 0.0, y: 0.0);
        Assert.Equal(1.0, centre.Zoom, 4);
        Assert.Equal(1.7, edge.Zoom,   4);
    }

    [Fact]
    public void WarpMesh_ProducesDefaultIdentityWhenNoPerPixel()
    {
        var preset = new MilkdropPreset(
            SectionName: "preset00",
            Parameters: new Dictionary<string, double>(),
            PerFrameEquations: Array.Empty<string>(),
            PerPixelEquations: Array.Empty<string>(),
            Shapes: Array.Empty<MilkdropCustomShape>(),
            Waves: Array.Empty<MilkdropCustomWave>());
        var ev = new MilkdropEvaluator(preset);
        ev.EvaluateFrame(0, 0, 60.0, 0, 0, 0);

        var mesh = new MilkdropWarpMesh();
        mesh.Compute(ev);

        // With identity zoom/rot/cx/cy and no per_pixel, every vertex's
        // source UV should match its own grid position (identity warp).
        for (var iy = 0; iy < mesh.Height; iy += 4)
        for (var ix = 0; ix < mesh.Width; ix += 4)
        {
            var v = mesh[ix, iy];
            Assert.Equal(v.GridX, v.SourceU, 3);
            Assert.Equal(v.GridY, v.SourceV, 3);
        }
    }

    [Fact]
    public async Task Renderer_WithCustomShape_PaintsPixels()
    {
        const string milk = """
            [preset00]
            shape_1_enabled=1
            shape_1_x=0.5
            shape_1_y=0.5
            shape_1_rad=0.25
            shape_1_sides=4
            shape_1_r=1
            shape_1_g=0
            shape_1_b=0
            shape_1_a=1
            """;
        var preset = await new MilkdropPresetParser().ParseAsync(new MemoryStream(Encoding.UTF8.GetBytes(milk)));
        var renderer = new MilkdropRenderer(preset);
        var frame = new RgbaFrame(48, 48);

        renderer.Render(new VisualizationInputs(new float[64], new SpectrumFrame(new float[16], 1.0f, 44100), 44100, 1), frame);

        // Some red pixels must appear from the centre-rendered shape.
        var hasRed = false;
        for (var i = 0; i < frame.Pixels.Length; i += 4)
            if (frame.Pixels[i] > 100 && frame.Pixels[i + 1] < 50 && frame.Pixels[i + 2] < 50)
            { hasRed = true; break; }
        Assert.True(hasRed, "custom shape should paint red pixels");
    }

    [Fact]
    public void Shader_RecognisesCommonPatterns()
    {
        const string src = """
            shader_body
            {
                ret = tex2D(sampler_main, uv);
                ret.rgb *= 0.9;
                ret.a = 1;
            }
            """;
        var shader = MilkdropShader.TryCompile(src);
        Assert.NotNull(shader);
        Assert.Equal(3, shader!.RecognisedStatementCount);
        Assert.Equal(0, shader.UnrecognisedStatementCount);
    }

    [Fact]
    public void Shader_AppliesTex2D_AndScalarMultiply()
    {
        const string src = "ret = tex2D(sampler_main, uv); ret.rgb *= 0.5;";
        var shader = MilkdropShader.TryCompile(src);
        Assert.NotNull(shader);

        var preset = new MilkdropPreset(
            SectionName: "preset00",
            Parameters: new Dictionary<string, double>(),
            PerFrameEquations: Array.Empty<string>(),
            PerPixelEquations: Array.Empty<string>(),
            Shapes: Array.Empty<MilkdropCustomShape>(),
            Waves: Array.Empty<MilkdropCustomWave>());
        var ev = new MilkdropEvaluator(preset);
        var target = new RgbaFrame(4, 4);
        // Fill target with red so tex2D pulls red.
        for (var i = 0; i < target.Pixels.Length; i += 4)
        {
            target.Pixels[i] = 200; target.Pixels[i + 1] = 0; target.Pixels[i + 2] = 0; target.Pixels[i + 3] = 0xFF;
        }
        var prev = new byte[target.Pixels.Length];
        Buffer.BlockCopy(target.Pixels, 0, prev, 0, prev.Length);
        // Set every pixel of prev to bright red.
        for (var i = 0; i < prev.Length; i += 4) { prev[i] = 200; prev[i + 1] = 0; prev[i + 2] = 0; prev[i + 3] = 0xFF; }

        shader!.RenderPerPixel(target, prev, ev);

        // After tex2D(red 200) * 0.5, red channel should be ~100.
        Assert.InRange(target.Pixels[0], 90, 110);
    }
}
