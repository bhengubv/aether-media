// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Milkdrop;

/// <summary>
/// One warp-mesh vertex — the post-per_pixel state at a normalised
/// (<see cref="GridX"/>, <see cref="GridY"/>) position, plus the cached
/// source UV that vertex samples from in the previous frame.
/// </summary>
public sealed class MilkdropMeshVertex
{
    public double GridX { get; init; }
    public double GridY { get; init; }
    public double SourceU { get; set; }
    public double SourceV { get; set; }
}

/// <summary>
/// 32×24 warp mesh — Milkdrop's documented default density. Each frame the
/// renderer re-evaluates per_pixel equations at every vertex, computes the
/// vertex's source UV, and rasterises by bilinear-interpolating UVs across
/// each mesh quad onto the output frame.
/// </summary>
public sealed class MilkdropWarpMesh
{
    /// <summary>Default mesh density: 32 columns × 24 rows of vertices.</summary>
    public const int DefaultWidth  = 32;
    public const int DefaultHeight = 24;

    private readonly MilkdropMeshVertex[,] _vertices;

    public MilkdropWarpMesh(int width = DefaultWidth, int height = DefaultHeight)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 2);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 2);
        Width = width;
        Height = height;
        _vertices = new MilkdropMeshVertex[width, height];
        for (var iy = 0; iy < height; iy++)
            for (var ix = 0; ix < width; ix++)
            {
                _vertices[ix, iy] = new MilkdropMeshVertex
                {
                    GridX = (double)ix / (width - 1),
                    GridY = (double)iy / (height - 1),
                };
            }
    }

    public int Width { get; }

    public int Height { get; }

    public MilkdropMeshVertex this[int x, int y] => _vertices[x, y];

    /// <summary>
    /// Re-compute every vertex's source UV from the evaluator. Uses
    /// <paramref name="evaluator"/>'s <see cref="MilkdropEvaluator.State"/>
    /// as the starting point and applies per_pixel equations per vertex.
    /// </summary>
    public void Compute(MilkdropEvaluator evaluator)
    {
        ArgumentNullException.ThrowIfNull(evaluator);

        for (var iy = 0; iy < Height; iy++)
        for (var ix = 0; ix < Width; ix++)
        {
            var v = _vertices[ix, iy];
            var x = v.GridX;
            var y = v.GridY;
            var dx = x - 0.5;
            var dy = y - 0.5;
            var rad = Math.Sqrt(dx * dx + dy * dy);
            var ang = Math.Atan2(dy, dx);

            var s = evaluator.HasPerPixel
                ? evaluator.EvaluatePerPixel(rad, ang, x, y)
                : evaluator.State;

            // Map this vertex to a source UV in the previous frame. Standard
            // Milkdrop warp formula: invert zoom + rotate around (cx,cy), then
            // shift by (dx,dy). sx/sy stretch the source.
            var localX = x - s.Cx;
            var localY = y - s.Cy;
            var zoom = s.Zoom <= 0.001 ? 0.001 : s.Zoom;
            var sx = s.Sx == 0 ? 1.0 : s.Sx;
            var sy = s.Sy == 0 ? 1.0 : s.Sy;
            var rx = localX / zoom / sx;
            var ry = localY / zoom / sy;
            var cos = Math.Cos(-s.Rot);
            var sin = Math.Sin(-s.Rot);
            var srcX = rx * cos - ry * sin + s.Cx - s.Dx;
            var srcY = rx * sin + ry * cos + s.Cy - s.Dy;

            v.SourceU = srcX;
            v.SourceV = srcY;
        }
    }
}
