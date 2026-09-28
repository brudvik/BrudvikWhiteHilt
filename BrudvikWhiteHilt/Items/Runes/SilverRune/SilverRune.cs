using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes.SilverRune;

/// <summary>
/// Lets a portal carry silver.
/// </summary>
public class SilverRune : WhiteHiltRuneBase
{
    /// <summary>
    /// Constructor for the SilverRune class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public SilverRune(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override int Index => 2;

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltSilverRune";

    /// <inheritdoc/>
    protected override string FullName => "Silver Rune";

    /// <inheritdoc/>
    protected override string Description => "An iron ring carved with runes of the Mountains. Hung on a rune post by a portal, it lets silver through.";

    /// <inheritdoc/>
    protected override string[] UnlockedItems => new[]
    {
        "$item_silverore", "$item_silver"
    };

    /// <inheritdoc/>
    protected override Color BodyColor => new(0.75f, 0.77f, 0.8f);

    /// <inheritdoc/>
    protected override Color RuneColor => new(0.55f, 0.8f, 1f);

    /// <inheritdoc/>
    public override bool Enabled => true;
}
