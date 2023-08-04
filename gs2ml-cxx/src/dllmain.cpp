// thanks to Adaf for helping me figure out the proxy dll loading stuff

// ReSharper disable CppZeroConstantCanBeReplacedWithNullptr
#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <filesystem>
#include <shellapi.h>
#include <iostream>
#include <fstream>
#include <DbgHelp.h>
#include <chrono>
#include <thread>


constexpr auto PROXY_DLL = TEXT("version.dll");
constexpr auto PROXY_MAX_PATH = 260;

#define DLL_PROXY_ORIGINAL(name) original_##name

#define DLL_NAME(name)                \
    FARPROC DLL_PROXY_ORIGINAL(name); \
    void _##name() {                  \
        DLL_PROXY_ORIGINAL(name)();   \
    }
#include "proxy.h"

#undef DLL_NAME

std::filesystem::path getSystemDirectory() {
    const auto systemDirectory(std::make_unique<TCHAR[]>(PROXY_MAX_PATH));
    ::GetSystemDirectory(systemDirectory.get(), PROXY_MAX_PATH);
    return {systemDirectory.get()};
}

bool hasLoaded = false;
bool hasGameArg = false;


bool loadProxy() {
    const auto libPath = getSystemDirectory() / PROXY_DLL;
    const auto lib = LoadLibrary(libPath.c_str());
    if(!lib) return false;

    #define DLL_NAME(name) DLL_PROXY_ORIGINAL(name) = GetProcAddress(lib, ###name);
    #include "proxy.h"
    DLL_NAME(GetFileVersionInfoSizeA)
    #undef DLL_NAME

    return true;
}

void loadMods() {
    // Check if the game is being launched with the -game argument.
    if (hasGameArg || hasLoaded) return;
    hasLoaded = true;
    LPWSTR lpCmdLine = GetCommandLine();
    std::wstring commandLine(lpCmdLine);

    int argc;
    LPWSTR* argv = CommandLineToArgvW(lpCmdLine, &argc);
    LPWSTR executable(argv[0]);
    wchar_t buffer[MAX_PATH];
    GetModuleFileName(NULL, buffer, sizeof(buffer));
    std::filesystem::path game_directory = std::filesystem::path(buffer).parent_path();
    for (int i = 0; i < argc; ++i)
    {
        std::wstring argument(argv[i]);
        if (argument == L"-game")
            hasGameArg = true;
    }
    if (hasGameArg)
        return;

    // End check

    std::filesystem::path folder = std::filesystem::path(buffer).parent_path() / "data.win";
    
    // Prepare to execute the c# executable.
    LPWSTR lpCommandLine((LPWSTR)folder.c_str());
    for (int i = 1; i < argc; ++i) {
        wcscat_s(lpCommandLine, MAX_PATH, L" ");
        wcscat_s(lpCommandLine, MAX_PATH, argv[i]);
    }

    STARTUPINFO si;
    PROCESS_INFORMATION pi;
    ZeroMemory(&si, sizeof(STARTUPINFO));
    si.cb = sizeof(STARTUPINFO);
    // Execute c# code
    std::filesystem::path csExePath = (game_directory / "gs2ml" / "gs2ml-csharp.exe");
    int error;
    error = CreateProcess(csExePath.c_str(), lpCommandLine, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi);
    if(0 != error)
    {
        std::cout << error << std::endl;
    }

    LocalFree(argv);
}

DWORD WINAPI ThreadProc(LPVOID lpParam)
{
    SuspendThread(lpParam); 
    loadMods();
    //ResumeThread(lpParam);
    //exit(0);
    return 0;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
    bool shouldLoad = false;
    switch(ul_reason_for_call) {
        case DLL_PROCESS_ATTACH:
            AllocConsole();
            freopen_s((FILE**)stdout, "CONOUT$", "w", stdout);
            if(!loadProxy()) {
                return FALSE;
            }
            shouldLoad = true;
            break;
        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
        case DLL_PROCESS_DETACH:
        default:
            break;
    }
    if (shouldLoad) {
        LPWSTR lpCmdLine = GetCommandLine();
        std::wstring commandLine(lpCmdLine);
        int argc;
        LPWSTR* argv = CommandLineToArgvW(lpCmdLine, &argc);
        for (int i = 0; i < argc; ++i)
        {
            std::wstring argument(argv[i]);
            if (argument == L"-game")
                hasGameArg = true;
        }
        if (!hasGameArg) {
            HANDLE thisThread;
            DuplicateHandle(GetCurrentProcess(), GetCurrentThread(), GetCurrentProcess(), &thisThread, 0, FALSE, DUPLICATE_SAME_ACCESS);
            HANDLE loaderThread = CreateThread(NULL, 0, ThreadProc, thisThread, 0, NULL);
            if (loaderThread == 0) return FALSE;
        }
    }
    return TRUE;
}