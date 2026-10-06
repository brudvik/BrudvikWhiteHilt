using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltStaffLightning;

/// <summary>
/// The White Hilt Staff of Lightning, cloned from the vanilla <c>StaffShield</c> and the White Hilt model
/// <c>whstaff</c> from the asset bundle. In linear progression it unlocks with the Plains tier.
/// </summary>
public class WhiteHiltStaffLightning : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltStaffLightning(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltStaffLightning";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Staff of Lightning";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Staff of Lightning of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "StaffShield";

    /// <summary>
    /// Dark scepter from Weapon Set by Asylum Nox, with a white grip wrap.
    /// </summary>
    protected override string ModelName => "whstaff";

    /// <summary>
    /// Lights a white-blue flame on the scepter head, the colour of lightning; green belongs to the Necromancer's
    /// Staff.
    /// </summary>
    /// <param name="model">The staff model under the attach child.</param>
    protected override void OnModelApplied(GameObject model)
    {
        StaffFlame.Apply(model, new Color(0.7f, 0.85f, 1f));
    }

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Thunderstone", Amount = 5, Recover = false },
        new() { Item = "ElderBark", Amount = 10, Recover = false },
        new() { Item = "Guck", Amount = 5, Recover = false }
    };
}
