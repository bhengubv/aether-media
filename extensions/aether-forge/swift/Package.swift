// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "AetherNetForge",
    platforms: [
        .iOS(.v15),
        .macOS(.v12)
    ],
    products: [
        .library(
            name: "AetherNetForge",
            targets: ["AetherNetForge"]
        )
    ],
    targets: [
        .target(
            name: "AetherNetForge",
            path: "Sources/AetherNetForge"
        ),
        .testTarget(
            name: "AetherNetForgeTests",
            dependencies: ["AetherNetForge"],
            path: "Tests/AetherNetForgeTests"
        )
    ]
)
