using BrudvikWhiteHilt.Items.Binding;
using BrudvikWhiteHilt.Monsters;
using BrudvikWhiteHilt.Pieces.Smithing.RuneForge;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Gear;

/// <summary>
/// Shows a bound trophy and an etched rune in the tooltip, gives the web and the grip of the deep their effect on hit,
/// and lets the Rune Forge extensions show their own hover text.
/// </summary>
[HarmonyPatch]
public static class BindingPatches
{
    private static readonly int webHash = MonsterRegistry.WebEffectName.GetStableHashCode();

    /// <summary>
    /// Adds the bound trophy and the etched rune to the item's tooltip.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="__result">The tooltip.</param>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) })]
    [HarmonyPostfix]
    public static void GetTooltip(ItemDrop.ItemData item, ref string __result)
    {
        __result += GearBinding.TooltipText(item);
    }

    /// <summary>
    /// On the attacker's machine: a weapon etched with the web webs the target, and one etched with the grip of the deep
    /// heals the attacker for part of the damage.
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

        ItemDrop.ItemData weapon = player.GetCurrentWeapon();
        if (GearBinding.Webs(weapon) && hit.m_statusEffectHash == 0)
        {
            hit.m_statusEffectHash = webHash;
        }
        else if (GearBinding.Dreads(weapon) && hit.m_statusEffectHash == 0 && !__instance.IsBoss()
            && UnityEngine.Random.value < BindingSettings.DreadChance.Value)
        {
            hit.m_statusEffectHash = DreadEffect.Hash;
        }
        else if (GearBinding.Tars(weapon) && hit.m_statusEffectHash == 0)
        {
            hit.m_statusEffectHash = SEMan.s_statusEffectTared;
        }

        hit.m_damage.m_poison *= GearBinding.PoisonMultiplier(weapon, __instance);

        float rage = GearBinding.Rage(weapon, player);
        if (rage > 0f)
        {
            hit.m_damage.Modify(1f + rage);
        }

        float lifeSteal = GearBinding.LifeSteal(weapon);
        float damage = hit.GetTotalDamage();
        if (lifeSteal > 0f && damage > 0f)
        {
            player.Heal(damage * lifeSteal);
        }
    }

    /// <summary>
    /// Shows a Rune Forge extension's own hover text in place of the vanilla extension text.
    /// </summary>
    /// <param name="__instance">The extension.</param>
    /// <param name="__result">The hover text.</param>
    [HarmonyPatch(typeof(StationExtension), nameof(StationExtension.GetHoverText))]
    [HarmonyPostfix]
    public static void GetHoverText(StationExtension __instance, ref string __result)
    {
        RuneForgeExtensionComponent extension = __instance.GetComponent<RuneForgeExtensionComponent>();
        if (extension != null)
        {
            __result = extension.GetHoverText();
        }
    }
}
