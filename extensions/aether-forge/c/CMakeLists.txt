cmake_minimum_required(VERSION 3.20)
project(aethermesh_forge VERSION 1.0.0 LANGUAGES C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

add_library(aethermesh_forge STATIC
    src/forge.c
)

target_include_directories(aethermesh_forge
    PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
        $<INSTALL_INTERFACE:include>
)

target_compile_options(aethermesh_forge PRIVATE
    $<$<C_COMPILER_ID:GNU,Clang>:-Wall -Wextra -Wpedantic>
    $<$<C_COMPILER_ID:MSVC>:/W4>
)

install(TARGETS aethermesh_forge
    ARCHIVE DESTINATION lib
)
install(FILES include/aethermesh_forge.h DESTINATION include)
