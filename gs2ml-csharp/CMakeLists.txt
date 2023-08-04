
cmake_minimum_required (VERSION 3.8)

add_custom_target(gsml-csharp ALL
	COMMAND dotnet build
	WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR})

add_custom_command(TARGET gsml-csharp POST_BUILD
	COMMAND ${CMAKE_COMMAND} -E make_directory "${Output_Bin_Dir}/gs2ml"
    COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_CURRENT_SOURCE_DIR}/bin/Debug/net6.0/gs2ml-csharp.exe "${Output_Bin_Dir}/gs2ml/"
    COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_CURRENT_SOURCE_DIR}/bin/Debug/net6.0/gs2ml-csharp.dll "${Output_Bin_Dir}/gs2ml/"
    COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_CURRENT_SOURCE_DIR}/bin/Debug/net6.0/gs2ml-csharp.runtimeconfig.json "${Output_Bin_Dir}/gs2ml/"
    COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_CURRENT_SOURCE_DIR}/bin/Debug/net6.0/gs2ml-csharp.deps.json "${Output_Bin_Dir}/gs2ml/"
    COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_CURRENT_SOURCE_DIR}/bin/Debug/net6.0/UndertaleModLib.dll "${Output_Bin_Dir}/gs2ml/"
    COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_CURRENT_SOURCE_DIR}/bin/Debug/net6.0/ICSharpCode.SharpZipLib.dll "${Output_Bin_Dir}/gs2ml/"
    COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_CURRENT_SOURCE_DIR}/bin/Debug/net6.0/Microsoft.Win32.SystemEvents.dll "${Output_Bin_Dir}/gs2ml/"
    COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_CURRENT_SOURCE_DIR}/bin/Debug/net6.0/PropertyChanged.dll "${Output_Bin_Dir}/gs2ml/"
    COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_CURRENT_SOURCE_DIR}/bin/Debug/net6.0/System.Drawing.Common.dll "${Output_Bin_Dir}/gs2ml/"
    COMMAND ${CMAKE_COMMAND} -E copy_directory ${CMAKE_CURRENT_SOURCE_DIR}/bin/Debug/net6.0/runtimes "${Output_Bin_Dir}/gs2ml/runtimes"

)