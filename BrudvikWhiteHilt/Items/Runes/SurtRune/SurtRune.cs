using BrudvikWhiteHilt.Monsters;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes.SurtRune;

/// <summary>
/// An etching rune of iron round a Surtstone from a Muspell Giant. Etched with surtling cores and flametal it gives
/// Surt's Wrath: fire damage, and some hits leave a burning pool under the foe.
/// </summary>
public class SurtRune : EtchingRuneBase
{
    /// <summary>
    /// Prefab name of the Surt rune.
    /// </summary>
    public const string Name = "WhiteHiltSurtRune";

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public SurtRune(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Surt Rune";

    /// <inheritdoc/>
    protected override string Description => "An iron ring round a Surtstone that has never stopped burning, carved with the runes of Muspelheim. Etched into a bound weapon with surtling cores and flametal, it sets the ground alight under your foes.";

    /// <inheritdoc/>
    protected override RequirementConfig[] ExtraRequirements => new RequirementConfig[]
    {
        new() { Item = MuspellRegistry.SurtstoneName, Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Ashlands;

    /// <inheritdoc/>
    protected override Color BodyColor => new(0.2f, 0.16f, 0.15f);

    /// <inheritdoc/>
    protected override Color RuneColor => new(1f, 0.42f, 0.08f);

    /// <inheritdoc/>
    public override bool Enabled => true;
}
