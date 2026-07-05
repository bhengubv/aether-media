// SPDX-License-Identifier: MIT

using AetherMedia.Ingest;
using AetherMedia.Ingest.Hls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AetherMedia.DependencyInjection;

/// <summary>
/// Registers the stream-ingest gateway. Purely additive — a new opt-in alongside the other
/// <c>Add*</c> builder methods; it changes nothing existing.
/// </summary>
public static class IngestBuilderExtensions
{
    /// <summary>
    /// Register the ingest gateway: the HLS source adapter, the passthrough transcoder, and the
    /// gateway itself. Requires <see cref="AetherNetMediaBuilder.AddStreaming"/> for the
    /// <c>ILiveStreamPublisher</c> the gateway publishes through.
    /// </summary>
    public static AetherNetMediaBuilder AddIngest(this AetherNetMediaBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<HttpClient>();
        builder.Services.TryAddSingleton<ITranscoder, PassthroughTranscoder>();
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ISourceAdapter, HlsSourceAdapter>());
        builder.Services.TryAddSingleton<IStreamGateway, StreamGateway>();

        return builder;
    }
}
