// SPDX-License-Identifier: MIT
using System.Net;
using AetherMesh.Forge.Core;

namespace AetherMesh.Forge.Proxy;

/// <summary>
/// Starts an <see cref="HttpListener"/> on the configured port and dispatches
/// each incoming connection to a <see cref="ForgeProxy"/> instance.
/// </summary>
public sealed class ForgeProxyServer : IAsyncDisposable
{
    /// <summary>Default listen port for the Forge proxy.</summary>
    public const int DefaultPort = 2301;

    private readonly HttpListener _listener;
    private readonly ForgeProxy   _proxy;
    private readonly int          _port;
    private          Task?        _acceptLoop;
    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// Initialises a new <see cref="ForgeProxyServer"/>.
    /// </summary>
    /// <param name="forgeService">Backing Forge cache service.</param>
    /// <param name="port">TCP port to listen on (default 2301).</param>
    /// <param name="httpClient">Optional upstream HTTP client.</param>
    public ForgeProxyServer(
        IForgeService forgeService,
        int           port       = DefaultPort,
        HttpClient?   httpClient = null)
    {
        if (port is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be 1–65535.");

        _port     = port;
        _proxy    = new ForgeProxy(forgeService, httpClient);
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{port}/");
    }

    /// <summary>
    /// Starts the HTTP listener and begins accepting connections in the
    /// background.
    /// </summary>
    public void Start()
    {
        _listener.Start();
        _acceptLoop = AcceptLoopAsync(_cts.Token);
    }

    /// <summary>
    /// Stops the server and waits for the accept loop to drain.
    /// </summary>
    public async Task StopAsync()
    {
        await _cts.CancelAsync().ConfigureAwait(false);
        _listener.Stop();

        if (_acceptLoop is not null)
        {
            try { await _acceptLoop.ConfigureAwait(false); }
            catch (OperationCanceledException) { /* expected */ }
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _listener.Close();
        _cts.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (HttpListenerException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            // Handle each connection on a thread-pool thread so the accept
            // loop is never blocked.
            _ = Task.Run(() => _proxy.HandleRequestAsync(context, ct), ct);
        }
    }

    /// <summary>Gets the port this server is listening on.</summary>
    public int Port => _port;
}
