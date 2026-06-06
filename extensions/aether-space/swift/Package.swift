// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "AetherMeshSpace",
    platforms: [
        .iOS(.v15),
        .macOS(.v12)
    ],
    products: [
        .library(
            name: "AetherMeshSpace",
            targets: ["AetherMeshSpace"]
        )
    ],
    targets: [
        .target(
            name: "AetherMeshSpace",
            path: "Sources/AetherMeshSpace"
        ),
        .testTarget(
            name: "AetherMeshSpaceTests",
            dependencies: ["AetherMeshSpace"],
            path: "Tests/AetherMeshSpaceTests"
        )
    ]
)
