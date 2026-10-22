using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes.RimeRune;

/// <summary>
/// An etching rune of iron round a drake's freeze gland and a dragon's tear. Etched with freeze glands and crystal it
/// gives Niflheim's Rime: strong frost damage, the bane of the fire-born of the Ashlands, and some hits freeze the foe
/// fast for a moment.
/// </summary>
public class RimeRune : EtchingRuneBase
{
    /// <summary>
    /// Prefab name of the Rime rune.
    /// </summary>
    public const string Name = "WhiteHiltRimeRune";

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public RimeRune(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Rime Rune";

    /// <inheritdoc/>
    protected override string Description => "An iron ring round a dragon's tear, white with a frost that never melts, carved with the runes of Niflheim. Etched into a bound weapon with freeze glands and crystal, it bites the fire-born of the Ashlands and now and then freezes them fast.";

    /// <inheritdoc/>
    protected override RequirementConfig[] ExtraRequirements => new RequirementConfig[]
    {
        new() { Item = "FreezeGland", Amount = 5, Recover = false },
        new() { Item = "DragonTear", Amount = 1, Recover = false }
    };

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Ashlands;

    /// <inheritdoc/>
    protected override Color BodyColor => new(0.78f, 0.86f, 0.92f);

    /// <inheritdoc/>
    protected override Color RuneColor => new(0.35f, 0.75f, 1f);

    /// <inheritdoc/>
    public override bool Enabled => true;
}
