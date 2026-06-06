// SPDX-License-Identifier: MIT

using System.Xml;
using System.Xml.Linq;
using AetherMesh.Media.LocalLibrary.Interfaces;
using AetherMesh.Media.LocalLibrary.Models;
using Microsoft.Extensions.Logging;

namespace AetherMesh.Media.LocalLibrary;

/// <summary>
/// Reads and writes Kodi-compatible <c>.nfo</c> XML files alongside video files.
/// The XML schema follows the Kodi &lt;movie&gt; element convention so any existing
/// Kodi library remains compatible.
/// </summary>
public sealed class MovieMetadataService : IMovieMetadataService
{
    private readonly ILogger<MovieMetadataService> _logger;

    public MovieMetadataService(ILogger<MovieMetadataService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public string GetNfoPath(string videoFilePath) =>
        Path.ChangeExtension(videoFilePath, ".nfo");

    /// <inheritdoc/>
    public async Task<MovieMetadata?> ReadAsync(string videoFilePath, CancellationToken ct = default)
    {
        var nfoPath = GetNfoPath(videoFilePath);

        if (!File.Exists(nfoPath))
            return null;

        try
        {
            var xml = await File.ReadAllTextAsync(nfoPath, ct).ConfigureAwait(false);
            var doc = XDocument.Parse(xml);
            var root = doc.Root;

            if (root is null)
                return null;

            string? Text(string name)    => root.Element(name)?.Value;
            int     Int(string name)     => int.TryParse(Text(name), out var v) ? v : 0;
            float   Float(string name)   => float.TryParse(
                Text(name),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var v) ? v : 0f;
            bool    Bool(string name)    => string.Equals(Text(name), "true",
                                               StringComparison.OrdinalIgnoreCase);
            string[] Elements(string name) =>
                root.Elements(name).Select(e => e.Value).ToArray();

            return new MovieMetadata
            {
                FilePath       = videoFilePath,
                Title          = Text("title")    ?? string.Empty,
                Year           = Int("year"),
                Plot           = Text("plot")     ?? string.Empty,
                Tagline        = Text("tagline")  ?? string.Empty,
                Rating         = Float("rating"),
                RuntimeMinutes = Int("runtime"),
                Genres         = Elements("genre"),
                Directors      = Elements("director"),
                Cast           = root.Elements("actor")
                                     .Select(a => a.Element("name")?.Value ?? string.Empty)
                                     .Where(n => n.Length > 0)
                                     .ToArray(),
                ImdbId         = Text("id"),
                TmdbId         = Text("tmdbid"),
                Watched        = Bool("watched"),
                PosterPath     = Text("thumb")
            };
        }
        catch (XmlException ex)
        {
            _logger.LogWarning(ex, "Failed to parse NFO file: {Path}", nfoPath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MovieMetadataService.ReadAsync failed: {Path}", nfoPath);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task WriteAsync(MovieMetadata metadata, CancellationToken ct = default)
    {
        var nfoPath = GetNfoPath(metadata.FilePath);

        try
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("movie",
                    new XElement("title",   metadata.Title),
                    new XElement("year",    metadata.Year),
                    new XElement("plot",    metadata.Plot),
                    new XElement("tagline", metadata.Tagline),
                    new XElement("rating",  metadata.Rating.ToString(
                        "F1", System.Globalization.CultureInfo.InvariantCulture)),
                    new XElement("runtime", metadata.RuntimeMinutes),
                    metadata.Genres.Select(g => new XElement("genre", g)),
                    metadata.Directors.Select(d => new XElement("director", d)),
                    metadata.Cast.Select(c => new XElement("actor",
                        new XElement("name", c))),
                    metadata.ImdbId   is not null ? new XElement("id",     metadata.ImdbId)   : null!,
                    metadata.TmdbId   is not null ? new XElement("tmdbid", metadata.TmdbId)   : null!,
                    new XElement("watched", metadata.Watched.ToString().ToLowerInvariant()),
                    metadata.PosterPath is not null ? new XElement("thumb", metadata.PosterPath) : null!
                ));

            // Remove null elements (XDocument skips actual null nodes, but be explicit)
            doc.Descendants().Where(e => e.Value == null!).Remove();

            await using var writer = new StreamWriter(nfoPath, append: false,
                encoding: System.Text.Encoding.UTF8);
            await writer.WriteAsync(doc.ToString()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MovieMetadataService.WriteAsync failed: {Path}", nfoPath);
            throw;
        }
    }
}
