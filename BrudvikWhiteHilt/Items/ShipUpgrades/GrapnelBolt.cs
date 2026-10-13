using BrudvikWhiteHilt.Pieces.Ships.Ballista;
using BrudvikWhiteHilt.SeaBattles.Weapons;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.ShipUpgrades;

/// <summary>
/// The Grapnel Bolt: a hooked bolt on a rope, for the Ship Ballista. A hit on another ship binds it to the ship the bolt was
/// shot from and pulls the two together (see <see cref="ShotHits"/> and <see cref="GrappleLink"/>), so the crew can board. A
/// Nail Ship that is bound cannot flee.
/// </summary>
public class GrapnelBolt : ShipAmmoBase
{
    /// <summary>
    /// Prefab name of the projectile.
    /// </summary>
    public const string ProjectilePrefab = "WhiteHilt_Grapnel_projectile";

    // Hemp rope over the iron hook.
    private static readonly Color rope = new(0.62f, 0.5f, 0.32f);

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public GrapnelBolt(ItemManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    public override string Id => BallistaSettings.GrapnelBoltName;

    /// <inheritdoc/>
    protected override string FullName => "Grapnel Bolt";

    /// <inheritdoc/>
    protected override string ProjectileName => ProjectilePrefab;

    /// <inheritdoc/>
    protected override ShotKind Kind => ShotKind.Grapnel;

    /// <inheritdoc/>
    protected override Color Tint => rope;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 2, Recover = false },
        new() { Item = "BogIron", Amount = 1, Recover = false },
        new() { Item = "DeerHide", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    protected override float Damage => BallistaSettings.GrapnelDamage.Value;

    /// <inheritdoc/>
    protected override int AmountPerCraft => BallistaSettings.GrapnelsPerCraft.Value;
}
