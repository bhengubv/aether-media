// swift-tools-version:5.9
import PackageDescription

let package = Package(
    name: "AetherMeshMedia",
    platforms: [
        .iOS(.v16),
        .macOS(.v13),
    ],
    products: [
        .library(
            name: "AetherMeshMedia",
            targets: ["AetherMeshMedia"]
        ),
    ],
    dependencies: [
        // aether-protocol: integrated at runtime via Aether mesh — not a compile-time dependency
    ],
    targets: [
        .target(
            name: "AetherMeshMedia",
            dependencies: [],
            path: "Sources/AetherMeshMedia"
        ),
        .testTarget(
            name: "AetherMeshMediaTests",
            dependencies: ["AetherMeshMedia"],
            path: "Tests/AetherMeshMediaTests"
        ),
    ]
)
