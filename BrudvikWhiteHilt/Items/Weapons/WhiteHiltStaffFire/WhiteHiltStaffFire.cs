using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltStaffFire;

/// <summary>
/// The White Hilt Staff of Fire, cloned from the vanilla <c>StaffFireball</c> and the White Hilt model <c>whstaff</c>
/// from the asset bundle. In linear progression it unlocks with the Mountain tier.
/// </summary>
public class WhiteHiltStaffFire : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltStaffFire(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltStaffFire";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Staff of Fire";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Staff of Fire of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "StaffFireball";

    /// <summary>
    /// Dark scepter from Weapon Set by Asylum Nox, with a white grip wrap.
    /// </summary>
    protected override string ModelName => "whstaff";

    /// <summary>
    /// Lights a red flame on the scepter head.
    /// </summary>
    /// <param name="model">The staff model under the attach child.</param>
    protected override void OnModelApplied(GameObject model)
    {
        StaffFlame.Apply(model, new Color(1f, 0.22f, 0.12f));
    }

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "SurtlingCore", Amount = 10, Recover = false },
        new() { Item = "ElderBark", Amount = 10, Recover = false },
        new() { Item = "Guck", Amount = 5, Recover = false }
    };
}
