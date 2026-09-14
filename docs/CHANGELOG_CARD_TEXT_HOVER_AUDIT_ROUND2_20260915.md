# Card Text and Hover Audit — Round 2 (2026-09-15)

## Version boundary

- Branch: `codex/maiden-controlled-merge-v2`
- Parent commit: `f4b37420b34c7eb904e9f94fbefac091d521c23c`
- Pre-change tag: `maiden-pre-card-text-hover-audit-round2-20260915`
- Requirements authority: `DesignDoc.md`
- Authority SHA-256: `91DF66648B14C357B54EE10D0B9E4085043454AA1A065018CD98DEFC059E6303`

## Scope and result

- Re-audited all 215 registered cards against the current DesignDoc and the shared card-hover pipeline.
- Kept the two explicit `效果待后续设计` cards excluded from executable effect expectations; their registration and text structure remain audited.
- The generated report contains one record for each registered card. 150 cards contain description terms that require description-driven hover entries; the other 65 do not contain such terms and still retain engine-generated dynamic-variable, keyword, enchantment, and card-specific hover entries where applicable.
- No card rules or numeric behavior were changed in this round.

## Corrections

- Changed description-driven hover detection from raw substring matching to exact rich-text token matching.
  - `[gold]消耗牌堆[/gold]` no longer incorrectly produces the `消耗` keyword hover.
  - `圣言牌` and `断罪审判` retain the intended containing-term hover behavior.
- Replaced card-context references to runtime-only top-bar tips for `堕落值` and `诱惑度` with static reference tips that contain no unresolved placeholders.
- Replaced the unrelated vanilla card-transform hover used by `变身` with the character transformation rule from DesignDoc.
- Added card-context reference hovers for `侵犯` and `色情攻击`.
- Added the missing `稳定` enchantment hover to `战术分析仪`.
- Normalized the established enchantment wording and color format for `黑暗风暴`, `锻成·锋利`, `战术分析仪`, and `锻成·打击`: `[gold]附魔[/gold]：[purple]具体附魔[/purple]`.
- Colored `击晕` as an interactive rules term on `剑之裁决`, `妨碍射击`, `咬`, `欲望鞭挞`, and `情人匕首`, and linked it to the vanilla stun hover.
- Colored the generated-card names `冰雾` and `困了` in their power-state descriptions.
- Extended the localization audit to verify:
  - all custom static hover keys and placeholder safety;
  - exact rich-text keyword tokens;
  - named enchantment text and hover contracts;
  - required colors in card and power descriptions;
  - inline generated cards defined outside the main card catalogue, including `谦逊` under the fourth-route relic section.

## Verification

- `python scripts/AuditCardLocalization.py`: `audited=215 failures=0`.
- `dotnet build -c Debug --no-restore -p:DeployMod=false -p:ValidateMod=true`: passed with 0 warnings and 0 errors.
- `dotnet build -c Debug --no-restore -p:DeployMod=true -p:ValidateMod=true`: passed with 0 warnings and 0 errors.
- Content contract: 215 exact card registrations across 44 neutral, 62 corrupt, 55 holy, 17 invasion-curse, and 37 generated cards.
- Card-effect contract: 213 executable cards and 2 DesignDoc-pending cards; the 59-card iteration-two numeric suite remains structurally valid.
- The deployed DLL, PDB, manifest, `cards.json`, `powers.json`, and `static_hover_tips.json` SHA-256 values exactly match the source artifacts.
- Runtime visual verification remains a manual follow-up after deployment.
