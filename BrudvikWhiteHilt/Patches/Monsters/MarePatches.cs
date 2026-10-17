using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Monsters;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Monsters;

/// <summary>
/// The Mare's ride and the charm against her: a player who sleeps in the Mistlands may wake hurt and in her Nightmare,
/// and one who wears a Mare Charm is never given the Nightmare at all.
/// </summary>
[HarmonyPatch]
public static class MarePatches
{
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.Internal_AddStatusEffect))]
    [HarmonyPrefix]
    private static bool AddStatusEffectPrefix(SEMan __instance, int nameHash, ref StatusEffect __result)
    {
        if (nameHash != MareSpirit.NightmareHash || !MareSpirit.Warded(__instance.m_character))
        {
            return true;
        }

        __result = null;
        return false;
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SetSleeping))]
    [HarmonyPrefix]
    private static void SetSleepingPrefix(Player __instance, out bool __state)
    {
        __state = __instance.IsSleeping();
    }

    // On waking: a sleeper in the Mistlands may have been ridden.
    [HarmonyPatch(typeof(Player), nameof(Player.SetSleeping))]
    [HarmonyPostfix]
    private static void SetSleepingPostfix(Player __instance, bool sleep, bool __state)
    {
        if (!__state || sleep || __instance != Player.m_localPlayer || !WildMonsterSettings.Of(WildMonsters.Mare).Enabled.Value
            || Heightmap.FindBiome(__instance.transform.position) != Heightmap.Biome.Mistlands
            || MareSpirit.Warded(__instance) || Random.value * 100f >= MareSettings.SleepChance.Value)
        {
            return;
        }

        __instance.SetHealth(Mathf.Min(__instance.GetHealth(), __instance.GetMaxHealth() * MareSettings.SleepHealth.Value));
        __instance.GetSEMan().AddStatusEffect(MareSpirit.NightmareHash, resetTime: true);
        __instance.Message(MessageHud.MessageType.Center, "$msg_whitehilt_mare_rode");
        if (!VisualHelper.IsHeadless)
        {
            GameObject holder = new("whitehilt_mare_ride");
            holder.transform.position = __instance.transform.position;
            AudioSource source = holder.AddComponent<AudioSource>();
            WhiteHiltSound.Place(source, 4f, 20f);
            WhiteHiltSound.PlayFrom(source, "mare_ride");
            Object.Destroy(holder, 6f);
        }
    }
}
