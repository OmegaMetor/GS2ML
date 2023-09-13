---
title: Installing GS2ML
description: GS2ML Installation Guide
sidebar:
  order: 1
---

There are a few different ways to install GS2ML. These range in complexity but sometimes can be needed.

## Runtime Dependencies
GS2ML has a dependency required to run it. No matter how you install gs2ml, you must install the following to use it.

### Installing DotNet
The main dependency is the Dotnet Runtime 6.0.
To install this, go to [this website](https://dotnet.microsoft.com/en-us/download/dotnet/6.0). From here, you should download the most recent version of the `.NET Runtime`
alternatively, you can use winget (if you have it installed) by running `winget install Microsoft.DotNet.Runtime.6`.

## Installing from Github Releases
Releases are published by github actions every time there is a commit pushed to the repo. These will be the most up to date and also the most likely to contain unknown bugs and issues. However, this is the reccomended method for installing it simply due to it's ease.

### Getting the release
You can download the latest release from [the releases page](https://github.com/OmegaMetor/GS2ML/releases). For most people, the `gs2ml-win64.zip` is the only one you need. (If you want to make mods, [check out modding guide!](/GS2ML/guides/Mod%20Development/mod-development.md))

### Locating your game files
Next, you'll want to find the place where your game is installed. This folder should contain an exe for the game and a data.win file for the games data. For games installed through Steam, you can use the browse local files option to easily open it.

### Extracting GS2ML
Now you'll want to open the zip you downloaded earlier. In this, find the `gs2ml/` folder and the `version.dll`. Put both of these files in the games install folder as shown below:
```
.
└── Game Install Folder/
    ├── game.exe
    ├── data.win
    ├── version.dll
    └── gs2ml/
```

## Compiling GS2ML
GS2ML uses the CMake toolchain generator to make building fairly easy. (Honestly, the only reason you should do this is to contribute, so we await your pull requests!)

### Compilation Requirements
There are a few extra things needed to compile gs2ml.

#### Visual Studio 2022
Visual Studio 2022 is required. You can download this from [microsoft's website](https://visualstudio.microsoft.com/vs/features/cplusplus/). Community should have everything you need.
Note the link above should include the specific parts required to build gs2ml. If you use some other installation method, please continue.

When installing Visual Studio, you must include the c++ toolchain and cmake support, along with the Dotnet 6.0 SDK.

### Building GS2ML
Once you've installed visual studio and the specific components, you now can clone this repository. Open the cloned repository in Visual Studio, then build with `control+shift+B`. If you then open the project in explorer, there should be a `out/bin` folder. Copy the contents of this folder to your game's folder as in the Extracting GS2ML step above, then you should be good to go.

## Final Step
No matter which way you install it, you gotta try it at the end.
### Running the game
Now, just run the game! If it worked, you should see a console window open, show some logs, then relaunch the game with a new console window, then open as normal. If this doesn't happen, you'll probably be shown some logs, so either figure them out, or ask on discord (server TDB)
