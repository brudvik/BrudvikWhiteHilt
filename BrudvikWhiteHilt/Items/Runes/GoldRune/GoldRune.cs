using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes.GoldRune;

/// <summary>
/// Lets a portal carry gold from the Deep North.
/// </summary>
public class GoldRune : WhiteHiltRuneBase
{
    /// <summary>
    /// Constructor for the GoldRune class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public GoldRune(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override int Index => 5;

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltGoldRune";

    /// <inheritdoc/>
    protected override string FullName => "Gold Rune";

    /// <inheritdoc/>
    protected override string Description => "An iron ring carved with runes of the Deep North. Hung on a rune post by a portal, it lets gold through.";

    /// <inheritdoc/>
    protected override string[] UnlockedItems => new[]
    {
        "$item_goldore", "$item_gold"
    };

    /// <inheritdoc/>
    protected override Color BodyColor => new(0.7f, 0.55f, 0.15f);

    /// <inheritdoc/>
    protected override Color RuneColor => new(1f, 0.9f, 0.5f);

    /// <inheritdoc/>
    public override bool Enabled => true;
}
