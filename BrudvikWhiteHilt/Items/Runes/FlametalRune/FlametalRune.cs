using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes.FlametalRune;

/// <summary>
/// Lets a portal carry flametal, both the Ashlands and the legacy kind.
/// </summary>
public class FlametalRune : WhiteHiltRuneBase
{
    /// <summary>
    /// Constructor for the FlametalRune class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public FlametalRune(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override int Index => 4;

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltFlametalRune";

    /// <inheritdoc/>
    protected override string FullName => "Flametal Rune";

    /// <inheritdoc/>
    protected override string Description => "An iron ring carved with runes of the Ashlands. Hung on a rune post by a portal, it lets flametal through.";

    /// <inheritdoc/>
    protected override string[] UnlockedItems => new[]
    {
        "$item_flametalore", "$item_flametal", "$item_flametalore_old", "$item_flametal_old"
    };

    /// <inheritdoc/>
    protected override Color BodyColor => new(0.35f, 0.12f, 0.05f);

    /// <inheritdoc/>
    protected override Color RuneColor => new(1f, 0.45f, 0.1f);

    /// <inheritdoc/>
    public override bool Enabled => true;
}
