# ModTemplateValheim (Windows OS & Steam game)
Template mod for Valheim using Bepinex. Works for Steam, client-side only!
Everything required is **automatically installed into Valheim**:
 - BepInEx (Unstripped UnityEngine dlls too) + HarmonyX
 - Publicizes dlls from game as project dependencies
 - Moves your mod into the BepInEx plugins folder.

## How to use template
1. (optional) Rename the .sln file to rename the mod automatically.
   1. Change mod metadata in AssemblyInfo.cs.
   2. Rename code namespace in .cs files
2. Rebuild solution **twice**: PLAY!
3. Remove the code in the `Mod.Patches` folder and write your own mod :)

## Requirements
 - MSBuild 16+ (installed with Visual Studio)
 - If you want modern C# lang support you need https://dotnet.microsoft.com/en-us/download as well. See Directory.Build.props file to change language version.

## Troubleshooting
 - **Missing dependencies**
   - Run rebuild again (need to do it twice for fresh install)
 - **Code is red - IntelliSense broken**
   - Restart VS (after you've built solution twice)

## Credits
https://github.com/MrPurple6411 - First to make a proper template. Used their declared project dependencies as a base.  
https://github.com/sebastienvercammen - Providing help with overriding game DLLs with unstripped dlls through UnityDoorstop.
