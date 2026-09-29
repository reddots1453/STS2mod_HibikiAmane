"""Mutation tests: the migrated gate must reject real sidebar regressions."""
from pathlib import Path
import unittest
from ValidateSidebarLayout20260927 import read_sources, validate


class SidebarLayoutTests(unittest.TestCase):
    def setUp(self):
        self.sources = read_sources(Path(__file__).resolve().parents[1])

    def reject(self, name, old, new):
        self.assertIn(old, self.sources[name], 'Mutation no longer targets production source')
        self.sources[name] = self.sources[name].replace(old, new)
        with self.assertRaises(ValueError):
            validate(self.sources)

    def test_current_source(self):
        validate(self.sources)

    def test_safe_geometry_change_is_not_pixel_locked(self):
        self.sources['MaidenSidebarRail'] = self.sources['MaidenSidebarRail'].replace(
            'new(78f, 304f)', 'new(80f, 320f)')
        validate(self.sources)

    def test_wrong_icon(self):
        self.reject('TemptationMeter', 'temptation_lipstick_64.png', 'old.png')

    def test_unattached_icon(self):
        self.reject('TemptationMeter', 'AddChild(icon);', '// AddChild(icon);')

    def test_non_centered_value(self):
        for axis in ('Horizontal', 'Vertical'):
            with self.subTest(axis=axis):
                self.setUp()
                self.reject('TemptationMeter', axis + 'Alignment.Center', axis + 'Alignment.Left')

    def test_value_wrong_parent(self):
        self.reject('TemptationMeter', 'badge.AddChild(_value);', 'AddChild(_value);')

    def test_badge_clipped(self):
        self.reject('TemptationMeter', 'new Vector2(18f, 38f)', 'new Vector2(56f, 34f)')

    def test_badge_not_over_lipstick(self):
        self.reject('TemptationMeter', 'new Vector2(18f, 38f)', 'new Vector2(35f, 29f)')

    def test_trial_modal_does_not_show_resources(self):
        self.reject('MaidenSidebarRail', 'NModalContainer.Instance?.OpenModal == null', 'true')
        self.setUp()
        self.reject('CorruptionMeter', 'NModalContainer.Instance?.OpenModal != null', 'false')

    def test_native_topbar_visibility_is_authoritative(self):
        self.reject('MaidenSidebarRail', '_topBar.Map?.IsVisibleInTree() == true', 'true')
        self.setUp()
        self.reject('CorruptionMeter', '_topBar.Deck?.IsVisibleInTree() != true', 'false')

    def test_desire_frame_is_cropped(self):
        self.reject('DesireMeter', 'MeterArtworkHeight = 344f', 'MeterArtworkHeight = 416f')

    def test_vertical_overlap(self):
        self.reject('MaidenSidebarRail', 'new(7f, 88f)', 'new(7f, 70f)')

    def test_horizontal_misalignment(self):
        self.reject('MaidenSidebarRail', 'new(7f, 88f)', 'new(8f, 88f)')

    def test_rail_clips_meter(self):
        self.reject('MaidenSidebarRail', 'new(78f, 304f)', 'new(78f, 250f)')

    def test_meter_not_reparented(self):
        for name in ('TemptationMeter', 'DesireMeter'):
            with self.subTest(name=name):
                self.setUp()
                self.reject(name, 'Reparent(rail);', '/* Reparent(rail); */')

    def test_settings_not_hidden(self):
        self.reject('MaidenSidebarRail', '!_settingsOpen && _topBar != null', '_topBar != null')

    def test_settings_close_callback_inverted(self):
        self.reject('MaidenSidebarRail', 'SetSettingsOpen(false)', 'SetSettingsOpen(true)')

    def test_signal_cleanup_missing(self):
        self.reject('MaidenSidebarRail', 'SettingsClosed -= OnSettingsClosed;', 'SettingsClosed += OnSettingsClosed;')

    def test_initial_settings_state_missing(self):
        self.reject('MaidenSidebarRail', 'SetSettingsOpen(settings.IsVisibleInTree());', '')

    def test_hover_cleanup_missing(self):
        self.reject('MaidenSidebarRail', 'NHoverTipSet.Remove(child);', '')

    def test_missing_file_fails_closed(self):
        with self.assertRaises(OSError):
            read_sources(Path(__file__).resolve())


if __name__ == '__main__':
    unittest.main()
