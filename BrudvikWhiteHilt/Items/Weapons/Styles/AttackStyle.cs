namespace BrudvikWhiteHilt.Items.Weapons.Styles;

/// <summary>
/// A way of swinging a melee weapon: one of the vanilla attack animations of the player, or the Rune Sword's own.
/// See <see cref="AttackStyles"/> for the animation and timing behind each.
/// </summary>
public enum AttackStyle
{
    /// <summary>The sword's three cuts (<c>swing_longsword</c>), also used by vanilla maces, clubs and torches.</summary>
    Slash,

    /// <summary>The one-handed axe's three chops (<c>swing_axe</c>).</summary>
    Chop,

    /// <summary>The knife's three quick stabs and slashes (<c>knife_stab</c>).</summary>
    Stab,

    /// <summary>The spear's single overhand thrust (<c>spear_poke</c>).</summary>
    Lunge,

    /// <summary>The battleaxe's three heavy two-handed swings (<c>battleaxe_attack</c>).</summary>
    Cleave,

    /// <summary>The greatsword's three two-handed cuts (<c>greatsword</c>).</summary>
    Greatsword,

    /// <summary>The sledge's single overhead slam (<c>swing_sledge</c>).</summary>
    Slam,

    /// <summary>The pickaxe's single overhead blow (<c>swing_pickaxe</c>).</summary>
    Hew,

    /// <summary>The atgeir's three thrusts and sweeps (<c>atgeir_attack</c>).</summary>
    Polearm,

    /// <summary>
    /// The Rune Sword's own three cuts, ending in a whirl. They play in the dual knives' states, whose clips are swapped
    /// for the Rune Sword's while it is in hand (see RuneSwordMotion).
    /// </summary>
    Rune
}
