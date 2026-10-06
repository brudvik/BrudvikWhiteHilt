using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltBow;

/// <summary>
/// The White Hilt Bow, cloned from the vanilla <c>BowHuntsman</c> and the White Hilt model <c>whbow</c> from the asset
/// bundle.
/// </summary>
public class WhiteHiltBow : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltBow(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltBow";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Bow";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Bow of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "BowHuntsman";

    /// <summary>
    /// Bow of the Pack Hunter by Asylum Nox, with a white grip.
    /// </summary>
    protected override string ModelName => "whbow";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 20, Recover = false },
        new() { Item = "Feathers", Amount = 20, Recover = false },
        new() { Item = "BowFineWood", Amount = 1, Recover = false }
    };

    /// <summary>
    /// Lets the limbs bend and the string follow the drawing hand.
    /// </summary>
    /// <param name="model">The bow model under the attach child.</param>
    protected override void OnModelApplied(GameObject model)
    {
        model.AddComponent<WhiteHiltBowFlex>();
    }
}
