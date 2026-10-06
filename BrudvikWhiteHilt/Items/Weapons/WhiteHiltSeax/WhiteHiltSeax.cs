using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltSeax;

/// <summary>
/// The White Hilt Seax, a knife cloned from the vanilla <c>KnifeChitin</c> with the White Hilt model <c>whseax</c>. It
/// cuts deeper than the White Hilt Knife and makes its target bleed (<see cref="BleedEffect"/>).
/// </summary>
public class WhiteHiltSeax : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltSeax(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltSeax";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Seax";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Seax of Dyrnwyn. Its wounds bleed.";

    /// <inheritdoc/>
    protected override string CopyFrom => "KnifeChitin";

    /// <summary>
    /// Viking Seax by Bram@NC, with a white grip.
    /// </summary>
    protected override string ModelName => "whseax";

    /// <inheritdoc/>
    protected override WeaponTrait Trait => new() { Bleeds = true };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 10, Recover = false },
        new() { Item = "Bloodbag", Amount = 5, Recover = false },
        new() { Item = "KnifeCopper", Amount = 1, Recover = false }
    };

    /// <summary>
    /// Cuts a little deeper than the Abyssal Razor, more slash than pierce.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_damages = new HitData.DamageTypes { m_slash = 25f, m_pierce = 15f };
        shared.m_damagesPerLevel = new HitData.DamageTypes { m_slash = 3f, m_pierce = 2f };
    }
}
