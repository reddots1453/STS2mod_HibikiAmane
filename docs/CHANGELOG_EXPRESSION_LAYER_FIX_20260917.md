# 2026-09-17 — Expression overlay ordering

- Pre-change snapshot: `a428150` (also tagged `maiden-intent-hook-fix-20260916`).
- Symptom: the face remained bright over the darkened character on the game-over screen.
- Cause: ExpressionSprite used body ZIndex + 1. Godot sorts that sprite above later sibling screen dimmers at the body's ZIndex, even though both sprites share a parent.
- Fix: keep ExpressionSprite at the body's ZIndex. Adding it after CharacterSprite already places it over the body through sibling draw order; screen overlays can now cover both consistently. Preserve expression selection, canvas alignment and shared animation/modulation.
- Validation: Debug build with DeployMod=false and ValidateMod=true; runtime visual acceptance requires checking game-over, map and deck overlays after restarting.
- No new gameplay behavior, assets or Harmony patches. Original art and user changes remain intact.
