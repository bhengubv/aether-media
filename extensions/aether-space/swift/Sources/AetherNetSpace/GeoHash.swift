import Foundation

public struct GeoHash: Codable, Equatable, Hashable, CustomStringConvertible {
    public let value: String

    private static let base32: [Character] = Array("0123456789bcdefghjkmnpqrstuvwxyz")

    public init(_ value: String) {
        precondition(!value.isEmpty && value.count <= 12, "GeoHash must be 1-12 characters")
        self.value = value
    }

    public var description: String { value }

    /// Encodes geographic coordinates into a GeoHash string.
    /// - Parameters:
    ///   - lat: Latitude in degrees (-90...90)
    ///   - lon: Longitude in degrees (-180...180)
    ///   - precision: Number of characters in the hash (1-12, default 6)
    /// - Returns: A GeoHash with the encoded string
    public static func fromCoordinates(lat: Double, lon: Double, precision: Int = 6) -> GeoHash {
        precondition((-90.0...90.0).contains(lat), "Latitude must be in [-90, 90]")
        precondition((-180.0...180.0).contains(lon), "Longitude must be in [-180, 180]")
        precondition((1...12).contains(precision), "Precision must be between 1 and 12")

        var minLat = -90.0, maxLat = 90.0
        var minLon = -180.0, maxLon = 180.0

        var hash = ""
        var isEven = true
        var bit = 0
        var ch = 0

        while hash.count < precision {
            if isEven {
                let mid = (minLon + maxLon) / 2.0
                if lon >= mid {
                    ch |= (1 << (4 - bit))
                    minLon = mid
                } else {
                    maxLon = mid
                }
            } else {
                let mid = (minLat + maxLat) / 2.0
                if lat >= mid {
                    ch |= (1 << (4 - bit))
                    minLat = mid
                } else {
                    maxLat = mid
                }
            }
            isEven = !isEven
            if bit < 4 {
                bit += 1
            } else {
                hash.append(base32[ch])
                bit = 0
                ch = 0
            }
        }

        return GeoHash(hash)
    }
}
