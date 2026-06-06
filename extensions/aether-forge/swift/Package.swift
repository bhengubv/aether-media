// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "AetherMeshForge",
    platforms: [
        .iOS(.v15),
        .macOS(.v12)
    ],
    products: [
        .library(
            name: "AetherMeshForge",
            targets: ["AetherMeshForge"]
        )
    ],
    targets: [
        .target(
            name: "AetherMeshForge",
            path: "Sources/AetherMeshForge"
        ),
        .testTarget(
            name: "AetherMeshForgeTests",
            dependencies: ["AetherMeshForge"],
            path: "Tests/AetherMeshForgeTests"
        )
    ]
)
