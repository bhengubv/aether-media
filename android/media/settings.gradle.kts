// Resolve aether-protocol-kotlin from the local monorepo instead of GitHub Packages.
// This substitution is active on all machines regardless of GITHUB_TOKEN availability.
includeBuild("../../../aether-protocol/kotlin") {
    dependencySubstitution {
        substitute(module("dev.aethermesh:aethermesh-protocol-kotlin")).using(project(":"))
    }
}

pluginManagement {
    repositories {
        google()
        mavenCentral()
        gradlePluginPortal()
    }
}
dependencyResolutionManagement {
    repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS)
    repositories {
        google()
        mavenCentral()
        maven { url = uri("https://maven.pkg.github.com/bhengubv/aether-protocol") }
    }
}
rootProject.name = "AetherMeshMedia"
include(":app")
