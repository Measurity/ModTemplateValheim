# ModTemplateValheim (Windows OS & Steam game)
Template mod for Valheim using Bepinex. Works for Steam, client-side only!
Everything required is **automatically installed into Valheim**:
 - BepInEx (Unstripped UnityEngine dlls too) + HarmonyX
 - Publicizes dlls from game dlls as dependencies
 - Moves your mod into the BepInEx plugins folder.

## How to use template
 - Rebuild solution **twice**: PLAY!
 - Rename the ModTemplateValheim.sln to rename mod. Things not yet automatically renamed:
   - Code namespaces
   - The BepInPugin attribute on Mod.cs.
   - Mod name in /AssemblyInfo.cs (shared between all projects)

## Requirements
 - MSBuild 16+ (installed with Visual Studio)
 - If you want C# 9 support you need https://dotnet.microsoft.com/download/dotnet/5.0 as well or change projects to use C# 8. See Directory.Build.props file to change language version.

## Credits
https://github.com/MrPurple6411 - First to make a proper template. Used the proj dependencies from it as a base.  
https://github.com/sebastienvercammen - Providing help with overriding game dlls with unstripped dlls through UnityDoorstop.
