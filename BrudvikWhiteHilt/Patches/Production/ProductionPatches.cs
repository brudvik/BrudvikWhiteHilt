using BrudvikWhiteHilt.Production;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Production;

/// <summary>
/// Production timers: remembers the stations as they wake up, adds the timers to their hover texts, and updates the
/// labels, the overview and the messages with the HUD.
/// </summary>
[HarmonyPatch]
public static class ProductionPatches
{
    /// <summary>Remembers a smelter.</summary>
    /// <param name="__instance">The station.</param>
    [HarmonyPatch(typeof(Smelter), nameof(Smelter.Awake))]
    [HarmonyPostfix]
    public static void AddSmelter(Smelter __instance) => ProductionRegistry.Add(__instance);

    /// <summary>Remembers a fermenter.</summary>
    /// <param name="__instance">The station.</param>
    [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.Awake))]
    [HarmonyPostfix]
    public static void AddFermenter(Fermenter __instance) => ProductionRegistry.Add(__instance);

    /// <summary>Remembers a cooking station.</summary>
    /// <param name="__instance">The station.</param>
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.Awake))]
    [HarmonyPostfix]
    public static void AddCookingStation(CookingStation __instance) => ProductionRegistry.Add(__instance);

    /// <summary>Remembers a beehive.</summary>
    /// <param name="__instance">The station.</param>
    [HarmonyPatch(typeof(Beehive), nameof(Beehive.Awake))]
    [HarmonyPostfix]
    public static void AddBeehive(Beehive __instance) => ProductionRegistry.Add(__instance);

    /// <summary>Remembers a sap collector.</summary>
    /// <param name="__instance">The station.</param>
    [HarmonyPatch(typeof(SapCollector), nameof(SapCollector.Awake))]
    [HarmonyPostfix]
    public static void AddSapCollector(SapCollector __instance) => ProductionRegistry.Add(__instance);

    /// <summary>Remembers a fire.</summary>
    /// <param name="__instance">The fire.</param>
    [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.Awake))]
    [HarmonyPostfix]
    public static void AddFire(Fireplace __instance) => ProductionRegistry.Add(__instance);

    /// <summary>Remembers an egg.</summary>
    /// <param name="__instance">The egg.</param>
    [HarmonyPatch(typeof(EggGrow), nameof(EggGrow.Start))]
    [HarmonyPostfix]
    public static void AddEgg(EggGrow __instance) => ProductionRegistry.Add(__instance);

    /// <summary>Remembers an animal that can breed.</summary>
    /// <param name="__instance">The animal.</param>
    [HarmonyPatch(typeof(Procreation), nameof(Procreation.Awake))]
    [HarmonyPostfix]
    public static void AddAnimal(Procreation __instance) => ProductionRegistry.Add(__instance);

    /// <summary>
    /// Smelters and cooking stations are hovered through their switches: the ore and fuel holes, the food spit.
    /// </summary>
    /// <param name="__instance">The switch.</param>
    /// <param name="__result">The hover text.</param>
    [HarmonyPatch(typeof(Switch), nameof(Switch.GetHoverText))]
    [HarmonyPostfix]
    public static void SwitchHover(Switch __instance, ref string __result)
    {
        if (string.IsNullOrEmpty(__result))
        {
            return;
        }

        Smelter smelter = __instance.GetComponentInParent<Smelter>();
        if (smelter != null)
        {
            __result = ProductionRegistry.AppendHover(smelter, __result);
            return;
        }

        CookingStation station = __instance.GetComponentInParent<CookingStation>();
        if (station != null)
        {
            __result = ProductionRegistry.AppendHover(station, __result);
        }
    }

    /// <summary>Adds the timers to a cooking station hovered without a switch.</summary>
    /// <param name="__instance">The station.</param>
    /// <param name="__result">The hover text.</param>
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.GetHoverText))]
    [HarmonyPostfix]
    public static void CookingHover(CookingStation __instance, ref string __result) => __result = ProductionRegistry.AppendHover(__instance, __result);

    /// <summary>Adds the timer to a fermenter's hover text.</summary>
    /// <param name="__instance">The station.</param>
    /// <param name="__result">The hover text.</param>
    [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.GetHoverText))]
    [HarmonyPostfix]
    public static void FermenterHover(Fermenter __instance, ref string __result) => __result = ProductionRegistry.AppendHover(__instance, __result);

    /// <summary>Adds the timers to a beehive's hover text.</summary>
    /// <param name="__instance">The station.</param>
    /// <param name="__result">The hover text.</param>
    [HarmonyPatch(typeof(Beehive), nameof(Beehive.GetHoverText))]
    [HarmonyPostfix]
    public static void BeehiveHover(Beehive __instance, ref string __result) => __result = ProductionRegistry.AppendHover(__instance, __result);

    /// <summary>Adds the timers to a sap collector's hover text.</summary>
    /// <param name="__instance">The station.</param>
    /// <param name="__result">The hover text.</param>
    [HarmonyPatch(typeof(SapCollector), nameof(SapCollector.GetHoverText))]
    [HarmonyPostfix]
    public static void SapCollectorHover(SapCollector __instance, ref string __result) => __result = ProductionRegistry.AppendHover(__instance, __result);

    /// <summary>Adds how long the fuel lasts to a fire's hover text.</summary>
    /// <param name="__instance">The fire.</param>
    /// <param name="__result">The hover text.</param>
    [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.GetHoverText))]
    [HarmonyPostfix]
    public static void FireHover(Fireplace __instance, ref string __result) => __result = ProductionRegistry.AppendHover(__instance, __result);

    /// <summary>Adds when an egg hatches, or why it does not, to its hover text.</summary>
    /// <param name="__instance">The egg.</param>
    /// <param name="__result">The hover text.</param>
    [HarmonyPatch(typeof(EggGrow), nameof(EggGrow.GetHoverText))]
    [HarmonyPostfix]
    public static void EggHover(EggGrow __instance, ref string __result) => __result = ProductionRegistry.AppendHover(__instance, __result);

    /// <summary>Adds when a tame animal gives birth, or what keeps it from breeding, to its hover text.</summary>
    /// <param name="__instance">The animal.</param>
    /// <param name="__result">The hover text.</param>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.GetHoverText))]
    [HarmonyPostfix]
    public static void AnimalHover(Tameable __instance, ref string __result)
    {
        Procreation procreation = __instance.GetComponent<Procreation>();
        if (procreation != null)
        {
            __result = ProductionRegistry.AppendHover(procreation, __result);
        }
    }

    /// <summary>
    /// Updates the labels, the overview and the messages after the game's HUD update.
    /// </summary>
    /// <param name="__instance">The HUD.</param>
    [HarmonyPatch(typeof(Hud), nameof(Hud.Update))]
    [HarmonyPostfix]
    public static void HudUpdate(Hud __instance)
    {
        Player player = Player.m_localPlayer;
        if (player == null || ZNet.instance == null)
        {
            return;
        }

        ProductionLabels.Update(player);
        ProductionOverview.Update(__instance, player);
        ProductionNotifier.Update(player);
    }
}
