cmake_minimum_required(VERSION 3.20)
project(aethernet_forge VERSION 1.0.0 LANGUAGES C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

add_library(aethernet_forge STATIC
    src/forge.c
)

target_include_directories(aethernet_forge
    PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
        $<INSTALL_INTERFACE:include>
)

target_compile_options(aethernet_forge PRIVATE
    $<$<C_COMPILER_ID:GNU,Clang>:-Wall -Wextra -Wpedantic>
    $<$<C_COMPILER_ID:MSVC>:/W4>
)

install(TARGETS aethernet_forge
    ARCHIVE DESTINATION lib
)
install(FILES include/aethernet_forge.h DESTINATION include)
