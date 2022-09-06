# Beat Saber - Tilt Five v0.0.1

This is a proof-of-concept mod for Beat Saber that adds [Tilt Five](https://www.tiltfive.com/) support to the game.

Video here: https://youtu.be/dH0xym4nFSc

## Notes / Caveats

- Only one Tilt Five wand is currently supported.
- You cannot interact with menus using the Tilt Five wand - it is only set as the right saber. Use your mouse for interface navigation.
- The game board should be viewed from the bottom-left corner!
- Wand motions are optimized to make it possible to pass maps - the key is to play "gently", your swings will be amplified!

## Installation

_Assuming you have already modded Beat Saber:_

1. Download the zip from the [latest release here](https://github.com/SteffanDonal/BeatSaber-TiltFive/releases).
2. Unzip the folder into your Beat Saber install directory.
3. Add `fpfc` to Beat Saber's launch options. _Note: You will want to remove this option when you don't want to play using Tilt Five_

## Usage

1. Make sure `fpfc` is set in Beat Saber's launch options.
2. If you have SteamVR installed, unplug your headset if you don't want it to run. This mod will disable VR support proactively, but SteamVR will still load.
3. Run the game, and plug in your Tilt Five glasses at any time to view/play!
4. You may use your mouse via the Beat Saber window to interact with menus. Press `ESC` to exit maps quickly.
5. Make sure you either play only Zen Mode or lightshow maps, or single-saber maps only - you'll fail very quickly otherwise 😉

## Development Setup

1. **Make sure your copy of Beat Saber is already fully modded.** Short version: Install Beat Saber, run it, run [ModAssistant](https://github.com/Assistant/ModAssistant/releases) and install the core mods, then run Beat Saber once more.
2. If you have the [Beat Saber Modding Tools](https://github.com/Zingabopp/BeatSaberModdingTools) extension, open the solution, right click the project name in the Solution Explorer, and select `Beat Saber Modding Tools -> Set Beat Saber Directory...`
3. If you don't have the mentioned extension, create a file called `TiltFive.csproj.user` add the following to it.

```
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <!-- Make sure to change this path if your Beat Saber installation is saved somewhere else. -->
    <BeatSaberDir>C:\Program Files (x86)\Steam\steamapps\common\Beat Saber</BeatSaberDir>
  </PropertyGroup>
</Project>
```

4. Open the solution, and build.
5. By default, it will automatically copy to your Beat Saber plugins folder. If for some reason you want to disable this, add `<DisableCopyToGame>true</DisableCopyToGame>` to the PropertyGroup in the .user file, build the project, navigate to /bin/Plugins, copy the dll into your Plugins folder and you're done!
6. Optionally, add `--verbose` to Beat Saber's launch options so you can view log output in real-time.