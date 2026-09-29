using BrudvikWhiteHilt.Items.Ranching;
using BrudvikWhiteHilt.Ranching;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Ranching;

/// <summary>
/// Grooming with the <see cref="GroomingComb"/>, tethering, production and the slaughter bonus of content animals.
/// </summary>
[HarmonyPatch]
public static class AnimalCarePatches
{
    /// <summary>
    /// Registers the grooming and tethering RPCs.
    /// </summary>
    /// <param name="__instance">The animal.</param>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.Awake))]
    [HarmonyPostfix]
    private static void RegisterRpcs(Tameable __instance)
    {
        AnimalCare.RegisterRpcs(__instance);
    }

    /// <summary>
    /// Grooms a tame animal when the player uses the comb on it.
    /// </summary>
    /// <param name="__instance">The animal.</param>
    /// <param name="user">The player.</param>
    /// <param name="item">The item used.</param>
    /// <param name="__result">True when the comb was used.</param>
    /// <returns>False to skip vanilla for the comb.</returns>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.UseItem))]
    [HarmonyPrefix]
    private static bool GroomWithComb(Tameable __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
    {
        if (!GroomingComb.IsComb(item) || __instance.m_nview == null || !__instance.m_nview.IsValid())
        {
            return true;
        }

        __result = true;
        if (!__instance.IsTamed())
        {
            user.Message(MessageHud.MessageType.Center, "$msg_whitehilt_groom_wild");
            return false;
        }

        string name = __instance.GetHoverName();
        if (AnimalCare.IsContent(__instance))
        {
            user.Message(MessageHud.MessageType.Center, $"{name} $msg_whitehilt_groom_already");
            return false;
        }

        AnimalCare.Groom(__instance);
        __instance.m_sootheEffect.Create(__instance.transform.position, Quaternion.identity);
        user.Message(MessageHud.MessageType.Center, $"{name} $msg_whitehilt_groomed");
        if (user is Player player && player == Player.m_localPlayer)
        {
            player.RaiseSkill(HusbandrySkill.Type, AnimalCare.GroomExperience);
            HusbandrySkill.ShareLevel(player);
        }

        return false;
    }

    /// <summary>
    /// Lets content, fed animals put their product in a nearby trough. Runs every 3 seconds.
    /// </summary>
    /// <param name="__instance">The animal.</param>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.TamingUpdate))]
    [HarmonyPostfix]
    private static void Produce(Tameable __instance)
    {
        AnimalCare.UpdateProduction(__instance);
    }

    /// <summary>
    /// Shows whether a tame animal is groomed and tethered.
    /// </summary>
    /// <param name="__instance">The animal.</param>
    /// <param name="__result">The hover text.</param>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.GetHoverText))]
    [HarmonyPostfix]
    private static void ShowCare(Tameable __instance, ref string __result)
    {
        if (string.IsNullOrEmpty(__result) || !__instance.IsTamed())
        {
            return;
        }

        string text = AnimalCare.IsContent(__instance) ? "\n$whitehilt_animal_content" : "\n$whitehilt_animal_wants_grooming";
        if (AnimalCare.IsTethered(__instance))
        {
            text += "\n$whitehilt_animal_tethered";
        }

        __result += Localization.instance.Localize(text);
    }

    /// <summary>
    /// A content animal gives one more of each drop, except its trophy, when slaughtered.
    /// </summary>
    /// <param name="__instance">The animal's drops.</param>
    /// <param name="__result">The drops.</param>
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    [HarmonyPostfix]
    private static void ContentBonus(CharacterDrop __instance, List<KeyValuePair<GameObject, int>> __result)
    {
        Tameable tameable = __instance.GetComponent<Tameable>();
        if (__result == null || tameable == null || !tameable.IsTamed() || !AnimalCare.IsContent(tameable))
        {
            return;
        }

        for (int i = 0; i < __result.Count; i++)
        {
            if (__result[i].Key != null && __result[i].Value > 0 && !__result[i].Key.name.StartsWith("Trophy"))
            {
                __result[i] = new KeyValuePair<GameObject, int>(__result[i].Key, __result[i].Value + 1);
            }
        }
    }
}
