// swift-tools-version:5.9
import PackageDescription

let package = Package(
    name: "AetherMedia",
    platforms: [
        .iOS(.v16),
        .macOS(.v13),
    ],
    products: [
        .library(
            name: "AetherMedia",
            targets: ["AetherMedia"]
        ),
    ],
    dependencies: [
        // aether-protocol: integrated at runtime via Aether mesh — not a compile-time dependency
    ],
    targets: [
        .target(
            name: "AetherMedia",
            dependencies: [],
            path: "Sources/AetherMedia"
        ),
        .testTarget(
            name: "AetherMediaTests",
            dependencies: ["AetherMedia"],
            path: "Tests/AetherMediaTests"
        ),
    ]
)
