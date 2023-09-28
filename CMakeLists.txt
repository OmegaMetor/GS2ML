# CMakeList.txt : Top-level CMake project file, do global configuration
# and include sub-projects here.
#

cmake_minimum_required (VERSION 3.27)

project ("gsml")
include(FetchContent)

FetchContent_Declare(
    undertalemodtool
    GIT_REPOSITORY https://github.com/krzys-h/UndertaleModTool.git
    GIT_TAG 7b876ad457eca5cdd69957dc02ef57a569412e5e
)

FetchContent_MakeAvailable(undertalemodtool)

file(COPY ${undertalemodtool_SOURCE_DIR} DESTINATION ${CMAKE_SOURCE_DIR})

set(Output_Bin_Dir "${CMAKE_SOURCE_DIR}/out/bin/")


# Include sub-projects.
add_subdirectory ("gs2ml-cxx")

add_subdirectory("gs2ml-csharp")

add_subdirectory("gs2ml-interop")

add_subdirectory("gs2ml-interopbridge")