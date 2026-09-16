# 2026-09-16 — Enemy intent hook dispatch

Pre-change snapshot: `maiden-pre-intent-hook-fix-20260916` / `ee43bbe`.

## Evidence

- Latest `godot.log` registers the 102-monster catalogue and threshold power types, but registration alone does not invoke character lifecycle overrides.
- Decompilation of the installed `sts2.dll` confirms `CombatState.IterateHookListeners` does not automatically include `Player.Character`. RitsuLib 0.4.64's capability hook expansion does not add character models either.
- The installed game supplies `ModHelper.SubscribeForCombatStateHooks`. `RunState.IterateHookListeners` includes the combat stream, so registering the same character in both streams would duplicate execution.

## Implementation

- Subscribe distinct active MaidenSuccubus character models through the native combat subscription API. This connects their existing awaited combat-start, creature-added and player-turn-start callbacks.
- On turn start, initialize missing temptation/threshold carriers before selection. Existing runtime carriers, use counters and cooldowns remain intact. This recovers combats saved before hook registration was fixed.
- Keep the existing role guard and tighten player-turn matching to the actual character instance.
- DarkElement already implements the current DesignDoc: corruption >3 repeats damage after magic release; <=3 gains block. Correct the damage branch's localization to use Damage rather than Block, which previously previewed the wrong modifiers.

## Verification

- `dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=true`: passed, 0 warnings, 0 errors after allowing dependency restore outside the network sandbox.
- Content, structural, visual-asset, localization and test-catalog validation passed (222 cards).
- Added `lifecycle_threshold_dispatch` to Ctrl+F10: checks exactly one character listener through combat/run streams; invokes real `Hook.BeforeCombatStart` and `Hook.AfterPlayerTurnStart`; verifies summoned Byrdonis thresholds 25 and 40, unchanged intent at 24, no mid-turn replacement at 25, turn-start replacement and exactly one consumed use.
- This is compiled runtime test coverage, not a claim of an executed game test. Run Ctrl+F10 in a disposable combat after restarting the deployed game.
- Latest log also records a separate full-card run with 79 passed / 141 failed / 2 pending; this patch does not declare those failures resolved.

User edits to DesignDoc, other changelog entries, research assets and unrelated staged files are preserved.
