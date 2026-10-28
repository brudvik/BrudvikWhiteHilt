using BrudvikWhiteHilt.Performance;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Performance;

/// <summary>
/// Answers ObjectDB's recipe and status effect searches from <see cref="ObjectDBLookups"/> when it knows the answer,
/// and lets vanilla search otherwise. Both replace vanilla's method only on a hit, so they run last and stand back when
/// an earlier prefix has already answered.
/// </summary>
[HarmonyPatch]
public static class LookupPatches
{
    /// <summary>
    /// Finds an item's recipe by name.
    /// </summary>
    /// <param name="__instance">The object database.</param>
    /// <param name="item">The item.</param>
    /// <param name="__result">The recipe.</param>
    /// <param name="__runOriginal">False when an earlier prefix has answered.</param>
    /// <returns>False when the recipe was found here.</returns>
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.GetRecipe))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    public static bool GetRecipe(ObjectDB __instance, ItemDrop.ItemData item, ref Recipe __result, bool __runOriginal)
    {
        if (!__runOriginal || !VanillaFixSettings.RecipeLookup.Active || !ObjectDBLookups.TryGetRecipe(__instance, item, out Recipe recipe))
        {
            return __runOriginal;
        }

        __result = recipe;
        return false;
    }

    /// <summary>
    /// Finds a status effect by name hash.
    /// </summary>
    /// <param name="__instance">The object database.</param>
    /// <param name="nameHash">The status effect's name hash.</param>
    /// <param name="__result">The status effect.</param>
    /// <param name="__runOriginal">False when an earlier prefix has answered.</param>
    /// <returns>False when the status effect was found here.</returns>
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.GetStatusEffect), typeof(int))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    public static bool GetStatusEffect(ObjectDB __instance, int nameHash, ref StatusEffect __result, bool __runOriginal)
    {
        if (!__runOriginal || !VanillaFixSettings.StatusEffectLookup.Active
            || !ObjectDBLookups.TryGetStatusEffect(__instance, nameHash, out StatusEffect effect))
        {
            return __runOriginal;
        }

        __result = effect;
        return false;
    }
}
