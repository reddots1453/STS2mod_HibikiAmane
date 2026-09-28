"""Inventory reviewed text-test declarations, never runtime pass/fail evidence.

Read-only by default. --output may create a NEW report only under this mod's obj/.
This is a curated, bounded inventory, not C# control-flow analysis. New provider
forms require review; unidentified does not prove that no test exists anywhere.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import re
import sys

import AuditCardLocalization as audit

PREFIX = 'src/Debugging/CardEffects/'
RUNNER = PREFIX + 'CardEffectTestRunner.cs'
CATALOG = PREFIX + 'CardEffectTestCatalog.cs'
BOTH = 'full_run_and_combat_declared'
COMBAT = 'full_combat_only_declared'
PARTIAL = 'partial_text_declared'
MISSING = 'not_identified'

# These providers were read/reviewed, unlike metadata-only Entries collections.
# (class, list form, independent assertion anchor, reviewed model count)
GLOBAL = (
    ('DesignSyncStarterTextContract', 'Entries', 'starter full run text', 5),
    ('DesignSyncRemainingTextContract', 'Entries', 'remaining exact run text', 7),
    ('DesignSyncNeutralTextContract', 'Entries', 'DS27 neutral full rendered text', 30),
    ('DesignSyncHolyTextContract', 'Entries', 'DS27 holy full rendered text', 48),
    ('DesignSyncCombatTextContract', 'Entries', 'combat text contract true run instance', 8),
    ('DesignSyncTextBatchContract', 'Types', 'DS27 full outside text incl punctuation and native keywords', 12),
    ('DesignSyncEnchantmentInputContract', 'Types', 'enchantment input full deck preview text', 4),
    ('DesignSyncChainCopyContract', 'Types', 'chain/copy full permanent preview', 3),
    ('DesignSyncShatterRandomContract', 'Types', 'shatter/random full text', 4),
)
# (model, provider, method, scope, exact assertion anchor)
DIRECT = (
    ('AcceleratedMotion', 'DesignSyncAcceleratedMotionContract', 'Run', BOTH, 'accelerated full card text'),
    ('CalmMind', 'DesignSyncCalmMindContract', 'Run', BOTH, 'calm mind exact run text'),
    ('GoddessOfIce', 'DesignSyncIceGoddessContract', 'Run', BOTH, 'goddess exact outside description'),
    ('InsatiableGreed', 'DesignSyncLibraryAuraContract', 'Run', BOTH, 'library aura full rendered text'),
    ('TemperanceSignet', 'DesignSyncTemperanceSignetContract', 'Run', BOTH, 'signet full rendered text'),
    ('TemperanceCirclet', 'DesignSyncLibraryContract', 'Run', BOTH, 'library full rendered text'),
    ('FlameSword', 'DesignSyncFlameSwordContract', 'Run', COMBAT, 'exact runtime description in hand'),
    ('WindGodCloak', 'DesignSyncWindGodCloakContract', 'Run', COMBAT, 'Cloak exact runtime text'),
    ('BeyondReasonForge', 'DesignSyncForgeContract', 'Run', COMBAT, 'forge full description'),
    ('SuperRegeneration', 'DesignSyncExhaustContract', 'Regeneration', COMBAT, 'regen exact sentence and keyword order'),
    ('DarkStorm', 'DesignSyncDarkStormContract', 'Run', PARTIAL, 'runtime formatted upgrade enchantment text'),
    ('CurseInfection', 'DesignSyncCurseInfectionContract', 'Run', PARTIAL, 'source formal text'),
)


def read(path):
    return (audit.ROOT / path).read_text(encoding='utf-8-sig')


def listed_models(source, form):
    code = audit.mask_csharp_literals(source)
    match = re.search(r'\b' + form + r'\s*=\s*\[(.*?)\];', code, re.S)
    if not match:
        return []
    body = match.group(1)
    pattern = r'new\(typeof\((\w+)\),' if form == 'Entries' else r'typeof\((\w+)\)'
    return re.findall(pattern, body)


def inventory(read_source=read, registered=None):
    cards, _ = audit.registered_cards() if registered is None else registered
    evidence = {model: [] for model in cards}
    errors = []
    sources = {}

    def source(path):
        try:
            text = read_source(path)
            sources[path] = hashlib.sha256(text.encode('utf-8')).hexdigest()
            return text
        except (OSError, UnicodeError) as exc:
            errors.append(f'{path}: {type(exc).__name__}: {exc}')
            return ''

    runner = audit.mask_csharp_literals(source(RUNNER))
    catalog_text = source(CATALOG)
    catalog = audit.mask_csharp_literals(catalog_text)

    def add(models, provider, scope, anchor, connection, form=None, count=None):
        path = PREFIX + provider + '.cs'
        text = source(path)
        if form:
            models = listed_models(text, form)
        valid = True
        if count is not None and len(models) != count:
            errors.append(f'{provider}: expected {count} declarations, got {len(models)}')
            valid = False
        if len(set(models)) != len(models):
            errors.append(f'{provider}: duplicate model declarations')
            valid = False
        if not connection or anchor not in text:
            errors.append(f'{provider}: missing reviewed entrypoint or assertion anchor')
            valid = False
        if scope == BOTH and ('RunState.CreateCard' not in text or 'PileType.Deck' not in text):
            errors.append(f'{provider}: missing real run-instance text path')
            valid = False
        for model in models:
            if model not in evidence:
                errors.append(f'{provider}: unregistered model {model}')
            elif valid:
                evidence[model].append({'provider': path, 'scope': scope, 'anchor': anchor,
                                        'execution': 'not_run'})

    for provider, form, anchor, count in GLOBAL:
        add([], provider, BOTH, anchor, f'{provider}.Validate(context, card, scenario.Upgraded);' in runner,
            form, count)
    for model, provider, method, scope, anchor in DIRECT:
        connection = re.search(r'CustomVariants<' + model + r'>\(\s*' + provider + r'\.' + method + r'\s*,', catalog)
        add([model], provider, scope, anchor, bool(connection))

    scripture = 'DesignSyncScriptureContract'
    scripture_source = source(PREFIX + scripture + '.cs')
    scriptures = listed_models(scripture_source, 'Types')
    scripture_connected = ('CustomVariants<T>(DesignSyncScriptureContract.Run, 20)' in catalog
                           and all(f'Scripture<{name}>();' in catalog for name in scriptures))
    add(scriptures, scripture, BOTH, 'scripture exact full text', scripture_connected, count=6)

    # Generic catalog helper: one base-only card, not an upgraded-text contract.
    if ('HandCostRestriction<GagCurse>(CardType.Skill, 1, expectedOwnCost: 1);' in catalog
            and '"restriction full text"' in catalog_text
            and 'DesignSyncCombatTextContract.AssertText(ctx, card, PileType.Hand,' in catalog):
        if 'GagCurse' in evidence:
            evidence['GagCurse'].append({'provider': CATALOG, 'scope': COMBAT,
                                         'anchor': 'restriction full text', 'execution': 'not_run'})
    else:
        errors.append('GagCurse: reviewed base-only full text helper disconnected')

    manifest = json.loads(source('docs/content_contract_20260824.json') or '{}')
    retired = set(manifest.get('retiredCompatibility', {}).get('cards', []))
    loc = json.loads(source('MaidenSuccubus/localization/zhs/cards.json') or '{}')
    rank = {BOTH: 3, COMBAT: 2, PARTIAL: 1, MISSING: 0}
    rows = []
    for model in sorted(cards):
        providers = evidence[model]
        scope = max((item['scope'] for item in providers), key=rank.get, default=MISSING)
        category = ('technical' if model in audit.TECHNICAL_CARD_TYPES else
                    'retired_compatibility' if model in retired else 'current_card_or_derivative')
        rows.append({'model': model, 'title': loc.get('MAIDEN_SUCCUBUS_CARD_' + audit.screaming_snake(model) + '.title'),
                     'source': cards[model][1].relative_to(audit.ROOT).as_posix(),
                     'category': category, 'textEvidence': scope, 'providers': providers, 'runtime': 'not_run'})
    current = [row for row in rows if row['category'] == 'current_card_or_derivative']
    return {'schemaVersion': 1, 'sources': sources, 'integrityErrors': errors, 'cards': rows,
            'summary': {'registered': len(rows), 'current': len(current),
                        'categories': dict(Counter(row['category'] for row in rows)),
                        'currentTextEvidence': dict(Counter(row['textEvidence'] for row in current)),
                        'runtime': 'not_run', 'goalCompleted': False},
            'limitations': ['Reviewed declarations and connection guards are not semantic execution proofs.',
                            'Both means a real run instance plus combat instance, not visual layout verification.',
                            'Not identified is a review queue, not proof that no other test exists.',
                            'No runtime report is imported and no gameplay/monster/event coverage is inferred.']}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, help='New report under obj/; never overwrite existing files')
    args = parser.parse_args(argv)
    destination = None
    if args.output:
        destination = args.output.resolve()
        if not destination.is_relative_to((audit.ROOT / 'obj').resolve()):
            parser.error('--output must stay under this mod obj/')
        if destination.exists():
            parser.error('--output already exists; choose a new evidence path')
    report = inventory()
    if destination:
        destination.parent.mkdir(parents=True, exist_ok=True)
        with destination.open('x', encoding='utf-8') as stream:
            json.dump(report, stream, ensure_ascii=False, indent=2)
    print(json.dumps({'summary': report['summary'], 'integrityErrors': report['integrityErrors'],
                      'output': str(destination) if destination else None}, ensure_ascii=True, indent=2))
    return 1 if report['integrityErrors'] else 0  # Inventory success, never goal completion.


if __name__ == '__main__':
    sys.exit(main())
