// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "AetherMeshVault",
    platforms: [
        .iOS(.v15),
        .macOS(.v12)
    ],
    products: [
        .library(
            name: "AetherMeshVault",
            targets: ["AetherMeshVault"]
        )
    ],
    targets: [
        .target(
            name: "AetherMeshVault",
            path: "Sources/AetherMeshVault"
        ),
        .testTarget(
            name: "AetherMeshVaultTests",
            dependencies: ["AetherMeshVault"],
            path: "Tests/AetherMeshVaultTests"
        )
    ]
)
