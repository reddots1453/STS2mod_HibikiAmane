using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib;
using STS2RitsuLib.Combat.SecondaryResources;
using MaidenSuccubus.Characters;

namespace MaidenSuccubus.Core.Desire;

public static class DesireResource
{
    public const string LocalId = "desire";

    public static SecondaryResourceDefinition Definition { get; private set; }
        = null!;

    public static string Id => Definition.Id;

    public static void Register()
    {
        var registry =
            RitsuLibFramework.GetSecondaryResourceRegistry(
                MaidenSuccubusMod.ModId);
        Definition = registry.Register(
            LocalId,
            new SecondaryResourceDefinition(
                defaultAmount: 0,
                baseMaxAmount: 10,
                minAmount: 0,
                hardMaxAmount: int.MaxValue,
                turnStartPolicy: SecondaryResourceTurnStartPolicy.None,
                persistencePolicy: SecondaryResourcePersistencePolicy.Run,
                smallIconPath:
                    "res://images/packed/sprite_fonts/star_icon.png",
                largeIconPath:
                    "res://images/packed/sprite_fonts/star_icon.png",
                locTable: "static_hover_tips",
                titleKey:
                    "MAIDENSUCCUBUS_SECONDARY_RESOURCE_DESIRE.title",
                descriptionKey:
                    "MAIDENSUCCUBUS_SECONDARY_RESOURCE_DESIRE.description"));

        registry.RegisterCardUi(
            "desire_card_cost",
            parent =>
            {
                var ui = NSecondaryResourceCardCostUi.Create(
                    Definition,
                    SecondaryResourceCardCostUiStyle.Default with
                    {
                        ReserveVanillaStarCostSlot = true,
                        // The DesignDoc defines the two independent X-cost
                        // variables as energy X and Desire Y.
                        FormatCost = line => line.CostsX
                            ? "Y"
                            : line.Cost.ToString(),
                    });
                TextureRect starIcon =
                    parent.GetNode<TextureRect>("%StarIcon");
                ui.Position = starIcon.Position;
                return ui;
            },
            context => context.Node.Refresh(context));

        registry.AlwaysShowInCombatUiForCharacter<MaidenSuccubusCharacter>(
            Definition.LocalId);
        SecondaryResourceHook.RegisterGlobalListener(
            new DesireResourceRules());

        MaidenSuccubusMod.Logger.Info(
            $"Secondary resource registered: {Definition.Id} " +
            $"(persistence=Run, max=10)");
    }
}
