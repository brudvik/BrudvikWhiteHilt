using Jotunn.Configs;
using Jotunn.Managers;
using BrudvikWhiteHilt.Progression;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes.BoneRune;

/// <summary>
/// An etching rune of iron and bone. Etched with juniper it gives Seid Smoke: spirit damage, the bane of the dead.
/// </summary>
public class BoneRune : EtchingRuneBase
{
    /// <summary>
    /// Prefab name of the bone rune.
    /// </summary>
    public const string Name = "WhiteHiltBoneRune";

    /// <summary>
    /// Constructor for the BoneRune class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public BoneRune(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Bone Rune";

    /// <inheritdoc/>
    protected override string Description => "An iron ring inlaid with bone and carved with the runes of the seidr. Etched into a bound weapon with juniper, it burns the dead.";

    /// <inheritdoc/>
    protected override RequirementConfig[] ExtraRequirements => new RequirementConfig[]
    {
        new() { Item = "BoneFragments", Amount = 10, Recover = false },
        new() { Item = "Silver", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    protected override Color BodyColor => new(0.85f, 0.82f, 0.72f);

    /// <inheritdoc/>
    protected override Color RuneColor => new(0.55f, 1f, 0.85f);

    /// <inheritdoc/>
    public override bool Enabled => true;
}
