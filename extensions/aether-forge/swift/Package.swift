// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "AetherForge",
    platforms: [
        .iOS(.v15),
        .macOS(.v12)
    ],
    products: [
        .library(
            name: "AetherForge",
            targets: ["AetherForge"]
        )
    ],
    targets: [
        .target(
            name: "AetherForge",
            path: "Sources/AetherForge"
        ),
        .testTarget(
            name: "AetherForgeTests",
            dependencies: ["AetherForge"],
            path: "Tests/AetherForgeTests"
        )
    ]
)
