namespace MaidenSuccubus.Cards;

/// <summary>
/// Opt-in marker for cards whose combat effects are allowed to mutate their
/// persistent run-deck version. Ordinary cards must never implement this.
/// </summary>
public interface IPermanentGrowthCard
{
}
