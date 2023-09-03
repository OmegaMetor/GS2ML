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
#include <intrin.h>
#include <vector>

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
    wchar_t SystemDirectoryPath[MAX_PATH] = { 0 };
    
    if (!GetSystemDirectoryW(SystemDirectoryPath, MAX_PATH))
        std::cout << "GetSystemDirectoryW fails: " << GetLastError() << std::endl;

    return SystemDirectoryPath;
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

void loadMods(const std::vector<std::wstring>& originalArgs) {
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
    std::filesystem::path game_path = std::filesystem::path(buffer);
    for (int i = 0; i < argc; ++i)
    {
        std::wstring argument(argv[i]);
        if (argument == L"-game")
            hasGameArg = true;
    }
    if (hasGameArg)
        return;

    // End check

    std::filesystem::path data_win_path = std::filesystem::path(buffer).parent_path() / "data.win";

    // Prepare to execute the C# executable.

    STARTUPINFO si;
    PROCESS_INFORMATION pi;
    ZeroMemory(&si, sizeof(STARTUPINFO));
    si.cb = sizeof(STARTUPINFO);

    // Build the command line arguments for the C# executable.
    std::wstring csExeArgs;
    for (const auto& arg : originalArgs) {
        csExeArgs += L"\"";
        csExeArgs += arg;
        csExeArgs += L"\" ";
    }

    std::filesystem::path csExePath = (game_path.parent_path() / "gs2ml" / "gs2ml-csharp.exe");

    int error;
    error = CreateProcess(
        csExePath.c_str(),
        csExeArgs.data(),
        NULL,
        NULL,
        FALSE,
        0,
        NULL,
        NULL,
        &si,
        &pi
    );

    if (error == 0)
    {
        std::wcout << L"ERROR: " << GetLastError() << std::endl;
    }
    else
    {
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
    }

    LocalFree(argv);
}

DWORD WINAPI ThreadProc(LPVOID lpParam)
{
    SuspendThread(lpParam); 
    loadMods();
    ResumeThread(lpParam);
    exit(0);
    return 0;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
    if (ul_reason_for_call != DLL_PROCESS_ATTACH)
        return TRUE;

    AllocConsole();
    freopen_s((FILE**)stdout, "CONOUT$", "w", stdout);
    if (!loadProxy()) {
        return FALSE;
    }

    LPWSTR lpCmdLine = GetCommandLine();
    std::wstring commandLine(lpCmdLine);
    int argc;
    LPWSTR* argv = CommandLineToArgvW(lpCmdLine, &argc);

    // Create a vector to store the original command line arguments.
    std::vector<std::wstring> originalArgs;
    for (int i = 0; i < argc; ++i)
    {
        originalArgs.push_back(argv[i]);
        std::wstring argument(argv[i]);
        if (argument == L"-game")
            hasGameArg = true;
    }

    if (!hasGameArg) {
        HANDLE thisThread = OpenThread(THREAD_ALL_ACCESS, FALSE, GetCurrentThreadId());
        HANDLE loaderThread = CreateThread(NULL, 0, ThreadProc, thisThread, 0, NULL);
        if (loaderThread == 0) 
            return FALSE;
    }

    return TRUE;
}
