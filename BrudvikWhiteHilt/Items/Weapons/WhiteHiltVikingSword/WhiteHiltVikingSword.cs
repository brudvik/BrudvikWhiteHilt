using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltVikingSword;

/// <summary>
/// The White Hilt Viking Sword, a one-handed sword cloned from the vanilla <c>SwordIron</c>. The last blow of a run of
/// three strikes three times as hard instead of twice, with the White Hilt model <c>whvikingsword</c>.
/// </summary>
public class WhiteHiltVikingSword : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltVikingSword(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltVikingSword";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Viking Sword";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Viking Sword of Dyrnwyn. The last blow of a run strikes hardest.";

    /// <inheritdoc/>
    protected override string CopyFrom => "SwordIron";

    /// <summary>
    /// Viking sword by Shannen Art, with a white grip.
    /// </summary>
    protected override string ModelName => "whvikingsword";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 20, Recover = false },
        new() { Item = "Guck", Amount = 3, Recover = false },
        new() { Item = "LeatherScraps", Amount = 5, Recover = false }
    };

    /// <summary>
    /// The last blow of a run strikes three times as hard.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_attack.m_lastChainDamageMultiplier = 3f;
    }
}
