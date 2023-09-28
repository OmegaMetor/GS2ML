cmake_minimum_required (VERSION 3.8)

set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_VERBOSE_MAKEFILE ON)

set(CMAKE_CXX_STANDARD 17)

find_package(PkgConfig REQUIRED)

pkg_check_modules(MONO REQUIRED monosgen-2)

add_library(gs2ml-interop MODULE
    src/interop.cpp
)

add_compile_definitions(OS_Windows)

target_include_directories(gs2ml-interop PRIVATE ${MONO_INCLUDE_DIRS})
target_link_directories(gs2ml-interop PRIVATE ${MONO_LIBRARY_DIRS})
target_link_libraries(gs2ml-interop ${MONO_LIBRARIES})

set_target_properties(gs2ml-interop PROPERTIES PREFIX "")

add_custom_command(TARGET gs2ml-interop POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_directory "${MONO_LIBRARY_DIRS}/mono/4.5" "${Output_Bin_Dir}/gs2ml/lib/mono/4.5"
    COMMAND ${CMAKE_COMMAND} -E copy "${MONO_PREFIX}/bin/mono-2.0-sgen.dll" "${Output_Bin_Dir}/gs2ml"
    COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:gs2ml-interop> "${Output_Bin_Dir}/gs2ml"
)