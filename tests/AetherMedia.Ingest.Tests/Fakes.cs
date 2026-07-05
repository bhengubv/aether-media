// SPDX-License-Identifier: MIT

using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using AetherMedia.Streaming;

namespace AetherMedia.Ingest.Tests;

/// <summary>Captures everything published, so gateway tests can assert byte-fidelity and ordering.</summary>
internal sealed class FakeLiveStreamPublisher : ILiveStreamPublisher
{
    public List<(byte[] Bytes, bool IsKeyframe, uint Sequence)> Frames { get; } = [];
    public string? PublishedTitle { get; private set; }
    public string? PublishedCodec { get; private set; }
    public int StopCount { get; private set; }
    public Guid? ActiveStreamId { get; private set; }
    public bool IsPublishing { get; private set; }
    public int ViewerCount => 0;

    public event EventHandler<int>? ViewerCountChanged { add { } remove { } }
    public event EventHandler<Exception>? PublishError { add { } remove { } }

    public Task<Guid> StartPublishingAsync(
        string title, string codec, IReadOnlyList<string> tags, CancellationToken ct = default)
    {
        PublishedTitle = title;
        PublishedCodec = codec;
        ActiveStreamId = Guid.NewGuid();
        IsPublishing = true;
        return Task.FromResult(ActiveStreamId.Value);
    }

    public Task PublishFrameAsync(
        ReadOnlyMemory<byte> encodedFrame, bool isKeyframe, uint sequence, CancellationToken ct = default)
    {
        Frames.Add((encodedFrame.ToArray(), isKeyframe, sequence));
        return Task.CompletedTask;
    }

    public Task StopPublishingAsync(CancellationToken ct = default)
    {
        IsPublishing = false;
        StopCount++;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>Emits a fixed list of segments — a reproducible, network-free source.</summary>
internal sealed class FakeSourceAdapter : ISourceAdapter
{
    private readonly IReadOnlyList<MediaSegment> _segments;
    private readonly bool _canHandle;

    public FakeSourceAdapter(IReadOnlyList<MediaSegment> segments, bool canHandle = true)
    {
        _segments = segments;
        _canHandle = canHandle;
    }

    public bool CanHandle(SourceDescriptor source) => _canHandle;

    public async IAsyncEnumerable<MediaSegment> ReadAsync(
        SourceDescriptor source, [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var segment in _segments)
        {
            ct.ThrowIfCancellationRequested();
            yield return segment;
            await Task.Yield();
        }
    }
}

/// <summary>A capable transcoder that fans one segment into two rungs — stands in for real transcode.</summary>
internal sealed class DoublingTranscoder : ITranscoder
{
    public bool CanNormalize(string codec, TargetProfile target) => true;

    public ValueTask<IReadOnlyList<MediaSegment>> NormalizeAsync(
        MediaSegment segment, TargetProfile target, CancellationToken ct = default)
        => new(new[]
        {
            segment with { RungBitrateKbps = 400 },
            segment with { RungBitrateKbps = 800 },
        });
}

/// <summary>Serves canned HTTP responses by URL — reproducible HLS source for adapter tests.</summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly IReadOnlyDictionary<string, (string ContentType, byte[] Body)> _responses;

    public StubHttpMessageHandler(IReadOnlyDictionary<string, (string ContentType, byte[] Body)> responses)
        => _responses = responses;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri!.ToString();
        if (!_responses.TryGetValue(url, out var entry))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(entry.Body),
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(entry.ContentType);
        return Task.FromResult(response);
    }
}
