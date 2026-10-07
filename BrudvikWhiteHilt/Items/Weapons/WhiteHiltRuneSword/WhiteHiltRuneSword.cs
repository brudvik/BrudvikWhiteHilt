using BrudvikWhiteHilt.Items.Weapons.Styles;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltRuneSword;

/// <summary>
/// The White Hilt Rune Sword, cloned from the vanilla <c>SwordNiedhogg</c> with the White Hilt model
/// <c>whrunesword</c>, whose carved runes glow pale blue, or in the colour of a Glow Rune etched into it. In linear
/// progression it unlocks with the Ashlands tier.
/// </summary>
public class WhiteHiltRuneSword : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltRuneSword(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltRuneSword";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Rune Sword";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Rune Sword of Dyrnwyn. Its carved runes glow.";

    /// <inheritdoc/>
    protected override string CopyFrom => "SwordNiedhogg";

    /// <summary>
    /// Its own cuts first: an overhead strike, a lunging thrust and a leaping whirl (see RuneSwordMotion).
    /// </summary>
    protected override AttackStyle[] Swings => new[] { AttackStyle.Rune, AttackStyle.Slash, AttackStyle.Stab };

    /// <summary>
    /// Viking Rune Sword by Pitchforkone, with a white grip; its runes glow.
    /// </summary>
    protected override string ModelName => "whrunesword";

    /// <inheritdoc/>
    protected override Color? ModelGlow => new Color(0.6f, 0.85f, 1f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Ashlands;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FlametalNew", Amount = 20, Recover = false },
        new() { Item = "CharredBone", Amount = 10, Recover = false },
        new() { Item = "Eitr", Amount = 10, Recover = false }
    };
}
