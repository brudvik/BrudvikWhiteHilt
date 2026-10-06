using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes.MireRune;

/// <summary>
/// An etching rune of iron, copper and tar. Etched with peat and tar it gives Mire's Hold: hits that leave the target
/// tarred and slow.
/// </summary>
public class MireRune : EtchingRuneBase
{
    /// <summary>
    /// Prefab name of the mire rune.
    /// </summary>
    public const string Name = "WhiteHiltMireRune";

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public MireRune(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Mire Rune";

    /// <inheritdoc/>
    protected override string Description => "An iron and copper ring dipped in tar, carved with the runes of the bog. Etched into a bound weapon with peat and tar, it makes foes stick fast.";

    /// <inheritdoc/>
    protected override RequirementConfig[] ExtraRequirements => new RequirementConfig[]
    {
        new() { Item = "Copper", Amount = 3, Recover = false },
        new() { Item = "Tar", Amount = 2, Recover = false }
    };

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    protected override Color BodyColor => new(0.45f, 0.28f, 0.16f);

    /// <inheritdoc/>
    protected override Color RuneColor => new(0.15f, 0.12f, 0.1f);

    /// <inheritdoc/>
    public override bool Enabled => true;
}
