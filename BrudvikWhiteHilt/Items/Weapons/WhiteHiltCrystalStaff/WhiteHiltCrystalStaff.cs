using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltCrystalStaff;

/// <summary>
/// The White Hilt Crystal Staff, cloned from the vanilla <c>StaffIceShards</c> and a third stronger than the White
/// Hilt Staff of Ice, with the White Hilt model <c>whcrystalstaff</c>, whose crystal glows cyan, and a cyan flame on
/// its head while held. In linear progression it unlocks with the Mistlands tier.
/// </summary>
public class WhiteHiltCrystalStaff : WhiteHiltWeaponBase
{
    private const float Strength = 1.3f;
    private static readonly Color glow = new(0.35f, 1f, 0.95f);

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltCrystalStaff(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltCrystalStaff";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Crystal Staff";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Crystal Staff of Dyrnwyn. Its shards cut colder than the Staff of Ice.";

    /// <inheritdoc/>
    protected override string CopyFrom => "StaffIceShards";

    /// <summary>
    /// Crystal Staff by StalemateZ, with a white grip; its crystal glows.
    /// </summary>
    protected override string ModelName => "whcrystalstaff";

    /// <inheritdoc/>
    protected override Color? ModelGlow => glow;

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mistlands;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "YggdrasilWood", Amount = 15, Recover = false },
        new() { Item = "Eitr", Amount = 16, Recover = false },
        new() { Item = "Crystal", Amount = 10, Recover = false }
    };

    /// <summary>
    /// A third more damage than the vanilla staff.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_damages.Modify(Strength);
        shared.m_damagesPerLevel.Modify(Strength);
    }

    /// <summary>
    /// Puts the cyan flame just below the top of the crystal.
    /// </summary>
    /// <param name="model">The staff model under the attach child.</param>
    protected override void OnModelApplied(GameObject model)
    {
        Bounds bounds = model.GetComponent<MeshFilter>().sharedMesh.bounds;
        StaffFlame.Apply(model, glow, new Vector3(0f, 0f, bounds.max.z - 0.15f));
    }
}
