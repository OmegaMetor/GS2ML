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

    // Prepare to execute the c# executable.

    STARTUPINFO si;
    PROCESS_INFORMATION pi;
    ZeroMemory(&si, sizeof(STARTUPINFO));
    si.cb = sizeof(STARTUPINFO);
    // Execute c# code
    std::filesystem::path csExePath = (game_path.parent_path() / "gs2ml" / "gs2ml-csharp.exe");
    #define max_size 5120
    wchar_t lpCommandLine[max_size] = L"\0";

    #define concat_cmd(cmdline, path) \
    wcscat_s(cmdline, max_size, L"\""); \
    wcscat_s(cmdline, max_size, (LPWSTR)path.c_str()); \
    wcscat_s(cmdline, max_size, L"\" ")

    concat_cmd(lpCommandLine, csExePath);
    concat_cmd(lpCommandLine, data_win_path);
    concat_cmd(lpCommandLine, game_path);

    std::wstring cliString;
    
    if(argc > 1){
        for (int i = 1; i < argc; ++i) {
            std::wstring arg(argv[i]);
            cliString += L'"' + arg + L'"'; // Quote each argument and add to the command line
            cliString += L' '; // Add a space between arguments
        }
    }
    
    // Remove the trailing space
    if (!cliString.empty()) {
        cliString.pop_back();
    }

    wcscat_s(lpCommandLine, max_size, cliString.c_str());

    int error;
    error = CreateProcess(csExePath.c_str(), lpCommandLine, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi);
    if(0 == error)
    {
        std::cout << "ERROR: " << GetLastError() << std::endl;
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
    for (int i = 0; i < argc; ++i)
    {
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