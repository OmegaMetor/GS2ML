
cmake_minimum_required (VERSION 3.8)

add_custom_target(gs2ml-interopbridge ALL
	COMMAND dotnet build
	WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR})

add_custom_command(TARGET gs2ml-interopbridge POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_CURRENT_SOURCE_DIR}/bin/Debug/net6.0/gs2ml-interopbridge.dll "${Output_Bin_Dir}/gs2ml/"
    COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_CURRENT_SOURCE_DIR}/bin/Debug/net6.0/gs2ml-interopbridge.pdb "${Output_Bin_Dir}/gs2ml/"
    COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_CURRENT_SOURCE_DIR}/bin/Debug/net6.0/gs2ml-interopbridge.deps.json "${Output_Bin_Dir}/gs2ml/"
)