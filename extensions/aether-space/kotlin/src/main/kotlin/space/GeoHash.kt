package space

@JvmInline
value class GeoHash(val value: String) {

    init {
        require(value.isNotBlank()) { "GeoHash value must not be blank" }
        require(value.length in 1..12) { "GeoHash precision must be between 1 and 12" }
    }

    override fun toString(): String = value

    companion object {
        private const val BASE32 = "0123456789bcdefghjkmnpqrstuvwxyz"

        fun fromCoordinates(lat: Double, lon: Double, precision: Int = 6): GeoHash {
            require(lat in -90.0..90.0) { "Latitude must be in [-90, 90]" }
            require(lon in -180.0..180.0) { "Longitude must be in [-180, 180]" }
            require(precision in 1..12) { "Precision must be between 1 and 12" }

            var minLat = -90.0
            var maxLat = 90.0
            var minLon = -180.0
            var maxLon = 180.0

            val hash = StringBuilder()
            var isEven = true
            var bit = 0
            var ch = 0

            while (hash.length < precision) {
                if (isEven) {
                    val mid = (minLon + maxLon) / 2.0
                    if (lon >= mid) {
                        ch = ch or (1 shl (4 - bit))
                        minLon = mid
                    } else {
                        maxLon = mid
                    }
                } else {
                    val mid = (minLat + maxLat) / 2.0
                    if (lat >= mid) {
                        ch = ch or (1 shl (4 - bit))
                        minLat = mid
                    } else {
                        maxLat = mid
                    }
                }
                isEven = !isEven
                if (bit < 4) {
                    bit++
                } else {
                    hash.append(BASE32[ch])
                    bit = 0
                    ch = 0
                }
            }

            return GeoHash(hash.toString())
        }
    }
}
