using BrudvikWhiteHilt.Items.Navigation;
using BrudvikWhiteHilt.Mastery;
using BrudvikWhiteHilt.Navigation;
using BrudvikWhiteHilt.Navigation.Waypoints;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Navigation;

/// <summary>
/// Hooks the ruby amulet into the game: Shift + click on the large map sets the target, the arrows and marker follow
/// the map's update, and the amulet's recipe needs the Ruby Pathfinder milestone in Exploration.
/// </summary>
[HarmonyPatch]
public static class WaypointPatches
{
    /// <summary>
    /// Shift + click sets or removes the target instead of ticking a pin.
    /// </summary>
    /// <param name="__instance">The map.</param>
    /// <returns>False for a target click.</returns>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapLeftClick))]
    [HarmonyPrefix]
    public static bool LeftClick(Minimap __instance)
    {
        if (!WaypointGuide.IsTargetClick())
        {
            return true;
        }

        WaypointGuide.OnMapClick(__instance, __instance.ScreenToWorldPoint(ZInput.pointerPosition));
        return false;
    }

    /// <summary>
    /// Two quick Shift + clicks do not open the pin name box.
    /// </summary>
    /// <returns>False for a target click.</returns>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapDblClick))]
    [HarmonyPrefix]
    public static bool DoubleClick()
    {
        return !WaypointGuide.IsTargetClick();
    }

    /// <summary>
    /// Keeps the arrows and the marker up to date.
    /// </summary>
    /// <param name="__instance">The map.</param>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
    [HarmonyPostfix]
    public static void UpdatePostfix(Minimap __instance)
    {
        WaypointGuide.Update(__instance);
    }

    /// <summary>
    /// The ruby amulet can be learnt and made only with the Ruby Pathfinder milestone. One already made keeps working.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="recipe">The recipe.</param>
    /// <param name="__result">False while the milestone is missing.</param>
    /// <returns>False when the recipe is refused.</returns>
    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), new[] { typeof(Recipe), typeof(bool), typeof(int), typeof(int) })]
    [HarmonyPrefix]
    public static bool RubyRecipe(Player __instance, Recipe recipe, ref bool __result)
    {
        if (recipe?.m_item == null || recipe.m_item.name != RubyPathfinderAmulet.RubyPrefabName
            || Perks.RubyPathfinder.ReachedAt(ExplorationSkill.GetLevel(__instance)))
        {
            return true;
        }

        __result = false;
        return false;
    }

    /// <summary>
    /// Learns the ruby amulet's recipe as soon as Exploration reaches the milestone.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="skill">The skill that went up.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.OnSkillLevelup))]
    [HarmonyPostfix]
    public static void SkillLevelup(Player __instance, Skills.SkillType skill)
    {
        if (skill == ExplorationSkill.Type && __instance == Player.m_localPlayer)
        {
            __instance.UpdateKnownRecipesList();
        }
    }
}
