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
        .testTarget(
            name: "AetherNetMediaTests",
            dependencies: ["AetherNetMedia"],
            path: "Tests/AetherNetMediaTests"
        ),
    ]
)
