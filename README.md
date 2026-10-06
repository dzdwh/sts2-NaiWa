# NaiWa — Slay the Spire 2 Character Mod

NaiWa (奶蛙) is a custom character mod for Slay the Spire 2. This repository contains the current C# source, Godot scene/resource definitions, Chinese and English localization, and the version-selecting loader.

## English translation contributions

Contributions are welcome! Start with [CONTRIBUTING.md](CONTRIBUTING.md).

- English: [`mod/naiwa/naiwa/localization/eng/`](mod/naiwa/naiwa/localization/eng/)
- Simplified Chinese reference: [`mod/naiwa/naiwa/localization/zhs/`](mod/naiwa/naiwa/localization/zhs/)
- Use **NaiWa**, not **Milk Frog**, for the character's name.
- Fork the repository, edit the English JSON files, and open a pull request. No direct repository write access is needed.

## Source layout

| Directory | Purpose |
| --- | --- |
| `mod/naiwa/` | Current game-version implementation (0.111.0) |
| `mod/naiwa_loader/` | Cross-platform, version-selecting DLL loader |
| `variants/0.107.1/mod/naiwa/` | Compatibility implementation for game version 0.107.1 |
| `scripts/` | Dependency-free localization validation |

This is a clean source snapshot of the development workspace, including current uncommitted mod changes. Private development history is not included. It is not a packaged Steam Workshop release.

## Building

Requirements: .NET 9, Godot .NET 4.5.1, your own legitimately installed Slay the Spire 2 game assemblies, and RitsuLib. Game assemblies and third-party dependency binaries are not redistributed.

```powershell
dotnet build mod/naiwa/naiwa.csproj -c Release -p:Sts2DataDir="PATH_TO_MATCHING_GAME_ASSEMBLIES" -p:RitsuLibRoot="PATH_TO_RITSULIB_INSTALLATION"
dotnet build mod/naiwa_loader/naiwa_loader.csproj -c Release -p:Sts2DataDir="PATH_TO_GAME_ASSEMBLIES"
dotnet build variants/0.107.1/mod/naiwa/naiwa.csproj -c Release -p:Sts2DataDir="PATH_TO_0.107_GAME_ASSEMBLIES" -p:RitsuLibRoot="PATH_TO_RITSULIB_INSTALLATION"
```

The loader is installed as `mods/naiwa/naiwa.dll`; gameplay DLLs are installed under `mods/naiwa/lib/<game-version>/naiwa.dll`. Do not overwrite the loader with a gameplay DLL.

Godot scene definitions retain their original resource references. Binary artwork, Spine skeleton/atlas data, audio/video, exported PCK files, editor plugins and third-party binaries are intentionally not included in this source-only publication. Therefore this checkout alone is not a complete PCK-export asset bundle. Translation work does not require those assets or a local game build.

Public builds have telemetry disabled by default. No production endpoint credential, private server configuration, player data or bot files are included. Translation pull requests must not enable telemetry or add credentials.

## License

Project code is provided under the existing [MIT License](LICENSE). This license does not grant rights to Slay the Spire 2, RitsuLib, third-party libraries or separately distributed artwork/audio. Those remain subject to their respective owners' terms.
