# Renderite.Godot

A work-in-progress alternative rendering frontend for Resonite.

[Uses Godot 4.5 Beta 5 .NET](https://godotengine.org/article/dev-snapshot-godot-4-5-beta-5/)

[Requires a ResoniteModloader mod to fix shaders](https://github.com/Frozenreflex/Renderite.Godot.Patches)

## Building and running
If you have a Resonite prerelease install inside the default steamapps directory (`~/.local/share/Steam/steamapps/common/Resonite` on Linux or `C:\Program Files (x86)\Steam\steamapps\common\Resonite` on Windows) it should work out of the box. Otherwise you'll have to add your path inside `Renderite.Godot.csproj` and modify your launch args by adding `--resonitepath <path>` at the beginning (Debug -> Customize Run Instances if running in the editor).

You can also change the dotnet executable with `--executable <path>`. Resonite args are specified after `--resoniteargs`.

### Run Renderite.Host.dll separately
By default Renderite.Godot handles launching Resonite for you. If you'd like to launch it independently you can use `--noautolaunch`, which will simply print out the shmprefix you have to give to Renderite.Host.dll:
```
dotnet Renderite.Host.dll -shmprefix <prefix here> -LoadAssembly Libraries/ResoniteModLoader.dll
```
