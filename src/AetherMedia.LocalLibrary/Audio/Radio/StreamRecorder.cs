// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Radio;

/// <summary>
/// Records the raw bytes from a stream (typically the metadata-stripped
/// payload from <see cref="ShoutcastClient.OpenStreamAsync"/>) into a file
/// on disk. Splits the recording at <see cref="ShoutcastClient.MetadataUpdated"/>
/// events when <see cref="SplitOnTrackChange"/> is true — Winamp's
/// "split each track into its own file" mode.
/// </summary>
public sealed class StreamRecorder : IDisposable
{
    private readonly string _baseDirectory;
    private readonly Func<string, string> _filenameForTitle;
    private FileStream? _current;
    private string? _currentTitle;
    private long _bytesWritten;
    private bool _disposed;

    /// <summary>True to start a new file each time the stream announces a new title.</summary>
    public bool SplitOnTrackChange { get; init; } = true;

    /// <summary>Default extension when no MIME hint is available.</summary>
    public string DefaultExtension { get; init; } = ".mp3";

    /// <summary>The file currently being written to, or null if not recording.</summary>
    public string? CurrentFilePath { get; private set; }

    /// <summary>Total bytes written across the entire recording session.</summary>
    public long BytesWritten => _bytesWritten;

    /// <summary>
    /// Construct a recorder writing under <paramref name="baseDirectory"/>.
    /// <paramref name="filenameForTitle"/> turns an Icy <c>StreamTitle</c> into
    /// a safe file name (defaults to a timestamp + sanitised title).
    /// </summary>
    public StreamRecorder(string baseDirectory, Func<string, string>? filenameForTitle = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(baseDirectory);
        Directory.CreateDirectory(baseDirectory);
        _baseDirectory = baseDirectory;
        _filenameForTitle = filenameForTitle ?? DefaultFileName;
    }

    /// <summary>Begin a new recording with the optional initial title.</summary>
    public void Start(string? initialTitle = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Stop();
        Rotate(initialTitle);
    }

    /// <summary>Called by the host whenever the Icy metadata announces a new title.</summary>
    public void OnTrackChanged(string newTitle)
    {
        if (!SplitOnTrackChange) return;
        if (string.Equals(newTitle, _currentTitle, StringComparison.Ordinal)) return;
        Rotate(newTitle);
    }

    /// <summary>Write a chunk of bytes from the source stream.</summary>
    public async ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken ct = default)
    {
        if (_current is null) Rotate(_currentTitle);
        await _current!.WriteAsync(bytes, ct).ConfigureAwait(false);
        _bytesWritten += bytes.Length;
    }

    /// <summary>Stop recording, flushing and closing the current file.</summary>
    public void Stop()
    {
        _current?.Flush();
        _current?.Dispose();
        _current = null;
        CurrentFilePath = null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }

    private void Rotate(string? title)
    {
        _current?.Flush();
        _current?.Dispose();
        _currentTitle = title;
        var fileName = _filenameForTitle(title ?? "stream") + DefaultExtension;
        CurrentFilePath = Path.Combine(_baseDirectory, fileName);
        _current = new FileStream(CurrentFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, 64 * 1024, useAsync: true);
    }

    private static string DefaultFileName(string title)
    {
        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        Span<char> buf = stackalloc char[title.Length];
        for (var i = 0; i < title.Length; i++)
            buf[i] = "*?<>|:\"\\/".Contains(title[i]) ? '_' : title[i];
        return $"{stamp} - {new string(buf)}";
    }
}
