// SPDX-License-Identifier: MIT
using System.Net;
using System.Security.Cryptography;
using Aether.Forge.Core;

namespace Aether.Forge.Proxy;

/// <summary>
/// HTTP proxy that intercepts package-registry requests and serves them from
/// the Aether Forge mesh cache when possible.
///
/// On a cache hit the response is streamed from <see cref="IForgeService.FetchAsync"/>.
/// On a cache miss the request is forwarded to the internet, the response is
/// stored via <see cref="IForgeService.CacheAsync"/>, and then returned to the
/// caller.
/// </summary>
public sealed class ForgeProxy
{
    private readonly IForgeService _forge;
    private readonly HttpClient _http;

    /// <summary>
    /// Initialises a new <see cref="ForgeProxy"/>.
    /// </summary>
    /// <param name="forgeService">Backing Forge cache service.</param>
    /// <param name="httpClient">
    /// Optional <see cref="HttpClient"/> for origin-server fetches.
    /// When <see langword="null"/> a default instance is created.
    /// </param>
    public ForgeProxy(IForgeService forgeService, HttpClient? httpClient = null)
    {
        _forge = forgeService ?? throw new ArgumentNullException(nameof(forgeService));
        _http  = httpClient  ?? new HttpClient();
    }

    /// <summary>
    /// Handles a single incoming HTTP proxy request.
    ///
    /// The request URL is used as the package-ID lookup key.  If the Forge
    /// cache contains a matching entry the cached bytes are written directly
    /// to the response.  Otherwise the request is forwarded to the origin,
    /// the response is cached, and returned to the caller.
    /// </summary>
    /// <param name="context">The <see cref="HttpListenerContext"/> to handle.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var request  = context.Request;
        var response = context.Response;

        var url = request.Url?.ToString() ?? string.Empty;

        try
        {
            // Use the raw URL as the package-ID key for lookup.
            var entry = await _forge.QueryAsync(url, ct).ConfigureAwait(false);

            if (entry is not null)
            {
                // Cache hit — serve directly from the Forge mesh.
                var cached = await _forge.FetchAsync(entry.ContentHash, ct).ConfigureAwait(false);

                if (cached is not null)
                {
                    response.StatusCode        = 200;
                    response.ContentType       = "application/octet-stream";
                    response.ContentLength64   = entry.SizeBytes;
                    response.AddHeader("X-Aether-Forge-Cache", "HIT");

                    await cached.CopyToAsync(response.OutputStream, ct).ConfigureAwait(false);
                    return;
                }
            }

            // Cache miss — forward to internet.
            using var originRequest  = new HttpRequestMessage(
                new HttpMethod(request.HttpMethod), url);

            using var originResponse = await _http.SendAsync(
                originRequest,
                HttpCompletionOption.ResponseHeadersRead,
                ct).ConfigureAwait(false);

            response.StatusCode  = (int)originResponse.StatusCode;
            response.ContentType = originResponse.Content.Headers.ContentType?.ToString()
                                   ?? "application/octet-stream";
            response.AddHeader("X-Aether-Forge-Cache", "MISS");

            var originBytes = await originResponse.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);

            // Compute content hash and store in Forge cache.
            var hashBytes   = SHA256.HashData(originBytes);
            var contentHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            using var storeStream = new MemoryStream(originBytes, writable: false);
            await _forge.CacheAsync(url, storeStream, contentHash, ct).ConfigureAwait(false);

            response.ContentLength64 = originBytes.Length;
            await response.OutputStream.WriteAsync(originBytes, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            response.StatusCode = 503;
        }
        catch (Exception ex)
        {
            response.StatusCode = 502;
            var errorBytes = System.Text.Encoding.UTF8.GetBytes(ex.Message);
            response.ContentLength64 = errorBytes.Length;
            await response.OutputStream.WriteAsync(errorBytes, ct).ConfigureAwait(false);
        }
        finally
        {
            response.OutputStream.Close();
        }
    }
}
