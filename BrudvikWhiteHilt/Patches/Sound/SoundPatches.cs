using BrudvikWhiteHilt.Helpers;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Sound;

/// <summary>
/// Hooks the mod's own sounds onto the game: the fight music in place of the game's combat music, the blood moon's
/// drums, and the sound of drinking the mod's meads and the gods' gifts.
/// </summary>
[HarmonyPatch]
public static class SoundPatches
{
    private const string MeadPrefix = "drink_";
    private const string GiftPrefix = "GiftOf";
    private const string GiftSuffix = "Mead";

    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<BloodMoonAmbience>();
    }

    [HarmonyPatch(typeof(MusicMan), "Awake")]
    [HarmonyPostfix]
    private static void MusicAwakePostfix(MusicMan __instance)
    {
        WhiteHiltMusic.Register(__instance);
    }

    // In a fight with a great beast, near a Nail Ship's crew or under a blood moon, the mod's fight music plays instead
    // of the game's; when the fight is over it stops as the game's own would.
    [HarmonyPatch(typeof(MusicMan), "HandleCombatMusic")]
    [HarmonyPrefix]
    private static bool CombatMusicPrefix(MusicMan __instance, string currentMusic, ref bool __result)
    {
        WhiteHiltMusic.Register(__instance);
        string ours = __instance.InCombat() ? WhiteHiltMusic.Choose() : null;
        if (ours != null)
        {
            __instance.StartMusic(ours);
            __result = true;
            return false;
        }

        if (WhiteHiltMusic.IsOurs(currentMusic))
        {
            __instance.StopMusic();
            __result = false;
            return false;
        }

        return true;
    }

    // A god's gift sounds of its god; the mod's meads, ales and wines are gulped down.
    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeItem))]
    [HarmonyPostfix]
    private static void ConsumePostfix(Player __instance, ItemDrop.ItemData item, bool __result)
    {
        string name = item?.m_dropPrefab != null ? item.m_dropPrefab.name : null;
        if (!__result || name == null || __instance != Player.m_localPlayer)
        {
            return;
        }

        if (name.StartsWith(GiftPrefix, System.StringComparison.Ordinal) && name.EndsWith(GiftSuffix, System.StringComparison.Ordinal))
        {
            WhiteHiltSound.PlayLocal(MeadPrefix + name.Substring(0, name.Length - GiftSuffix.Length).ToLowerInvariant());
        }
        else if (global::BrudvikWhiteHilt.Items.Meads.WhiteHiltMeadBase.IsMead(name))
        {
            WhiteHiltSound.PlayLocal(global::BrudvikWhiteHilt.Monsters.CreatureSoundNames.Pick(MeadPrefix + "mead"));
        }
    }
}
