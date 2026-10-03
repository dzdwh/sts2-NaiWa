# Contributing translations

Thank you for helping improve NaiWa's English translation!

1. Fork this repository and create a translation branch.
2. Edit files in `mod/naiwa/naiwa/localization/eng/`. Use the matching Simplified Chinese files in `zhs/` and the C# card/power implementation to understand the mechanics.
3. Preserve localization keys. Translate values only; add missing English keys when necessary, but do not remove existing keys.
4. Preserve all dynamic placeholders and formatter syntax exactly, such as `{Damage:diff()}`, `{Energy:energyIcons()}`, `{StrengthPower:diff()}` and `{IfUpgraded:show:...|...}`. Translate display text inside conditional branches without changing the variable or formatter.
5. Preserve markup and control syntax: `[gold]...[/gold]`, other formatting tags, escape sequences and line breaks. Do not hard-code an upgrade value in place of a placeholder.
6. Use **NaiWa** for 奶蛙, not Milk Frog. Keep terminology consistent across cards, powers, relics, keywords and events. Explain substantial terminology changes in the pull request.
7. Run `python scripts/validate_localization.py` to check JSON syntax and placeholder/key preservation against the repository's baseline. This is not a substitute for testing in-game layout or SmartFormat behavior.
8. Open a pull request with a short description of the files changed. Screenshots from Mod Translator or in-game previews are welcome.

Please work on the main localization directory first. Maintainers will synchronize accepted translations to the 0.107.1 compatibility copy and handle version-specific differences. You do not need to edit both versions for a normal translation contribution.

If you use Mod Translator, export the translated JSON files and copy the relevant values into this repository. Do not submit a whole game dump, installed DLL/PCK, machine configuration, API tokens or player telemetry.

You do not need permission to push directly to the upstream repository: forks and pull requests are the normal contribution workflow. Please mention how you would like to be credited in your first pull request.
