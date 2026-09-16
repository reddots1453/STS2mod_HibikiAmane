using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib;
using STS2RitsuLib.Combat.SecondaryResources;
using MaidenSuccubus.Characters;
using MaidenSuccubus.UI;

namespace MaidenSuccubus.Core.Desire;

public static class DesireResource
{
    public const string LocalId = "desire";

    public static SecondaryResourceDefinition Definition { get; private set; }
        = null!;

    public static string Id => Definition.Id;

    public static void Register()
    {
        string smallIconPath = RuntimeTextureAssets.PrepareResource(
            "ui/core/desire_resource_icon_32.png",
            "user://maiden_succubus_desire_resource_32.res",
            "res://images/packed/sprite_fonts/star_icon.png");
        string largeIconPath = RuntimeTextureAssets.PrepareResource(
            "ui/core/desire_resource_icon_128.png",
            "user://maiden_succubus_desire_resource_128.res",
            "res://images/ui/combat/energy_star.png");

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
                smallIconPath: smallIconPath,
                largeIconPath: largeIconPath,
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

        registry.RegisterCombatUi(
            "desire_combat_counter",
            parent =>
            {
                var counter = NSecondaryResourceCounter.Create(
                    Definition,
                    SecondaryResourceCounterStyle.Default with
                    {
                        CounterSize = new Vector2(128f, 128f),
                        IconSize = new Vector2(128f, 128f),
                        FontSize = 36,
                        OutlineSize = 14,
                        AmountLabelOffset = new Vector2(0f, 20f),
                        OutlineColor = StsColors.defaultStarCostOutline,
                        GainFeedback = SecondaryResourceCounterGainFeedback.StarCounterLike,
                        FormatAmount = (amount, _) => amount.ToString(),
                    });

                // Match the vanilla NStarCounter scene's bottom-left anchor,
                // offsets and scale so Desire sits beside the energy counter.
                counter.SetAnchorsPreset(Godot.Control.LayoutPreset.BottomLeft);
                // Clear the vanilla energy counter instead of overlapping it.
                counter.Position = new Vector2(176f, -212f);
                counter.Scale = Vector2.One * 0.8f;
                counter.PivotOffset = new Vector2(64f, 64f);
                return counter;
            },
            context => context.Node.Bind(context.Player, autoRefresh: false),
            context =>
            {
                if (context.Definition.Id != Definition.Id)
                {
                    return;
                }
                context.Node.Visible = context.VisibleDefinitions.Any(
                    definition => definition.Id == Definition.Id);
                context.Node.SetAmount(
                    context.NewAmount,
                    SecondaryResourceCmd.GetMax(context.Player, Definition.Id));
            });

        registry.AlwaysShowInCombatUiForCharacter<MaidenSuccubusCharacter>(
            Definition.LocalId);
        SecondaryResourceHook.RegisterGlobalListener(
            new DesireResourceRules());

        MaidenSuccubusMod.Logger.Info(
            $"Secondary resource registered: {Definition.Id} " +
            $"(persistence=Run, max=10)");
    }
}
