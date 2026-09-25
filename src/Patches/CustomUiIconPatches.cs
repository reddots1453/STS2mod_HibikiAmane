using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.UI;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Patches;

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconTexture), MethodType.Getter)]
public static class MaidenCharacterIconPatch
{
    public static bool Prefix(CharacterModel __instance, ref Texture2D __result)
    {
        if (__instance is not MaidenSuccubusCharacter)
        {
            return true;
        }
        Texture2D? replacement = null;
        Safe.Run(
            () => replacement = RuntimeTextureAssets.Load(
                "ui/core/hibiki_amane_character_icon_128.png"),
            nameof(MaidenCharacterIconPatch));
        if (replacement == null)
        {
            return true;
        }
        __result = replacement;
        return false;
    }
}

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconOutlineTexture), MethodType.Getter)]
public static class MaidenCharacterIconOutlinePatch
{
    public static bool Prefix(CharacterModel __instance, ref Texture2D __result)
    {
        if (__instance is not MaidenSuccubusCharacter)
        {
            return true;
        }
        Texture2D? replacement = null;
        Safe.Run(
            () => replacement = RuntimeTextureAssets.Load(
                "ui/core/hibiki_amane_character_icon_outline_128.png"),
            nameof(MaidenCharacterIconOutlinePatch));
        if (replacement == null)
        {
            return true;
        }
        __result = replacement;
        return false;
    }
}

/// <summary>
/// Vanilla character icon scenes keep their visible portrait inside the
/// center half of the run-history player slot. RitsuLib's texture-backed
/// fallback fills the entire slot, so constrain only that generated
/// TextureRect while leaving the shared character icon asset unchanged.
/// </summary>
[HarmonyPatch(typeof(NRunHistoryPlayerIcon), nameof(NRunHistoryPlayerIcon.LoadRun))]
public static class MaidenRunHistoryCharacterIconPatch
{
    private const float InsetAnchor = 0.25f;
    private const float FarAnchor = 0.75f;
    private static readonly FieldInfo? CurrentIconField =
        AccessTools.Field(typeof(NRunHistoryPlayerIcon), "_currentIcon");

    public static bool Prepare()
    {
        bool compatible = CurrentIconField?.FieldType == typeof(Control);
        if (!compatible)
        {
            MaidenSuccubusMod.Logger.Warn(
                "[MaidenRunHistoryCharacterIconPatch] Run-history icon field changed; "
                + "portrait sizing patch disabled safely.");
        }
        return compatible;
    }

    public static void Postfix(
        NRunHistoryPlayerIcon __instance,
        RunHistoryPlayer player)
    {
        Safe.Run(
            () =>
            {
                CharacterModel character =
                    ModelDb.GetById<CharacterModel>(player.Character);
                if (character is not MaidenSuccubusCharacter
                    || CurrentIconField!.GetValue(__instance)
                        is not TextureRect icon)
                {
                    return;
                }

                icon.AnchorLeft = InsetAnchor;
                icon.AnchorTop = InsetAnchor;
                icon.AnchorRight = FarAnchor;
                icon.AnchorBottom = FarAnchor;
                icon.OffsetLeft = 0f;
                icon.OffsetTop = 0f;
                icon.OffsetRight = 0f;
                icon.OffsetBottom = 0f;
            },
            nameof(MaidenRunHistoryCharacterIconPatch));
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.EnergyIcon), MethodType.Getter)]
public static class MaidenCardEnergyIconPatch
{
    public static bool Prefix(CardModel __instance, ref Texture2D __result)
    {
        if (__instance.GetType().Assembly != typeof(MaidenSuccubusMod).Assembly)
        {
            return true;
        }
        Texture2D? replacement = null;
        Safe.Run(
            () => replacement = RuntimeTextureAssets.Load(
                "ui/core/magic_energy_cost_icon_128.png"),
            nameof(MaidenCardEnergyIconPatch));
        if (replacement == null)
        {
            return true;
        }
        __result = replacement;
        return false;
    }
}

/// <summary>
/// NIntent's frame loop normally writes a vanilla atlas frame after
/// UpdateVisuals. Custom intents opt out of that animation, and this stable
/// public update entry installs their reviewed standalone sprite. Any field
/// signature mismatch disables this cosmetic patch safely.
/// </summary>
[HarmonyPatch(typeof(NIntent), nameof(NIntent.UpdateIntent))]
public static class MaidenIntentSpritePatch
{
    private static readonly FieldInfo? IntentField =
        AccessTools.Field(typeof(NIntent), "_intent");
    private static readonly FieldInfo? AnimationNameField =
        AccessTools.Field(typeof(NIntent), "_animationName");
    private static readonly FieldInfo? AnimationFrameField =
        AccessTools.Field(typeof(NIntent), "_animationFrame");

    public static bool Prepare()
    {
        bool compatible = IntentField?.FieldType ==
                typeof(MegaCrit.Sts2.Core.MonsterMoves.Intents.AbstractIntent)
            && AnimationNameField?.FieldType == typeof(string)
            && AnimationFrameField?.FieldType == typeof(int?);
        if (!compatible)
        {
            MaidenSuccubusMod.Logger.Warn(
                "[MaidenIntentSpritePatch] NIntent fields changed; custom intent sprites disabled safely.");
        }
        return compatible;
    }

    public static void Postfix(NIntent __instance)
    {
        Safe.Run(
            () =>
            {
                var intent = IntentField!.GetValue(__instance) as
                    MegaCrit.Sts2.Core.MonsterMoves.Intents.AbstractIntent;
                if (intent is not (ControlIntent or InvasionIntent
                    or DesireGainIntent or TearClothingIntent))
                {
                    return;
                }

                Texture2D? texture = MaidenIntentIconAssets.Get(intent);
                if (texture == null)
                {
                    return;
                }

                __instance.GetNode<Sprite2D>("%Intent").Texture = texture;
                // Keep both fields clear even if another mod called the
                // update entry with stale animation state on this reused node.
                AnimationNameField!.SetValue(__instance, null);
                AnimationFrameField!.SetValue(__instance, null);
            },
            nameof(MaidenIntentSpritePatch));
    }
}
