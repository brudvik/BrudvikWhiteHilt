using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons.Styles;

/// <summary>
/// Lets the White Hilt melee weapons swing in several styles. Each weapon registers its styles, signature first; when
/// the local player starts a combo, one is drawn and written onto the attack the game has just cloned from the weapon,
/// before the game picks the animation from it.
/// </summary>
/// <remarks>
/// Runs on the attacker's machine only. The animation trigger reaches the other players through the game's own
/// animation sync, and the hit is the attacker's, as always, so nothing new crosses the network.
/// </remarks>
public static class AttackVariety
{
    // The game continues a combo only while the next click comes within this many seconds (Attack.Start).
    private const float ChainWindow = 0.2f;

    private static readonly Dictionary<string, WeaponStyles> weapons = new();
    private static bool startingSecondary;

    /// <summary>
    /// Registers the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_attackstyles", "Attack styles: {0}");
        Translations.AddEnglish("whitehilt_attackstyle_slash", "Slash");
        Translations.AddEnglish("whitehilt_attackstyle_chop", "Chop");
        Translations.AddEnglish("whitehilt_attackstyle_stab", "Stab");
        Translations.AddEnglish("whitehilt_attackstyle_lunge", "Lunge");
        Translations.AddEnglish("whitehilt_attackstyle_cleave", "Cleave");
        Translations.AddEnglish("whitehilt_attackstyle_greatsword", "Greatsword");
        Translations.AddEnglish("whitehilt_attackstyle_slam", "Slam");
        Translations.AddEnglish("whitehilt_attackstyle_hew", "Hew");
        Translations.AddEnglish("whitehilt_attackstyle_polearm", "Polearm");
        Translations.AddEnglish("whitehilt_attackstyle_rune", "Rune whirl");
    }

    /// <summary>
    /// Registers a weapon's styles. Styles that need the other grip are left out with a warning, since a two-handed
    /// swing would pull the shield arm onto the hilt.
    /// </summary>
    /// <param name="shared">The weapon's shared data, after its stats are final.</param>
    /// <param name="styles">The weapon's styles, signature first, or null for those of its vanilla animation.</param>
    /// <returns>True if the weapon swings in styles; false for bows, crossbows and staffs.</returns>
    public static bool Register(ItemDrop.ItemData.SharedData shared, AttackStyle[] styles)
    {
        string trigger = shared.m_attack?.m_attackAnimation;
        if (!AttackStyles.TryFromTrigger(trigger, out AttackStyle native))
        {
            return false;
        }

        bool twoHanded = shared.m_itemType is ItemDrop.ItemData.ItemType.TwoHandedWeapon
            or ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft;
        AttackStyle[] pool = (styles ?? AttackStyles.DefaultPool(trigger)).Distinct().ToArray();
        AttackStyle[] fitting = pool.Where(style => AttackStyles.IsTwoHanded(style) == twoHanded).ToArray();
        if (fitting.Length < pool.Length)
        {
            Jotunn.Logger.LogWarning($"{shared.m_name}: styles {string.Join(", ", pool.Except(fitting))} do not suit its grip and are left out.");
        }

        if (fitting.Length == 0)
        {
            return false;
        }

        weapons[shared.m_name] = new WeaponStyles(native, fitting);
        return true;
    }

    /// <summary>
    /// Marks whether the attack being started is the weapon's secondary one, which keeps its own animation.
    /// </summary>
    /// <param name="secondary">True for a secondary attack.</param>
    public static void BeginStart(bool secondary)
    {
        startingSecondary = secondary;
    }

    /// <summary>
    /// Clears the mark set by <see cref="BeginStart"/>.
    /// </summary>
    public static void EndStart()
    {
        startingSecondary = false;
    }

    /// <summary>
    /// Gives an attack the style of the local player's combo, before the game picks its animation. A combo that
    /// carries on keeps its style; a new one draws.
    /// </summary>
    /// <param name="attack">The attack, a fresh clone of the weapon's primary attack.</param>
    /// <param name="character">Who attacks.</param>
    /// <param name="weapon">The weapon.</param>
    /// <param name="previous">The attack before it, or null.</param>
    /// <param name="timeSinceLastAttack">Seconds since the previous attack.</param>
    public static void Apply(Attack attack, Humanoid character, ItemDrop.ItemData weapon, Attack previous, float timeSinceLastAttack)
    {
        if (startingSecondary || character == null || character != Player.m_localPlayer || weapon == null
            || !weapons.TryGetValue(weapon.m_shared.m_name, out WeaponStyles entry))
        {
            return;
        }

        AttackVarietyMode mode = AttackVarietySettings.Effective;
        if (mode == AttackVarietyMode.Vanilla)
        {
            return;
        }

        List<AttackStyle> usable = Usable(entry, character);
        if (usable.Count == 0)
        {
            return;
        }

        if (!Continues(previous, weapon, timeSinceLastAttack, usable, out AttackStyle style))
        {
            style = mode == AttackVarietyMode.Signature
                ? usable[0]
                : AttackStyles.Pick(usable, AttackVarietySettings.SignatureShare.Value, Random.value);
        }

        if (style == entry.Native)
        {
            return;
        }

        float evenOut = AttackVarietySettings.EvenOutTempo.Value;
        attack.m_attackAnimation = AttackStyles.Trigger(style);
        attack.m_attackChainLevels = AttackStyles.ChainLevels(style);
        attack.m_attackRandomAnimations = 0;
        attack.m_attackAngle = AttackStyles.Angle(style);
        attack.m_damageMultiplier *= AttackStyles.DamageFactor(entry.Native, style, attack.m_lastChainDamageMultiplier, evenOut);
        attack.m_attackStamina *= AttackStyles.StaminaFactor(entry.Native, style, evenOut);
    }

    /// <summary>
    /// The tooltip line naming a weapon's styles, or null when it has none or the player swings it as vanilla.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The line, or null.</returns>
    public static string TooltipLine(ItemDrop.ItemData item)
    {
        if (item == null || !weapons.TryGetValue(item.m_shared.m_name, out WeaponStyles entry))
        {
            return null;
        }

        AttackVarietyMode mode = AttackVarietySettings.Effective;
        if (mode == AttackVarietyMode.Vanilla)
        {
            return null;
        }

        IEnumerable<AttackStyle> shown = mode == AttackVarietyMode.Signature ? entry.Styles.Take(1) : entry.Styles;
        string names = string.Join(", ", shown.Select(style => Translations.Word($"whitehilt_attackstyle_{style.ToString().ToLowerInvariant()}")));
        return string.Format(Translations.Word("whitehilt_attackstyles"), names);
    }

    // The Rune Sword's own cuts need its clips in the player's animator; without them the dual knives would play.
    private static List<AttackStyle> Usable(WeaponStyles entry, Humanoid character)
    {
        return entry.Styles.Where(style => style != AttackStyle.Rune || RuneSwordMotion.IsActive(character)).ToList();
    }

    // Mirrors the game's own test for a combo going on, so the style changes only where the game starts a new combo.
    private static bool Continues(Attack previous, ItemDrop.ItemData weapon, float timeSinceLastAttack,
        List<AttackStyle> usable, out AttackStyle style)
    {
        style = default;
        return previous != null
            && previous.m_weapon == weapon
            && previous.m_nextAttackChainLevel > 0
            && timeSinceLastAttack <= ChainWindow
            && AttackStyles.TryFromTrigger(previous.m_attackAnimation, out style)
            && usable.Contains(style);
    }

    private sealed class WeaponStyles
    {
        public WeaponStyles(AttackStyle native, AttackStyle[] styles)
        {
            Native = native;
            Styles = styles;
        }

        public AttackStyle Native { get; }

        public AttackStyle[] Styles { get; }
    }
}
