using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes.ObsidianRune;

/// <summary>
/// An etching rune of iron and obsidian. Etched with ergot it gives Dread: hits that send foes running.
/// </summary>
public class ObsidianRune : EtchingRuneBase
{
    /// <summary>
    /// Prefab name of the obsidian rune.
    /// </summary>
    public const string Name = "WhiteHiltObsidianRune";

    /// <summary>
    /// Constructor for the ObsidianRune class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public ObsidianRune(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Obsidian Rune";

    /// <inheritdoc/>
    protected override string Description => "A black glass shard bound in iron, carved with the runes of nightmares. Etched into a bound weapon with ergot, it fills foes with dread.";

    /// <inheritdoc/>
    protected override RequirementConfig[] ExtraRequirements => new RequirementConfig[]
    {
        new() { Item = "Obsidian", Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    protected override Color BodyColor => new(0.12f, 0.1f, 0.14f);

    /// <inheritdoc/>
    protected override Color RuneColor => new(0.6f, 0.2f, 0.8f);

    /// <inheritdoc/>
    public override bool Enabled => true;
}
