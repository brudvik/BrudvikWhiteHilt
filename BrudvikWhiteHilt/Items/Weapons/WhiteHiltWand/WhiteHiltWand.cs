using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltWand;

/// <summary>
/// The White Hilt Wand, a one-handed staff cloned from the vanilla <c>StaffFireball</c>, so a shield can be carried
/// with it: smaller fireballs for half the eitr, with the White Hilt model <c>whwand</c> and an orange flame on its
/// tip while held. In linear progression it unlocks with the Mistlands tier.
/// </summary>
public class WhiteHiltWand : WhiteHiltWeaponBase
{
    private const float Strength = 0.6f;
    private const float EitrCost = 0.5f;
    private static readonly Color flame = new(1f, 0.55f, 0.2f);

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltWand(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltWand";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Wand";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Wand of Dyrnwyn. Small fire for half the eitr, and a hand free for a shield.";

    /// <inheritdoc/>
    protected override string CopyFrom => "StaffFireball";

    /// <summary>
    /// Magic Wand by Plat, with a white grip.
    /// </summary>
    protected override string ModelName => "whwand";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mistlands;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "YggdrasilWood", Amount = 5, Recover = false },
        new() { Item = "Eitr", Amount = 8, Recover = false },
        new() { Item = "SurtlingCore", Amount = 5, Recover = false }
    };

    /// <summary>
    /// One-handed, weaker fireballs for half the eitr.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_itemType = ItemDrop.ItemData.ItemType.OneHandedWeapon;
        shared.m_damages.Modify(Strength);
        shared.m_damagesPerLevel.Modify(Strength);
        shared.m_attack.m_attackEitr *= EitrCost;
    }

    /// <summary>
    /// Puts the orange flame on the tip.
    /// </summary>
    /// <param name="model">The wand model under the attach child.</param>
    protected override void OnModelApplied(GameObject model)
    {
        Bounds bounds = model.GetComponent<MeshFilter>().sharedMesh.bounds;
        StaffFlame.Apply(model, flame, new Vector3(0f, 0f, bounds.max.z));
    }
}
