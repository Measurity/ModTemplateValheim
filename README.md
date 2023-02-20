# ModTemplateValheim (Windows OS & Steam game)
Template mod for Valheim using Bepinex. Works for Steam, client-side only!
Everything required is **automatically installed into Valheim**:
 - BepInEx (Unstripped UnityEngine dlls too) + HarmonyX
 - Publicizes dlls from game as project dependencies
 - Moves your mod into the BepInEx plugins folder.

## How to use template
1. Download or clone this repository then open in Visual Studio or JetBrains Rider.
2. (optional) Rename the `ExampleCallMethodMod.csproj` to your mod name. 
3. Rebuild solution **twice**: PLAY!

Any project in this solution that ends with `Mod` will automatically install itself into Valheim and reference the game code.

## Requirements
 - [MSBuild 16+](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022) (also installed with Visual Studio)
 - If you want modern C# lang support you need https://dotnet.microsoft.com/en-us/download as well. See Directory.Build.props file to change language version.

## Troubleshooting
 - **Missing dependencies**
   - Run rebuild again (need to do it twice for fresh install)
 - **Code is red - IntelliSense broken**
   - Restart VS (after you've built solution twice)
 - **Valheim has updated, getting errors**
   - Remove folder `..\ModTemplateValheim\BuildTool\bin\generated_files` and rebuild solution.

## I want to..

- **Rename my mod**  
  Add (or change) the `AssemblyName` property to the mod's .csproj file. Note to run `clean` on this solution beforehand to uninstall your mods. Example:
```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <AssemblyName>My cool mod name</AssemblyName>
    </PropertyGroup>
</Project>
```
- **Change mod author or version**  
  Add (or change) the `Authors` property to the mod's .csproj file. Example:
```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <Authors>Measurity;geocine;eai04191</Authors>
        <AssemblyName>My cool mod - the second</AssemblyName>
        <Version>1.0.0.1</Version>
    </PropertyGroup>
</Project>
```
Which will generate the BepInEx plugin metadata as:
```cs
public const string PluginAuthor = "Measurity & geocine & eai04191";
public const string PluginGuid = "com.github.measurity.Mycoolmodthesecond";
public const string PluginName = "My cool mod - the second";
public const string PluginVersion = "1.0.0.1";
```

## Credits
https://github.com/MrPurple6411 - First to make a proper template. Used their declared project dependencies as a base.  
https://github.com/sebastienvercammen - Providing help with overriding game DLLs with unstripped dlls through UnityDoorstop.
