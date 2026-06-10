// swift-tools-version:5.9
import PackageDescription

let package = Package(
    name: "AetherNetMedia",
    platforms: [
        .iOS(.v16),
        .macOS(.v13),
    ],
    products: [
        .library(
            name: "AetherNetMedia",
            targets: ["AetherNetMedia"]
        ),
        // Cross-language wire-format conformance driver — used by
        // tests/cross-language/run_all.sh to prove the Swift SDK round-trips
        // the golden fixtures identically to every other language SDK.
        .executable(
            name: "wire-roundtrip",
            targets: ["wire-roundtrip"]
        ),
    ],
    dependencies: [
        // aether-protocol: integrated at runtime via Aether mesh — not a compile-time dependency
    ],
    targets: [
        .target(
            name: "AetherNetMedia",
            dependencies: [],
            path: "Sources/AetherNetMedia"
        ),
        .executableTarget(
            name: "wire-roundtrip",
            dependencies: ["AetherNetMedia"],
            path: "Sources/wire-roundtrip"
        ),
        .testTarget(
            name: "AetherNetMediaTests",
            dependencies: ["AetherNetMedia"],
            path: "Tests/AetherNetMediaTests"
        ),
    ]
)
