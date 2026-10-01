using BrudvikWhiteHilt.Mastery;
using BrudvikWhiteHilt.Pieces.Farming;
using HarmonyLib;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Mastery;

/// <summary>
/// Foraging and Farming milestones on wild plants, crops and seeds.
/// </summary>
[HarmonyPatch]
public static class PickingPatches
{
    /// <summary>
    /// Remembers pickables for sweep picking.
    /// </summary>
    /// <param name="__instance">The pickable.</param>
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.Awake))]
    [HarmonyPostfix]
    private static void RegisterPickable(Pickable __instance)
    {
        WildPicks.Register(__instance);
    }

    /// <summary>
    /// Notes whether this pick is a new one.
    /// </summary>
    /// <param name="__instance">The pickable.</param>
    /// <param name="__state">True if not picked yet.</param>
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
    [HarmonyPrefix]
    private static void BeforePick(Pickable __instance, out bool __state)
    {
        __state = __instance.m_nview != null && __instance.m_nview.IsValid() && !__instance.m_picked && !__instance.m_pickedLocal && __instance.m_enabled != 0;
    }

    /// <summary>
    /// Raises Foraging for a wild pick and sweeps the same plants nearby.
    /// </summary>
    /// <param name="__instance">The pickable.</param>
    /// <param name="character">Who picks.</param>
    /// <param name="__state">True if not picked before.</param>
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
    [HarmonyPostfix]
    private static void AfterPick(Pickable __instance, Humanoid character, bool __state)
    {
        if (!__state || character is not Player player || player != Player.m_localPlayer || !MasterySettings.Foraging.Value || !WildPicks.IsWild(__instance))
        {
            return;
        }

        player.RaiseSkill(ForagingSkill.Type);
        WildPicks.Sweep(__instance, player);
    }

    /// <summary>
    /// On the machine that owns the plant: extra yield, giants and the stars of what is picked.
    /// </summary>
    /// <param name="__instance">The pickable.</param>
    /// <param name="sender">The picker's peer.</param>
    /// <param name="bonus">Extra items.</param>
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.RPC_Pick))]
    [HarmonyPrefix]
    private static void BeforeDrop(Pickable __instance, long sender, ref int bonus)
    {
        if (__instance.m_nview == null || !__instance.m_nview.IsOwner() || __instance.m_picked)
        {
            return;
        }

        Player picker = SkillLevels.FindPlayer(sender);
        if (__instance.m_pickRaiseSkill == Skills.SkillType.Farming)
        {
            if (MasterySettings.Farming.Value)
            {
                bonus += Crops.PrepareHarvest(__instance, picker);
            }

            return;
        }

        if (!MasterySettings.Foraging.Value || !WildPicks.IsWild(__instance))
        {
            return;
        }

        float level = SkillLevels.Get(picker, ForagingSkill.Type);
        if (Random.value < Perks.ForagingExtraYield(level))
        {
            bonus++;
        }

        int max = Stars.MaxAt(level, Perks.KeenEye, Perks.ForagersBounty);
        if (max > 0)
        {
            float scale = Perks.SeasonSense.ReachedAt(level) && WildPicks.IsBestTime(__instance) ? 2f : 1f;
            Stars.DropStars = _ => Stars.Roll(level, max, scale);
        }
    }

    /// <summary>
    /// Stops giving stars.
    /// </summary>
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.RPC_Pick))]
    [HarmonyFinalizer]
    private static void AfterDrop()
    {
        Stars.DropStars = null;
    }

    /// <summary>
    /// Shows when a wild plant is at its best, and giant crops.
    /// </summary>
    /// <param name="__instance">The pickable.</param>
    /// <param name="__result">Hover text.</param>
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.GetHoverText))]
    [HarmonyPostfix]
    private static void HoverHints(Pickable __instance, ref string __result)
    {
        if (string.IsNullOrEmpty(__result) || __instance.m_picked)
        {
            return;
        }

        if (Crops.IsGiant(__instance))
        {
            __result += Localization.instance.Localize("\n<color=#8fd18f>$whitehilt_giant_crop</color>");
        }
        else if (Perks.SeasonSense.Has(Player.m_localPlayer) && WildPicks.IsWild(__instance) && WildPicks.IsBestTime(__instance))
        {
            __result += Localization.instance.Localize("\n<color=#8fd18f>$whitehilt_best_time</color>");
        }
    }

    /// <summary>
    /// Notes the planter's Farming level on a new plant.
    /// </summary>
    /// <param name="__instance">The piece.</param>
    /// <param name="uid">The creator's player id.</param>
    [HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
    [HarmonyPostfix]
    private static void NotePlanter(Piece __instance, long uid)
    {
        if (Player.m_localPlayer != null && uid == Player.m_localPlayer.GetPlayerID())
        {
            Crops.OnPlanted(__instance);
        }
    }

    /// <summary>
    /// Starts counting the stars of what is paid.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void BeforePay()
    {
        Stars.BeginLedger();
    }

    /// <summary>
    /// Gives plants just placed the stars of their seeds.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
    [HarmonyFinalizer]
    private static void AfterPay()
    {
        Crops.OnSeedsPaid(Stars.LedgerAverage());
        Stars.EndLedger();
    }

    /// <summary>
    /// Reads the seed stars and planter before the plant is replaced by its crop.
    /// </summary>
    /// <param name="__instance">The plant.</param>
    /// <param name="__state">Seed stars and planter level.</param>
    [HarmonyPatch(typeof(Plant), nameof(Plant.Grow))]
    [HarmonyPrefix]
    private static void BeforeGrow(Plant __instance, out (int Stars, int Farmer) __state)
    {
        ZDO zdo = __instance.m_nview != null && __instance.m_nview.IsValid() ? __instance.m_nview.GetZDO() : null;
        __state = zdo != null ? (zdo.GetInt(Crops.SeedStarsKey), zdo.GetInt(Crops.FarmerKey)) : (0, 0);
    }

    /// <summary>
    /// Carries the seed stars and planter to the crop, which may become a giant.
    /// </summary>
    /// <param name="__result">The grown crop.</param>
    /// <param name="__state">Seed stars and planter level.</param>
    [HarmonyPatch(typeof(Plant), nameof(Plant.Grow))]
    [HarmonyPostfix]
    private static void AfterGrow(GameObject __result, (int Stars, int Farmer) __state)
    {
        if (__result != null && MasterySettings.Farming.Value)
        {
            Crops.OnGrown(__state.Stars, __state.Farmer, __result);
        }
    }

    /// <summary>
    /// Grows faster for a green thumb and near compost.
    /// </summary>
    /// <param name="__instance">The plant.</param>
    /// <param name="__result">Growing time in seconds.</param>
    [HarmonyPatch(typeof(Plant), nameof(Plant.GetGrowTime))]
    [HarmonyPostfix]
    private static void GrowFaster(Plant __instance, ref float __result)
    {
        if (MasterySettings.Farming.Value)
        {
            __result *= Crops.GrowTimeFactor(__instance);
        }
        else
        {
            __result *= CompostBinComponent.GrowTimeFactor(__instance.transform.position);
        }
    }

    /// <summary>
    /// Adds what the Compost Bin is doing to its hover text.
    /// </summary>
    /// <param name="__instance">The container.</param>
    /// <param name="__result">Hover text.</param>
    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    [HarmonyPostfix]
    private static void CompostHover(Container __instance, ref string __result)
    {
        CompostBinComponent bin = __instance.GetComponent<CompostBinComponent>();
        if (bin != null && !string.IsNullOrEmpty(__result))
        {
            __result += bin.StatusText();
        }
    }
}
