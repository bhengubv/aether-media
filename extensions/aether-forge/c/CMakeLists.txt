cmake_minimum_required(VERSION 3.20)
project(aether_forge VERSION 1.0.0 LANGUAGES C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

add_library(aether_forge STATIC
    src/forge.c
)

target_include_directories(aether_forge
    PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
        $<INSTALL_INTERFACE:include>
)

target_compile_options(aether_forge PRIVATE
    $<$<C_COMPILER_ID:GNU,Clang>:-Wall -Wextra -Wpedantic>
    $<$<C_COMPILER_ID:MSVC>:/W4>
)

install(TARGETS aether_forge
    ARCHIVE DESTINATION lib
)
install(FILES include/aether_forge.h DESTINATION include)
