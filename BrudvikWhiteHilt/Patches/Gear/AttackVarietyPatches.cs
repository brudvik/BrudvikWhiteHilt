using BrudvikWhiteHilt.Items.Weapons.Styles;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Gear;

/// <summary>
/// Hands the White Hilt weapons' attacks to <see cref="AttackVariety"/> before the game picks their animation, shows
/// the styles in the tooltip and gives every player the Rune Sword's own cuts (<see cref="RuneSwordMotion"/>).
/// </summary>
[HarmonyPatch]
public static class AttackVarietyPatches
{
    /// <summary>
    /// Notes whether the attack about to start is the secondary one, which keeps its own animation.
    /// </summary>
    /// <param name="secondaryAttack">The game's argument.</param>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    [HarmonyPrefix]
    public static void StartAttackPrefix(bool secondaryAttack)
    {
        AttackVariety.BeginStart(secondaryAttack);
    }

    /// <summary>
    /// Clears the note, also when the attack throws.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    [HarmonyFinalizer]
    public static void StartAttackFinalizer()
    {
        AttackVariety.EndStart();
    }

    /// <summary>
    /// Gives the attack, a fresh clone of the weapon's, the style of the local player's combo before the game sets
    /// the animation trigger from it.
    /// </summary>
    /// <param name="__instance">The attack.</param>
    /// <param name="character">Who attacks.</param>
    /// <param name="weapon">The weapon.</param>
    /// <param name="previousAttack">The attack before it, or null.</param>
    /// <param name="timeSinceLastAttack">Seconds since the previous attack.</param>
    [HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
    [HarmonyPrefix]
    public static void AttackStart(Attack __instance, Humanoid character, ItemDrop.ItemData weapon, Attack previousAttack,
        float timeSinceLastAttack)
    {
        AttackVariety.Apply(__instance, character, weapon, previousAttack, timeSinceLastAttack);
    }

    /// <summary>
    /// Names the weapon's attack styles in its tooltip.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="__result">The tooltip.</param>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) })]
    [HarmonyPostfix]
    public static void GetTooltip(ItemDrop.ItemData item, ref string __result)
    {
        string line = AttackVariety.TooltipLine(item);
        if (line != null)
        {
            __result += "\n" + line;
        }
    }

    /// <summary>
    /// Gives every player, the local one and the others, the component that swaps in the Rune Sword's clips while it is
    /// in their hand.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    [HarmonyPostfix]
    public static void PlayerAwake(Player __instance)
    {
        RuneSwordMotion.Attach(__instance);
    }
}
