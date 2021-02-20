# ModTemplateValheim (Windows OS & Steam game)
Template mod for Valheim using Bepinex. Contains example patch for extra in-game console command.
Everything required is automatically installed:
 - BepInEx (Unstripped UnityEngine dlls too) + HarmonyX
 - Publicizes dlls from game dlls as dependencies
 - Moves your mod into the BepInEx plugins folder.

## How to use template
 - Rebuild solution **twice**: PLAY!

## Requirements
 - MSBuild 16+ (installed with Visual Studio)
 - Git (used to download & build Publicizer to publicize the game DLLs)
 - If you want C# 9 support you need https://dotnet.microsoft.com/download/dotnet/5.0 as well or change projects to use C# 8.

## Credits
https://github.com/MrPurple6411 - First to make a proper template. Used the proj dependencies from it as a base.  
https://github.com/sebastienvercammen - Providing help with overriding game dlls with unstripped dlls through UnityDoorstop.
