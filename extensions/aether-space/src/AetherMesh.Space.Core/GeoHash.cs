// SPDX-License-Identifier: MIT
namespace AetherMesh.Space.Core;

/// <summary>
/// Strongly-typed wrapper around a Geohash string.  Geohash encodes a
/// geographic coordinate as a short alphanumeric string where longer values
/// represent higher precision.
/// </summary>
public readonly record struct GeoHash
{
    // Base-32 alphabet used by the Geohash standard.
    private const string Base32 = "0123456789bcdefghjkmnpqrstuvwxyz";

    /// <summary>The raw geohash string value.</summary>
    public string Value { get; }

    private GeoHash(string value) => Value = value;

    /// <summary>
    /// Parses a raw geohash string, validating that every character belongs
    /// to the Geohash base-32 alphabet.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is null, empty, or contains
    /// characters outside the Geohash alphabet.
    /// </exception>
    public static GeoHash Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        foreach (var ch in value)
        {
            if (Base32.IndexOf(char.ToLowerInvariant(ch)) < 0)
                throw new ArgumentException(
                    $"Character '{ch}' is not a valid Geohash base-32 character.", nameof(value));
        }

        return new GeoHash(value.ToLowerInvariant());
    }

    /// <summary>
    /// Encodes the given WGS-84 coordinates to a Geohash of the requested
    /// <paramref name="precision"/> (number of characters, 1–12).
    /// </summary>
    /// <param name="lat">Latitude in degrees (−90 to +90).</param>
    /// <param name="lon">Longitude in degrees (−180 to +180).</param>
    /// <param name="precision">Number of Geohash characters (default 6).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when coordinates are out of WGS-84 range or precision is
    /// outside 1–12.
    /// </exception>
    public static GeoHash FromCoordinates(double lat, double lon, int precision = 6)
    {
        if (lat < -90.0 || lat > 90.0)
            throw new ArgumentOutOfRangeException(nameof(lat), "Latitude must be in −90 … +90.");
        if (lon < -180.0 || lon > 180.0)
            throw new ArgumentOutOfRangeException(nameof(lon), "Longitude must be in −180 … +180.");
        if (precision < 1 || precision > 12)
            throw new ArgumentOutOfRangeException(nameof(precision), "Precision must be 1–12.");

        double minLat = -90, maxLat = 90;
        double minLon = -180, maxLon = 180;

        var hash = new System.Text.StringBuilder(precision);
        int bits = 0, bitCount = 0, charIndex = 0;
        bool isLon = true; // alternate lon/lat, starting with lon

        while (hash.Length < precision)
        {
            if (isLon)
            {
                var mid = (minLon + maxLon) / 2;
                if (lon >= mid) { charIndex = (charIndex << 1) | 1; minLon = mid; }
                else            { charIndex = charIndex << 1;        maxLon = mid; }
            }
            else
            {
                var mid = (minLat + maxLat) / 2;
                if (lat >= mid) { charIndex = (charIndex << 1) | 1; minLat = mid; }
                else            { charIndex = charIndex << 1;        maxLat = mid; }
            }

            isLon = !isLon;
            bits++;

            if (bits == 5)
            {
                hash.Append(Base32[charIndex]);
                bits = 0;
                charIndex = 0;
                bitCount++;
            }

            _ = bitCount; // suppress unused-variable warning
        }

        return new GeoHash(hash.ToString());
    }

    /// <summary>Implicit conversion to <see langword="string"/>.</summary>
    public static implicit operator string(GeoHash g) => g.Value;

    /// <summary>Explicit conversion from <see langword="string"/>.</summary>
    public static explicit operator GeoHash(string s) => Parse(s);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
