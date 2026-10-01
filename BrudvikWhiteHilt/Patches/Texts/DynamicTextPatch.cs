using BrudvikWhiteHilt.Helpers;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Texts;

/// <summary>
/// Fills config values into texts registered with <see cref="Translations.AddDynamic"/>.
/// </summary>
[HarmonyPatch(typeof(Localization), nameof(Localization.Translate))]
public static class DynamicTextPatch
{
    private static void Postfix(string word, ref string __result)
    {
        __result = Translations.FillDynamic(word, __result);
    }
}
