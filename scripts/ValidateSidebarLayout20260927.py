"""Source-structure guard for the approved vertical sidebar, not a render test.

Intentionally restricted to these three C# controls and their current initializer
syntax. Unknown syntax fails closed and needs review; this is not a C# compiler.
"""
from pathlib import Path
import argparse
import re


def require(condition, message):
    if not condition:
        raise ValueError(message)


def code_only(source):
    # Preserve strings (including // in resource paths) while removing comments.
    return re.sub(r'"(?:\\.|[^"\\])*"|//[^\n]*|/\*[\s\S]*?\*/',
                  lambda m: m[0] if m[0].startswith('"') else ' ', source)


def initializer(source, variable, kind):
    match = re.search(r'\b' + re.escape(variable) + r'\s*=\s*new\s+' +
                      re.escape(kind) + r'\s*\{([^{}]*)\}\s*;', source)
    require(match, f'Missing {variable} {kind} initializer')
    return match[1]


def vector(source, field):
    number = r'(-?\d+(?:\.\d+)?)f?'
    match = re.search(r'\b' + re.escape(field) + r'\s*=\s*new(?:\s+Vector2)?\s*\(\s*' +
                      number + r'\s*,\s*' + number + r'\s*\)', source)
    require(match, f'Missing constant Vector2 {field}')
    return float(match[1]), float(match[2])


def has(source, expression, message):
    require(re.search(expression, source), message)


def validate(sources):
    rail, temptation, desire, corruption = (code_only(sources[name]) for name in
        ('MaidenSidebarRail', 'TemptationMeter', 'DesireMeter', 'CorruptionMeter'))
    icon = initializer(temptation, 'icon', 'TextureRect')
    has(icon, r'Texture\s*=\s*RuntimeTextureAssets.Load\(\s*"ui/temptation/temptation_lipstick_64\.png"\s*\)',
        'Formal sidebar icon is not loaded')
    has(temptation, r'\bAddChild\(icon\)', 'Icon is not attached')
    require('Name = "Title"' not in temptation, 'Temptation title must not occupy sidebar space')
    value = initializer(temptation, '_value', 'Label')
    for axis in ('Horizontal', 'Vertical'):
        has(value, axis + r'Alignment\s*=\s*' + axis + r'Alignment.Center',
            f'Value must be {axis.lower()}ly centered')
    has(value, r'Size\s*=\s*badge.Size', 'Value must fill its independent badge')
    has(temptation, r'badge.AddChild\(_value\)', 'Value must be a child of its badge')
    has(temptation, r'\bAddChild\(badge\)', 'Badge is not attached')
    badge = initializer(temptation, 'badge', 'Panel')
    bx, by = vector(badge, 'Position')
    bw, bh = vector(badge, 'Size')
    tw, th = vector(temptation, 'MeterSize')
    require(bw > 0 and bh > 0 and bx >= 0 and by >= 0 and bx + bw <= tw and by + bh <= th,
            'Value badge must fit inside its meter')
    ix, iy = vector(icon, 'Position') if 'Position = Vector2.Zero' not in icon else (0, 0)
    iw, ih = vector(icon, 'Size')
    require(abs((bx + bw / 2) - (ix + iw / 2)) <= 3
            and by >= iy + ih / 2 and by + bh <= iy + ih + 4,
            'Temptation value must sit at the lower center of the lipstick')
    rw, rh = vector(rail, 'RailSize')
    tx, ty = vector(rail, 'TemptationPosition')
    dx, dy = vector(rail, 'DesirePosition')
    dw, dh = vector(desire, 'MeterSize')
    for x, y, w, h in ((tx, ty, tw, th), (dx, dy, dw, dh)):
        require(w > 0 and h > 0 and x >= 0 and y >= 0 and x + w <= rw and y + h <= rh,
                'Sidebar meter must fit inside rail')
    require(tx + tw / 2 == dx + dw / 2 and ty + th <= dy,
            'Sidebar meters must be vertically aligned and non-overlapping')
    for name, source in (('Temptation', temptation), ('Desire', desire)):
        has(source, r'MaidenSidebarRail.GetOrCreate\(_topBar\)', name + ' must use shared rail')
        has(source, r'\bReparent\(rail\)', name + ' must attach to shared rail')
        has(source, r'Position\s*=\s*MaidenSidebarRail\.' + name + 'Position',
            name + ' must use rail placement')
    for event, callback in (('SettingsOpened', 'OnSettingsOpened'), ('SettingsClosed', 'OnSettingsClosed')):
        for op in ('+=', '-='):
            has(rail, event + r'\s*' + re.escape(op) + r'\s*' + callback,
                'Settings signal subscribe/unsubscribe missing')
    for callback, value in (('OnSettingsOpened', 'true'), ('OnSettingsClosed', 'false')):
        has(rail, callback + r'\(\)\s*=>\s*SetSettingsOpen\(' + value + r'\)',
            'Settings callback must update rail visibility')
    has(rail, r'bool shouldShow\s*=\s*!_settingsOpen\s*&&', 'Open settings must hide rail')
    has(rail, r'Visible\s*=\s*shouldShow\s*;', 'Rail must apply combined lifecycle visibility')
    has(rail, r'CurrentMapCoord\.HasValue', 'Rail must wait for map entry')
    for name, code in (('Rail', rail), ('Balance', corruption)):
        for term in ('Deck?.IsVisibleInTree()', 'Map?.IsVisibleInTree()',
                     'NModalContainer.Instance?.OpenModal', 'CanonicalEvent: Neow'):
            require(term in code, f'{name} must share native top-bar/modal lifecycle: {term}')
        require('ZIndex = 100' not in code, f'{name} must not float over full-screen UI')
    require('new AtlasTexture' in desire and 'MeterArtworkHeight = 344f' in desire,
            'Desire artwork must crop the baked-in numeric frame')
    has(rail, r'SetSettingsOpen\(settings.IsVisibleInTree\(\)\)', 'Initial settings visibility missing')
    has(rail, r'NHoverTipSet.Remove\(child\)', 'Settings must clear child hover tips')


def read_sources(project):
    return {name: (project / 'src' / 'UI' / (name + '.cs')).read_text(encoding='utf-8-sig')
            for name in ('MaidenSidebarRail', 'TemptationMeter', 'DesireMeter', 'CorruptionMeter')}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project-dir', type=Path, required=True)
    args = parser.parse_args()
    try:
        validate(read_sources(args.project_dir))
    except (OSError, ValueError) as error:
        parser.exit(1, f'Sidebar source contract failed: {error}\n')
    print('Sidebar source contracts passed; in-game rendering remains unverified.')


if __name__ == '__main__':
    main()
