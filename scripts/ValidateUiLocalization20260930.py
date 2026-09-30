"""Check UI keys and optionally verify the complete installed localization tables."""
import argparse
import json
import re
from pathlib import Path

def validate(project, installed=None):
    root = project / 'MaidenSuccubus/localization/zhs'
    tables = {name: json.loads((root / f'{name}.json').read_text(encoding='utf-8'))
              for name in ('intents', 'static_hover_tips', 'card_library', 'powers')}
    prefixes = set()
    for file in (project / 'src/Core/Intents').glob('*.cs'):
        prefixes.update(re.findall(r'IntentPrefix\s*=>\s*"(MAIDENSUCCUBUS_[A-Z_]+)"', file.read_text(encoding='utf-8')))
    required = [("intents", prefix + suffix) for prefix in prefixes for suffix in ('.title', '.description')]
    required += [('intents', 'MAIDENSUCCUBUS_BLOCK.format')]
    required += [('static_hover_tips', 'MAIDENSUCCUBUS_SHOP_CURSE_REMOVAL' + suffix)
                 for suffix in ('.title', '.description')]
    required += [('powers', 'MAIDEN_SUCCUBUS_POWER_CORRUPT_ROBE_POWER' + suffix)
                 for suffix in ('.description', '.smartDescription')]
    for table, key in required:
        assert tables[table].get(key), f'Missing UI localization: {table}.{key}'
    for key, parameters in {
        'MAIDENSUCCUBUS_INVASION_CURSE.description': ['CurseName'],
        'MAIDENSUCCUBUS_CLOTHING_HAZARD.description': ['CardCount', 'CardName', 'Placement'],
        'MAIDENSUCCUBUS_BLOCK.description': ['Effect'],
        'MAIDENSUCCUBUS_BLOCK.format': ['BlockAmount'],
    }.items():
        assert set(re.findall(r'\{(\w+)\}', tables['intents'][key])) == set(parameters), key
    assert set(re.findall(r'\{(\w+)\}', tables['static_hover_tips']['MAIDENSUCCUBUS_SHOP_CURSE_REMOVAL.description'])) == {'Count', 'Refund'}
    for suffix in ('.description', '.smartDescription'):
        plain = re.sub(r'\[[^\]]+\]', '', tables['powers']['MAIDEN_SUCCUBUS_POWER_CORRUPT_ROBE_POWER' + suffix])
        assert plain == '每回合开始时获得1层“魔力增幅”。失去魔装耐久时获得1点欲望。', plain
    if installed:
        for table, source in tables.items():
            target = installed / f'localization/zhs/{table}.json'
            assert target.is_file(), f'Missing installed table: {target}'
            runtime = json.loads(target.read_text(encoding='utf-8'))
            mismatch = [key for key, value in source.items() if runtime.get(key) != value]
            assert not mismatch, f'Stale installed {table}: {mismatch}'
    print(f'PASS: {len(required)} UI keys, format parameters, ' + ('4 installed tables match' if installed else 'source tables'))

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--project-dir', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--installed-root', type=Path)
    args = parser.parse_args()
    validate(args.project_dir, args.installed_root)
