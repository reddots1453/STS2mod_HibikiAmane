using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Enchantments;

/// <summary>
/// Base class for enchantments which are only ever attached to combat-card
/// clones. Concrete subclasses still need <c>[RegisterEnchantment]</c>.
///
/// Do not read or mutate <c>Card.DeckVersion</c> from subclasses: that property
/// points to the permanent run-deck card.
/// </summary>
public abstract class CombatOnlyEnchantmentTemplate : ModEnchantmentTemplate
{
    // A combat-only enchantment should never be previewed as a persistent
    // run-level modification in rewards, shops, or deck screens.
    public override bool PreviewOutsideOfCombat => false;
}
