using BrudvikWhiteHilt.Pieces.Ships.Ballista;
using BrudvikWhiteHilt.SeaBattles.Weapons;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.ShipUpgrades;

/// <summary>
/// The Chain Shot: two iron balls on a chain, for the Ship Ballista. A hit on a ship tears its sail, so it sails with less
/// force for a while (see <see cref="ShotHits"/> and <c>ShipDamage</c>); it does little damage to the hull.
/// </summary>
public class ChainShot : ShipAmmoBase
{
    /// <summary>
    /// Prefab name of the projectile.
    /// </summary>
    public const string ProjectilePrefab = "WhiteHilt_ChainShot_projectile";

    // Dark iron.
    private static readonly Color iron = new(0.3f, 0.3f, 0.33f);

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public ChainShot(ItemManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    public override string Id => BallistaSettings.ChainShotName;

    /// <inheritdoc/>
    protected override string FullName => "Chain Shot";

    /// <inheritdoc/>
    protected override string ProjectileName => ProjectilePrefab;

    /// <inheritdoc/>
    protected override ShotKind Kind => ShotKind.ChainShot;

    /// <inheritdoc/>
    protected override Color Tint => iron;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Chain", Amount = 1, Recover = false },
        new() { Item = "Iron", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Damage => BallistaSettings.ChainShotDamage.Value;

    /// <inheritdoc/>
    protected override int AmountPerCraft => BallistaSettings.ChainShotsPerCraft.Value;
}
