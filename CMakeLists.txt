# CMakeList.txt : Top-level CMake project file, do global configuration
# and include sub-projects here.
#

cmake_minimum_required (VERSION 3.8)

project ("gsml")

set(Output_Bin_Dir "${CMAKE_SOURCE_DIR}/out/bin/")


# Include sub-projects.
add_subdirectory ("gs2ml-cxx")

add_subdirectory("gs2ml-csharp")