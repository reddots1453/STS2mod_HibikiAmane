# UI layout correction — 2026-09-16

## Scope

Correct the five visual regressions reported after the reviewed UI asset integration. No gameplay rules, card semantics, or source artwork were changed.

## Changes

- Route-card wings now configure `TextureRect.ExpandMode` before assigning the 512px source texture and explicitly restore the reviewed 92x92 / 84x90 display bounds. This prevents raw-size wings from covering cards and adjacent UI.
- The corruption balance now seeds its first texture with the actual neutral value. Previously the invalidation sentinel was clamped to `+5` during construction, then the later zero refresh short-circuited and left the tilted `+5` image visible.
- The persistent desire rail and combat temptation icon moved 92px downward, below the top-left relic rows.
- Character expression overlays now compensate for the 922x922 expression canvas versus the 922x1250 body canvas. Their shared top-left artwork coordinates therefore align despite `Sprite2D` using centered origins.
- The RitsuLib combat desire counter moved right of the vanilla energy counter instead of overlapping it.

## Evidence and verification

- Reviewed `Screenshot (199).png`, `Screenshot (200).png`, and `Screenshot (201).png` at original resolution.
- Confirmed all 12 expression textures are 922x922 and the body texture is 922x1250.
- Confirmed `corruption_balance_zero.png` itself is level; the wrong state came from initialization order.
- Checked the latest game log (`godot2026-09-16T21.40.57.log`); it contained no runtime exception from these UI components.

## Versioning

- Pre-change snapshot: `maiden-pre-ui-layout-fix-20260916`
- Intended commit: `fix(maiden): correct custom UI layout`
- Intended completion tag: `maiden-ui-layout-fix-20260916`
