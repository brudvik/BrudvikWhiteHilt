using System;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.Items.Weapons;

/// <summary>
/// What sets a White Hilt weapon apart when it hits, beyond its damage. Values are read when they are used, so config
/// changes apply at once.
/// </summary>
public sealed class WeaponTrait
{
    /// <summary>
    /// Whether a hit makes the target bleed (<see cref="BleedEffect"/>).
    /// </summary>
    public bool Bleeds { get; set; }

    /// <summary>
    /// Stagger multiplier against a target that bears or raises a shield, or null for none: the beard of the axe
    /// hooks the shield.
    /// </summary>
    public Func<float> ShieldHook { get; set; }

    /// <summary>
    /// Chance (0 to 1) that a blow goes round the guard and cannot be blocked, or null for none: a flail's head
    /// swings past the shield.
    /// </summary>
    public Func<float> GuardBreakChance { get; set; }

    /// <summary>
    /// Whether a hit breaks the target's armour for a while (<see cref="ArmorBreakEffect"/>).
    /// </summary>
    public bool BreaksArmor { get; set; }

    /// <summary>
    /// Damage multiplier against large creatures (<see cref="WeaponTraits.IsLarge"/>), or null for none.
    /// </summary>
    public Func<float> LargeFoeBonus { get; set; }

    /// <summary>
    /// Share of the damage dealt that heals the wielder, or null for none.
    /// </summary>
    public Func<float> LifeSteal { get; set; }

    /// <summary>
    /// Damage multiplier against a target in the water or a sea creature (<see cref="WeaponTraits.IsOfTheSea"/>), or
    /// null for none.
    /// </summary>
    public Func<float> SeaBonus { get; set; }
}

/// <summary>
/// The traits of the White Hilt weapons, by the item's shared name, for the hit and tooltip patches.
/// </summary>
public static class WeaponTraits
{
    /// <summary>
    /// Height in metres from which a creature counts as large: trolls, lox, abominations, serpents, gjall.
    /// </summary>
    public const float LargeHeight = 3f;

    private static readonly Dictionary<string, WeaponTrait> bySharedName = new();

    /// <summary>
    /// Whether a creature is large, by the height of its body. Measured rather than listed, so creatures from other
    /// mods count too.
    /// </summary>
    /// <param name="character">The creature.</param>
    /// <returns>True if it is at least <see cref="LargeHeight"/> tall.</returns>
    public static bool IsLarge(Character character)
    {
        return character != null && !character.IsPlayer() && character.GetHeight() >= LargeHeight;
    }

    /// <summary>
    /// Whether a target is in the water, or a creature of the sea (serpents, krakens, octopuses) wherever it is.
    /// </summary>
    /// <param name="character">The target.</param>
    /// <returns>True for a target in the water or a sea creature.</returns>
    public static bool IsOfTheSea(Character character)
    {
        if (character == null)
        {
            return false;
        }

        if (character.InWater() || character.IsSwimming())
        {
            return true;
        }

        string name = Utils.GetPrefabName(character.gameObject);
        return name.IndexOf("Serpent", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Kraken", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Octopus", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Registers a weapon's trait; a null trait is skipped.
    /// </summary>
    /// <param name="sharedName">The item's shared name, e.g. <c>$item_whitehiltseax</c>.</param>
    /// <param name="trait">The trait.</param>
    public static void Register(string sharedName, WeaponTrait trait)
    {
        if (trait != null)
        {
            bySharedName[sharedName] = trait;
        }
    }

    /// <summary>
    /// The trait of an item, or null if it has none.
    /// </summary>
    /// <param name="item">The item, or null.</param>
    /// <returns>The trait, or null.</returns>
    public static WeaponTrait Get(ItemDrop.ItemData item)
    {
        return item != null && bySharedName.TryGetValue(item.m_shared.m_name, out WeaponTrait trait) ? trait : null;
    }
}
