using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MaidenSuccubus.Cards.Scriptures;
using MaidenSuccubus.Core.Scriptures;
using MaidenSuccubus.Powers.Scriptures;

namespace MaidenSuccubus.Commands;

public static class ScriptureCmd
{
    public static event Action<ScriptureTriggered>? Triggered;

    public static async Task<CardModel> TransformCombatCard<TScripture>(
        CardModel combatCard,
        CardPreviewStyle previewStyle = CardPreviewStyle.HorizontalLayout)
        where TScripture : ScriptureCardTemplate
    {
        ArgumentNullException.ThrowIfNull(combatCard);
        if (!CombatEnchantmentCmd.IsCombatClone(combatCard))
        {
            throw new InvalidOperationException(
                $"Card {combatCard.Id} is not an active combat clone.");
        }
        if (!combatCard.IsTransformable)
        {
            throw new InvalidOperationException(
                $"Card {combatCard.Id} cannot be transformed.");
        }

        CardPileAddResult? result = await CardCmd.TransformTo<TScripture>(
            combatCard,
            previewStyle);
        return result?.cardAdded
            ?? throw new InvalidOperationException(
                $"Transforming {combatCard.Id} into {typeof(TScripture).Name} failed.");
    }

    public static Task<TPower?> Apply<TPower>(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal duration,
        Creature? applier,
        CardModel? cardSource)
        where TPower : ScripturePowerTemplate =>
        PowerCmd.Apply<TPower>(
            choiceContext,
            target,
            duration,
            applier,
            cardSource);

    internal static async Task Publish(ScriptureTriggered scripture)
    {
        var combatState = scripture.Owner.CombatState;
        if (combatState == null)
        {
            return;
        }

        foreach (IScriptureTriggeredListener listener in combatState
            .IterateHookListeners()
            .OfType<IScriptureTriggeredListener>())
        {
            await listener.AfterScriptureTriggered(scripture);
        }

        Triggered?.Invoke(scripture);
    }
}
