# ApproximatelyUpMOD

MelonLoader mod for Approximately Up Demo.

## What is inside

- Harmony patches for gameplay tweaks.
- UniverseLib panel with item tools, resource toggles, and teleport helpers.
- Automatic GUI startup activation with fallback retries and diagnostics logs.

## Build requirements

- Visual Studio with .NET Framework 4.7.2 support.
- MelonLoader references from local game install.
- Game-managed assemblies (`Assembly-CSharp.dll`, Unity modules) from local install.

> The project references local game files by path. Update paths in `ApproximatelyUpMOD.csproj` if your installation location differs.

## Build

1. Restore NuGet packages.
2. Build `ApproximatelyUpMOD.csproj` in Release mode.
3. Copy generated DLL from `bin/Release` to your MelonLoader mods folder.

## Controls

- `F10`: Toggle mod panel visibility.

The mod now tries to open the panel automatically after scene load and logs each important initialization step in MelonLoader logs.
