cmake_minimum_required (VERSION 3.8)

set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_VERBOSE_MAKEFILE ON)

set(CMAKE_CXX_STANDARD 17)

add_library(gs2ml-interop MODULE
    src/interop.cpp
)

find_package(PkgConfig)

if(PkgConfig_FOUND)
    pkg_check_modules(MONO REQUIRED monosgen-2)
    target_include_directories(gs2ml-interop PRIVATE ${MONO_INCLUDE_DIRS})
    target_link_directories(gs2ml-interop PRIVATE ${MONO_LIBRARY_DIRS})
    target_link_libraries(gs2ml-interop ${MONO_LIBRARIES})
else()
    target_include_directories(gs2ml-interop PRIVATE "${CMAKE_SOURCE_DIR}/gs2ml-interop/lib/mono-2.0")
    target_link_directories(gs2ml-interop PRIVATE "${CMAKE_SOURCE_DIR}/gs2ml-interop/lib")
    target_link_libraries(gs2ml-interop 
        "mono-2.0-sgen"
        "ws2_32"
        "psapi"
        "ole32"
        "winmm"
        "oleaut32"
        "advapi32"
        "version"
    )
endif()

add_compile_definitions(OS_Windows)

set_target_properties(gs2ml-interop PROPERTIES PREFIX "")

add_custom_command(TARGET gs2ml-interop POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_directory "${CMAKE_SOURCE_DIR}/gs2ml-interop/lib/4.5" "${Output_Bin_Dir}/gs2ml/lib/mono/4.5"
    COMMAND ${CMAKE_COMMAND} -E copy "${CMAKE_SOURCE_DIR}/gs2ml-interop/lib/mono-2.0-sgen.dll" "${Output_Bin_Dir}/gs2ml"
    COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:gs2ml-interop> "${Output_Bin_Dir}/gs2ml"
)