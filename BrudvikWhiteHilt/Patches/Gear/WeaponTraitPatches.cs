using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Weapons;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Gear;

/// <summary>
/// Gives the White Hilt weapons' traits their effect on hit and shows them in the tooltip.
/// </summary>
[HarmonyPatch]
public static class WeaponTraitPatches
{
    /// <summary>
    /// Registers the English tooltip lines. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_trait_bleed", "Bleeding: {0} damage per second for {1} s");
        Translations.AddEnglish("whitehilt_trait_shieldhook", "Hooks shields: {0} stagger against a shield");
        Translations.AddEnglish("whitehilt_trait_guardbreak", "Swings past the guard: {0}% of blows cannot be blocked");
        Translations.AddEnglish("whitehilt_trait_armorbreak", "Breaks armour: the target takes {0}% more damage for {1} s");
        Translations.AddEnglish("whitehilt_trait_largefoe", "Made for big game: {0}% more damage against large creatures");
        Translations.AddEnglish("whitehilt_trait_sea", "Of the sea: {0}% more damage against foes in the water and sea creatures");
        Translations.AddEnglish("whitehilt_trait_lifesteal", "Reaps life: {0}% of the damage dealt heals you");
    }

    /// <summary>
    /// On the attacker's machine: a weapon that bleeds makes the target bleed, and one that hooks shields staggers a
    /// target with a shield more.
    /// </summary>
    /// <param name="__instance">The character hit.</param>
    /// <param name="hit">The hit.</param>
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    [HarmonyPrefix]
    public static void Damage(Character __instance, HitData hit)
    {
        Player player = Player.m_localPlayer;
        if (hit == null || player == null || __instance == player || hit.m_attacker != player.GetZDOID())
        {
            return;
        }

        WeaponTrait trait = WeaponTraits.Get(player.GetCurrentWeapon());
        if (trait == null)
        {
            return;
        }

        if (trait.Bleeds && hit.m_statusEffectHash == 0)
        {
            hit.m_statusEffectHash = BleedEffect.Hash;
        }

        if (trait.BreaksArmor && hit.m_statusEffectHash == 0)
        {
            hit.m_statusEffectHash = ArmorBreakEffect.Hash;
        }

        if (trait.ShieldHook != null && BearsShield(__instance))
        {
            hit.m_staggerMultiplier *= trait.ShieldHook();
        }

        if (trait.GuardBreakChance != null && UnityEngine.Random.value < trait.GuardBreakChance())
        {
            hit.m_blockable = false;
        }

        if (trait.LargeFoeBonus != null && WeaponTraits.IsLarge(__instance))
        {
            hit.m_damage.Modify(trait.LargeFoeBonus());
        }

        if (trait.SeaBonus != null && WeaponTraits.IsOfTheSea(__instance))
        {
            hit.m_damage.Modify(trait.SeaBonus());
        }

        float damage = hit.GetTotalDamage();
        if (trait.LifeSteal != null && damage > 0f)
        {
            player.Heal(damage * trait.LifeSteal());
        }
    }

    /// <summary>
    /// Adds the weapon's traits to its tooltip.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="__1">The quality the tooltip is shown for (the second argument).</param>
    /// <param name="__result">The tooltip.</param>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) })]
    [HarmonyPostfix]
    public static void GetTooltip(ItemDrop.ItemData item, int __1, ref string __result)
    {
        WeaponTrait trait = WeaponTraits.Get(item);
        if (trait == null)
        {
            return;
        }

        Localization localization = Localization.instance;
        if (trait.Bleeds)
        {
            __result += "\n<color=orange>" + string.Format(localization.Localize("$whitehilt_trait_bleed"),
                Translations.Number(BleedEffect.DamageAtQuality(__1)), Translations.Number(BleedEffect.Seconds)) + "</color>";
        }

        if (trait.ShieldHook != null)
        {
            __result += "\n<color=orange>" + string.Format(localization.Localize("$whitehilt_trait_shieldhook"),
                "×" + Translations.Number(trait.ShieldHook())) + "</color>";
        }

        if (trait.GuardBreakChance != null)
        {
            __result += "\n<color=orange>" + string.Format(localization.Localize("$whitehilt_trait_guardbreak"),
                Translations.Percent(trait.GuardBreakChance())) + "</color>";
        }

        if (trait.BreaksArmor)
        {
            __result += "\n<color=orange>" + string.Format(localization.Localize("$whitehilt_trait_armorbreak"),
                Translations.Percent(ArmorBreakEffect.ExtraDamage), Translations.Number(ArmorBreakEffect.Seconds)) + "</color>";
        }

        if (trait.LargeFoeBonus != null)
        {
            __result += "\n<color=orange>" + string.Format(localization.Localize("$whitehilt_trait_largefoe"),
                Translations.Percent(trait.LargeFoeBonus() - 1f)) + "</color>";
        }

        if (trait.SeaBonus != null)
        {
            __result += "\n<color=orange>" + string.Format(localization.Localize("$whitehilt_trait_sea"),
                Translations.Percent(trait.SeaBonus() - 1f)) + "</color>";
        }

        if (trait.LifeSteal != null)
        {
            __result += "\n<color=orange>" + string.Format(localization.Localize("$whitehilt_trait_lifesteal"),
                Translations.Percent(trait.LifeSteal())) + "</color>";
        }
    }

    // A shield in the left hand, or a raised guard: what the beard of an axe can hook.
    private static bool BearsShield(Character character)
    {
        if (character.IsBlocking())
        {
            return true;
        }

        return character is Humanoid humanoid
            && humanoid.GetLeftItem()?.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield;
    }
}
