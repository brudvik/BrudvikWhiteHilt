using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes.ThunderRune;

/// <summary>
/// An etching rune of iron, silver and crystal, the only one for a shield. Etched with fine wood and rosehips it gives
/// Rowan's Ward: a foe whose blow is blocked is struck by lightning.
/// </summary>
public class ThunderRune : EtchingRuneBase
{
    /// <summary>
    /// Prefab name of the thunder rune.
    /// </summary>
    public const string Name = "WhiteHiltThunderRune";

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public ThunderRune(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Thunder Rune";

    /// <inheritdoc/>
    protected override string Description => "A crystal set in silver and iron, humming like the air before a storm. The rowan saved Thor from the flood; etched into a bound shield with fine wood and red berries, it strikes back with his lightning.";

    /// <inheritdoc/>
    protected override RequirementConfig[] ExtraRequirements => new RequirementConfig[]
    {
        new() { Item = "Silver", Amount = 2, Recover = false },
        new() { Item = "Crystal", Amount = 3, Recover = false }
    };

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    protected override Color BodyColor => new(0.8f, 0.82f, 0.88f);

    /// <inheritdoc/>
    protected override Color RuneColor => new(0.45f, 0.75f, 1f);

    /// <inheritdoc/>
    public override bool Enabled => true;
}
