using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltStaffIce;

/// <summary>
/// The White Hilt Staff of Ice, cloned from the vanilla <c>StaffIceShards</c> and the White Hilt model <c>whstaff</c>
/// from the asset bundle. In linear progression it unlocks with the Mountain tier.
/// </summary>
public class WhiteHiltStaffIce : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltStaffIce(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltStaffIce";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Staff of Ice";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Staff of Ice of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "StaffIceShards";

    /// <summary>
    /// Dark scepter from Weapon Set by Asylum Nox, with a white grip wrap.
    /// </summary>
    protected override string ModelName => "whstaff";

    /// <summary>
    /// Lights a blue flame on the scepter head.
    /// </summary>
    /// <param name="model">The staff model under the attach child.</param>
    protected override void OnModelApplied(GameObject model)
    {
        StaffFlame.Apply(model, new Color(0.25f, 0.55f, 1f));
    }

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 10, Recover = false },
        new() { Item = "ElderBark", Amount = 10, Recover = false },
        new() { Item = "Guck", Amount = 5, Recover = false }
    };
}
