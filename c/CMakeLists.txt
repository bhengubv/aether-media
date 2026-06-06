cmake_minimum_required(VERSION 3.16)
project(aethermesh_media VERSION 1.0.0 LANGUAGES C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

# ── Library ────────────────────────────────────────────────────────────────────
add_library(aethermesh_media STATIC
    src/player/vlc_player.c
    src/streaming/stream_client.c
    src/social/social_graph.c
)
target_include_directories(aethermesh_media PUBLIC include)

# LibVLC (optional — graceful fallback if not found)
find_package(PkgConfig)
if(PkgConfig_FOUND)
    pkg_check_modules(LIBVLC libvlc)
    if(LIBVLC_FOUND)
        message(STATUS "LibVLC found: ${LIBVLC_LIBRARIES}")
        target_compile_definitions(aethermesh_media PRIVATE AETHERMESH_HAVE_LIBVLC=1)
        target_link_libraries(aethermesh_media PUBLIC ${LIBVLC_LIBRARIES})
        target_include_directories(aethermesh_media PRIVATE ${LIBVLC_INCLUDE_DIRS})
    else()
        message(WARNING "LibVLC not found — vlc_player will compile as a stub-free no-op")
        target_compile_definitions(aethermesh_media PRIVATE AETHERMESH_HAVE_LIBVLC=0)
    endif()
else()
    target_compile_definitions(aethermesh_media PRIVATE AETHERMESH_HAVE_LIBVLC=0)
endif()

# ── Demo executable ────────────────────────────────────────────────────────────
add_executable(aethermesh_media_demo src/main.c)
target_link_libraries(aethermesh_media_demo aethermesh_media)

# ── Tests ──────────────────────────────────────────────────────────────────────
enable_testing()
add_executable(test_social tests/test_social.c)
target_link_libraries(test_social aethermesh_media)
add_test(NAME social_graph COMMAND test_social)
