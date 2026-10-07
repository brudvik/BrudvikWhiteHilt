using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltFalchion;

/// <summary>
/// The White Hilt Falchion, a broad, curved sword cloned from the vanilla <c>SwordSilver</c> with slash in place of its
/// spirit. Its blows sweep half again as wide, with the White Hilt model <c>whfalchion</c>. In linear progression it
/// unlocks with the Mountain tier.
/// </summary>
public class WhiteHiltFalchion : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltFalchion(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltFalchion";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Falchion";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Falchion of Dyrnwyn. Its broad blade sweeps wide.";

    /// <inheritdoc/>
    protected override string CopyFrom => "SwordSilver";

    /// <summary>
    /// Falchion by Whatevvs, with a white grip.
    /// </summary>
    protected override string ModelName => "whfalchion";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Silver", Amount = 20, Recover = false },
        new() { Item = "WolfPelt", Amount = 5, Recover = false },
        new() { Item = "Obsidian", Amount = 3, Recover = false }
    };

    /// <summary>
    /// Pure slash at Mountain strength, and a sweep half again as wide.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_damages = new HitData.DamageTypes { m_slash = 80f };
        shared.m_damagesPerLevel = new HitData.DamageTypes { m_slash = 5f };
        shared.m_attack.m_attackAngle *= 1.5f;
    }
}
