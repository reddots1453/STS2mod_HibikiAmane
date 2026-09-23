using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
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
