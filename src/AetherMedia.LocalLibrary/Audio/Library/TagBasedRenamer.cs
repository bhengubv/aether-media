// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Tags;

namespace AetherMedia.LocalLibrary.Audio.Library;

/// <summary>
/// Renames audio files from their tag fields using a Winamp-style template:
/// <c>{Artist}/{Album}/{Track:00} - {Title}.{Ext}</c>.
///
/// <para>
/// Template tokens (case-insensitive):
/// <c>{Title}</c>, <c>{Artist}</c>, <c>{Album}</c>, <c>{Year}</c>,
/// <c>{Track}</c> (with optional <c>:NN</c> padding), <c>{Genre}</c>,
/// <c>{Ext}</c>. Missing tags fall back to <see cref="MissingPlaceholder"/>.
/// Forward / back slashes in the template denote subfolder structure and are
/// preserved as <see cref="System.IO.Path.DirectorySeparatorChar"/> on the
/// host OS. Filesystem-illegal characters in token values are replaced with
/// <see cref="ReplacementForIllegalChars"/>.
/// </para>
/// </summary>
public sealed class TagBasedRenamer
{
    private readonly IAudioTagReader _reader;

    public TagBasedRenamer(IAudioTagReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    /// <summary>Default replacement for <c>"unknown"</c> / missing tag tokens.</summary>
    public string MissingPlaceholder { get; init; } = "Unknown";

    /// <summary>What to substitute for filesystem-illegal characters in token values.</summary>
    public char ReplacementForIllegalChars { get; init; } = '_';

    /// <summary>
    /// Compute the new file path for <paramref name="filePath"/> applying
    /// <paramref name="template"/>. Returns the source path unchanged when no
    /// tags can be read.
    /// </summary>
    public async Task<string> ComputeNewPathAsync(string filePath, string template, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentException.ThrowIfNullOrEmpty(template);
        var tags = await _reader.ReadAsync(filePath, ct).ConfigureAwait(false);
        return BuildPath(filePath, template, tags);
    }

    /// <summary>
    /// Rename + move the file to the templated location, creating the target
    /// directory tree if needed. Returns the new path. No-op when the source
    /// already matches the target.
    /// </summary>
    public async Task<string> RenameAsync(string filePath, string template, CancellationToken ct = default)
    {
        var newPath = await ComputeNewPathAsync(filePath, template, ct).ConfigureAwait(false);
        if (string.Equals(filePath, newPath, StringComparison.OrdinalIgnoreCase)) return filePath;
        Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
        System.IO.File.Move(filePath, newPath);
        return newPath;
    }

    internal string BuildPath(string filePath, string template, AudioTags? tags)
    {
        var rootDir = Path.GetDirectoryName(filePath) ?? "";
        var ext = Path.GetExtension(filePath).TrimStart('.');
        var rendered = RenderTemplate(template, tags, ext);

        // Split rendered by / and \ into folder + file parts.
        var segments = rendered.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        var newRel = Path.Combine(segments);
        return Path.IsPathRooted(newRel) ? newRel : Path.Combine(rootDir, newRel);
    }

    private string RenderTemplate(string template, AudioTags? tags, string ext)
    {
        var sb = new System.Text.StringBuilder(template.Length * 2);
        var i = 0;
        while (i < template.Length)
        {
            var c = template[i];
            if (c != '{') { sb.Append(c); i++; continue; }
            var end = template.IndexOf('}', i + 1);
            if (end < 0) { sb.Append(c); i++; continue; }

            var token = template[(i + 1)..end];
            sb.Append(Resolve(token, tags, ext));
            i = end + 1;
        }
        return sb.ToString();
    }

    private string Resolve(string token, AudioTags? tags, string ext)
    {
        // Split format spec: Track:00 → name="Track", spec="00".
        var colon = token.IndexOf(':');
        var name = colon < 0 ? token : token[..colon];
        var spec = colon < 0 ? null : token[(colon + 1)..];

        string raw = name.ToUpperInvariant() switch
        {
            "TITLE"  => tags?.Title  ?? MissingPlaceholder,
            "ARTIST" => tags?.Artist ?? MissingPlaceholder,
            "ALBUM"  => tags?.Album  ?? MissingPlaceholder,
            "GENRE"  => tags?.Genre  ?? MissingPlaceholder,
            "YEAR"   => tags?.Year?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? MissingPlaceholder,
            "TRACK"  => FormatTrack(tags?.TrackNumber, spec),
            "EXT"    => ext,
            _ => MissingPlaceholder,
        };

        return Sanitise(raw);
    }

    private string FormatTrack(int? trackNumber, string? spec)
    {
        if (trackNumber is null) return MissingPlaceholder;
        if (string.IsNullOrEmpty(spec)) return trackNumber.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return trackNumber.Value.ToString(spec, System.Globalization.CultureInfo.InvariantCulture);
    }

    private string Sanitise(string segment)
    {
        Span<char> buf = stackalloc char[segment.Length];
        for (var i = 0; i < segment.Length; i++)
            buf[i] = "*?<>|:\"".Contains(segment[i]) ? ReplacementForIllegalChars : segment[i];
        return new string(buf).Trim();
    }
}
