// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "AetherSpace",
    platforms: [
        .iOS(.v15),
        .macOS(.v12)
    ],
    products: [
        .library(
            name: "AetherSpace",
            targets: ["AetherSpace"]
        )
    ],
    targets: [
        .target(
            name: "AetherSpace",
            path: "Sources/AetherSpace"
        ),
        .testTarget(
            name: "AetherSpaceTests",
            dependencies: ["AetherSpace"],
            path: "Tests/AetherSpaceTests"
        )
    ]
)
