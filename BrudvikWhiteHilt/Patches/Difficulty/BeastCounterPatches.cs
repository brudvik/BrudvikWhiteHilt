using BrudvikWhiteHilt.Bestiary;
using BrudvikWhiteHilt.Helpers;
using HarmonyLib;
using System.Linq;

namespace BrudvikWhiteHilt.Patches.Difficulty;

/// <summary>Resolves material hit markers on the creature owner before normal damage handling.</summary>
[HarmonyPatch]
public static class BeastCounterPatches
{
    /// <summary>Saves the local character's discovered biome without depending on its translated name.</summary>
    /// <param name="__instance">Character discovering a biome.</param>
    /// <param name="biome">Discovered sector.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.AddKnownBiome))]
    [HarmonyPostfix]
    public static void DiscoveredBiome(Player __instance, BiomeSector biome)
    {
        if (__instance == Player.m_localPlayer && biome != null)
        {
            BeastCounter.DiscoverBiome(__instance, biome.Biome);
        }
    }

    /// <summary>Consumes the projectile or treatment marker exactly once, on the target owner.</summary>
    /// <param name="__instance">The target.</param>
    /// <param name="hit">The networked hit, independent of the attacker's currently held weapon.</param>
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    [HarmonyPrefix]
    public static void Damage(Character __instance, HitData hit)
    {
        if (__instance.m_nview != null && __instance.m_nview.IsValid() && __instance.m_nview.IsOwner())
        {
            BeastCounter.Apply(global::Utils.GetPrefabName(__instance.gameObject), hit);
        }
    }

    /// <summary>Shows the configured target and material bonus on a counter item.</summary>
    /// <param name="item">Item being described.</param>
    /// <param name="__result">Localized tooltip.</param>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) })]
    [HarmonyPostfix]
    public static void Tooltip(ItemDrop.ItemData item, ref string __result)
    {
        BeastCounter counter = BeastCounter.All.FirstOrDefault(candidate => item?.m_shared?.m_name == Translations.Token(candidate.NameKey));
        if (counter != null)
        {
            __result += "\n" + string.Format(Localization.instance.Localize("$whitehilt_counter_bonus"),
                Localization.instance.Localize(Translations.Token(counter.Beast.NameKey)), Translations.Number(counter.Bonus * 100f));
            if (!counter.IsArrow)
            {
                __result += "\n" + string.Format(Localization.instance.Localize("$whitehilt_counter_treatment"),
                    Localization.instance.Localize(Translations.Token(counter.Beast.NameKey)), counter.Attacks);
            }
        }
    }

    /// <summary>Rejects incompatible treatment use and replaces the previous material preparation.</summary>
    /// <param name="__instance">User.</param>
    /// <param name="item">Consumable.</param>
    /// <param name="__result">Whether the use was handled.</param>
    /// <returns>True to use the normal consumption path.</returns>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UseItem))]
    [HarmonyPrefix]
    public static bool UseItem(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
    {
        BeastCounterCoating coating = item?.m_shared?.m_consumeStatusEffect as BeastCounterCoating;
        if (coating == null)
        {
            return true;
        }
        BeastCounter counter = BeastCounter.Get(coating.CounterKey);
        if (!BeastCounterCoating.Accepts(counter, __instance.GetCurrentWeapon()))
        {
            __instance.Message(MessageHud.MessageType.Center, "$whitehilt_counter_wrongweapon");
            __result = true;
            return false;
        }
        if (__instance is Player player && player.CanConsumeItem(item))
        {
            foreach (BeastCounter definition in BeastCounter.All.Where(candidate => !candidate.IsArrow))
            {
                player.GetSEMan().RemoveStatusEffect(("SE_" + definition.PrefabName).GetStableHashCode());
            }
        }
        return true;
    }

    /// <summary>Applies configured output counts to recipes copied into a newly loaded ObjectDB.</summary>
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    [HarmonyPostfix]
    public static void Recipes() => BeastCounterItem.RefreshYields();

    /// <summary>Closes the guide on Escape without also opening the game menu.</summary>
    /// <returns>False while the book owns input or closed in this frame.</returns>
    [HarmonyPatch(typeof(Menu), nameof(Menu.Update))]
    [HarmonyPrefix]
    public static bool MenuUpdate()
    {
        if (!BeastBookPanel.BlocksMenu)
        {
            return true;
        }
        if (ZInput.GetKeyDown(UnityEngine.KeyCode.Escape))
        {
            BeastBookPanel.Close();
        }
        return false;
    }
}