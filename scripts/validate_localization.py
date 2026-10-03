"""Offline JSON/key/dynamic-variable checks for translation contributions."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
LOCALIZATION = ROOT / 'mod/naiwa/naiwa/localization/eng'
VARIABLE = re.compile(r'\{([A-Za-z_][A-Za-z0-9_]*)')

def variable_names(value):
    return sorted(VARIABLE.findall(value))

def main():
    baseline = json.loads((ROOT / 'scripts/localization_baseline.json').read_text(encoding='utf-8'))
    errors = []
    checked = 0
    for name, original in baseline.items():
        path = LOCALIZATION / name
        try:
            def unique_keys(pairs):
                result = {}
                for key, value in pairs:
                    if key in result:
                        raise ValueError('duplicate localization key: '+key)
                    result[key] = value
                return result
            current = json.loads(path.read_text(encoding='utf-8-sig'), object_pairs_hook=unique_keys)
        except (OSError, ValueError) as exc:
            errors.append(f'{name}: {exc}')
            continue
        for key, placeholders in original.items():
            if key not in current:
                errors.append(f'{name}: missing key {key}')
            elif not isinstance(current[key], str):
                errors.append(f'{name}: non-string value for {key}')
            elif variable_names(current[key]) != placeholders:
                errors.append(f'{name}: changed dynamic variables for {key}')
            checked += 1
        for key, value in current.items():
            if not isinstance(value, str):
                errors.append(f'{name}: non-string value for {key}')
    if errors:
        print('\n'.join(errors))
        raise SystemExit(1)
    print(f'OK: {len(baseline)} English files; {checked} baseline keys checked.')
    print('Manual review is still required for formatter syntax, markup and in-game layout.')

if __name__ == '__main__':
    main()
