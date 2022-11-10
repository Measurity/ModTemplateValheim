# ModTemplateValheim (Windows OS & Steam game)
Template mod for Valheim using Bepinex. Works for Steam, client-side only!
Everything required is **automatically installed into Valheim**:
 - BepInEx (Unstripped UnityEngine dlls too) + HarmonyX
 - Publicizes dlls from game as project dependencies
 - Moves your mod into the BepInEx plugins folder.

## How to use template
 - Rebuild solution **twice**: PLAY!
 - Rename the ModTemplateValheim.sln to rename mod. Things not yet automatically renamed:
   - Code namespaces
   - The BepInPugin attribute on Mod.cs.

## Requirements
 - MSBuild 16+ (installed with Visual Studio)
 - If you want modern C# lang support you need https://dotnet.microsoft.com/en-us/download as well. See Directory.Build.props file to change language version.

## Credits
https://github.com/MrPurple6411 - First to make a proper template. Used the proj dependencies from it as a base.  
https://github.com/sebastienvercammen - Providing help with overriding game dlls with unstripped dlls through UnityDoorstop.
