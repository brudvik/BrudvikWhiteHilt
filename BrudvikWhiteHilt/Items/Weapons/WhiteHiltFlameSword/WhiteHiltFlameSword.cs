using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltFlameSword;

/// <summary>
/// The White Hilt Flame Sword, the Ice Sword's fiery twin: cloned from the vanilla <c>SwordSilver</c> with fire in place
/// of its spirit damage, on the Ice Sword's model <c>whicesword</c> with an ember-red glow and Dyrnwyn's fire along the
/// blade. Fire sets what it hits burning. The flames are its own, so a Flame Rune recolours them or puts them out. In
/// linear progression it unlocks with the Mountain tier, as the Ice Sword does.
/// </summary>
public class WhiteHiltFlameSword : WhiteHiltWeaponBase
{
    private const float BladeShare = 0.7f;
    private static readonly Color flameColor = new(1f, 0.45f, 0.12f);

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltFlameSword(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltFlameSword";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Flame Sword";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Flame Sword of Dyrnwyn. Its fire sets alight what it cuts.";

    /// <inheritdoc/>
    protected override string CopyFrom => "SwordSilver";

    /// <summary>
    /// The Ice Sword's model, glowing like embers instead of ice.
    /// </summary>
    protected override string ModelName => "whicesword";

    /// <inheritdoc/>
    protected override Color? ModelGlow => new Color(1f, 0.35f, 0.08f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Silver", Amount = 20, Recover = false },
        new() { Item = "SurtlingCore", Amount = 10, Recover = false },
        new() { Item = "Obsidian", Amount = 5, Recover = false }
    };

    /// <summary>
    /// Lights Dyrnwyn's fire along the blade.
    /// </summary>
    /// <param name="model">The sword model under the attach child.</param>
    protected override void OnModelApplied(GameObject model)
    {
        WeaponFlame.AddOwnFlame(model, flameColor, BladeShare);
    }

    /// <summary>
    /// The Silver Sword's slash with fire instead of spirit.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_damages.m_fire = shared.m_damages.m_spirit;
        shared.m_damages.m_spirit = 0f;
        shared.m_damagesPerLevel.m_fire = shared.m_damagesPerLevel.m_spirit;
        shared.m_damagesPerLevel.m_spirit = 0f;
    }
}
