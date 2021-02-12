# MethodCallCommand-Valheim
Valheim mod using Bepinex that can call methods from the in-game console

## Requirements
- [Bepinex](https://github.com/BepInEx/BepInEx/releases)
- [Harmony](https://github.com/pardeike/Harmony)

## How to use
- Move mod into `..\Valheim\BepInEx\plugins`
- Start game. Press F5 to open console.
- Enter a command in format: `call <CLASS> <METHOD> [arg1] [arg2]` like `Console AddString "Hello, World!"`
