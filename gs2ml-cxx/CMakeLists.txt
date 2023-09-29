# CMakeList.txt : CMake project for gsml-cxx, include source and define
# project specific logic here.
#
cmake_minimum_required (VERSION 3.8)
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set( CMAKE_VERBOSE_MAKEFILE on )
add_definitions(-DUNICODE)
message("${CMAKE_CURRENT_SOURCE_DIR}")

add_library(gs2ml-cxx SHARED "src/dllmain.cpp" "res/version.def")

target_include_directories(gs2ml-cxx PRIVATE "include/")

set_target_properties(gs2ml-cxx PROPERTIES OUTPUT_NAME "version")

add_custom_command(TARGET gs2ml-cxx POST_BUILD
	COMMAND ${CMAKE_COMMAND} -E make_directory ${Output_Bin_Dir}
    COMMAND ${CMAKE_COMMAND} -E make_directory "${Output_Bin_Dir}/gs2ml/mods"
    COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:gs2ml-cxx> ${Output_Bin_Dir}

)