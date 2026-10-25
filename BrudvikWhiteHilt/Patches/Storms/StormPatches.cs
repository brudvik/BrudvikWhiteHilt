using BrudvikWhiteHilt.Darkness;
using BrudvikWhiteHilt.Storms;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Storms;

/// <summary>
/// Hooks the storms onto the game object, puts their weather where the local player is and thickens the fog as the
/// view closes in.
/// </summary>
[HarmonyPatch]
public static class StormPatches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<StormService>();
        __instance.gameObject.AddComponent<StormClient>();
        __instance.gameObject.AddComponent<StormAmbience>();
        __instance.gameObject.AddComponent<StormHazards>();
        __instance.gameObject.AddComponent<StormWind>();
        __instance.gameObject.AddComponent<StormHail>();
        __instance.gameObject.AddComponent<StormVisions>();
        __instance.gameObject.AddComponent<StormRagnarok>();
        __instance.gameObject.AddComponent<RagnarokSpawner>();
        __instance.gameObject.AddComponent<RagnarokFire>();
        __instance.gameObject.AddComponent<Fimbulwinter>();
    }

    // Lost at sea in the storm: no place on the map.
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePlayerMarker))]
    [HarmonyPostfix]
    private static void UpdatePlayerMarkerPostfix(Minimap __instance)
    {
        StormBearings.Apply(__instance);
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.SetMapMode))]
    [HarmonyPostfix]
    private static void SetMapModePostfix(Minimap __instance)
    {
        StormBearings.Opened(__instance);
    }

    // After the game's own forced weathers: a dungeon, a raid, a boss or a special place still wins.
    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.GetEnvironmentOverride))]
    [HarmonyPostfix]
    private static void GetEnvironmentOverridePostfix(EnvMan __instance, ref string __result)
    {
        if (string.IsNullOrEmpty(__result))
        {
            __result = StormWeather.Override(__instance) ?? DeepNorthNight.Override(__instance) ?? Fimbulwinter.Override(__instance);
        }
    }

    // Nothing grows in the Fimbulwinter: the time a crop has grown leaves it out.
    [HarmonyPatch(typeof(Plant), nameof(Plant.TimeSincePlanted))]
    [HarmonyPostfix]
    private static void TimeSincePlantedPostfix(ref double __result)
    {
        if (StormState.FimbulStarts > 0.0 && ZNet.instance != null)
        {
            __result = RagnarokRules.Grown(__result, ZNet.instance.GetTimeSeconds(), StormState.FimbulStarts, StormState.FimbulEnds);
        }
    }

    // No weapon bites on Fenrir: the blow is stopped on the attacker's machine, before it is sent.
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    [HarmonyPrefix]
    private static bool CharacterDamagePrefix(Character __instance, HitData hit)
    {
        RagnarokHost host = RagnarokHost.Loaded.Count > 0 ? __instance.GetComponent<RagnarokHost>() : null;
        if (host == null || !host.IsFenrir)
        {
            return true;
        }

        if (hit != null && Player.m_localPlayer != null && hit.GetAttacker() == Player.m_localPlayer)
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_whitehilt_ragnarok_fenrir_immune");
        }

        return false;
    }

    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.SetEnv))]
    [HarmonyPostfix]
    private static void SetEnvPostfix()
    {
        StormWeather.ApplyFog();
        StormRagnarok.ApplyLight();
    }
}
