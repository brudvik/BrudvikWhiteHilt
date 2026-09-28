using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes.BronzeRune;

/// <summary>
/// Lets a portal carry copper, tin and bronze.
/// </summary>
public class BronzeRune : WhiteHiltRuneBase
{
    /// <summary>
    /// Constructor for the BronzeRune class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public BronzeRune(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override int Index => 0;

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltBronzeRune";

    /// <inheritdoc/>
    protected override string FullName => "Bronze Rune";

    /// <inheritdoc/>
    protected override string Description => "An iron ring carved with runes of the Black Forest. Hung on a rune post by a portal, it lets copper, tin and bronze through.";

    /// <inheritdoc/>
    protected override string[] UnlockedItems => new[]
    {
        "$item_copperore", "$item_copper", "$item_copperscrap", "$item_tinore", "$item_tin", "$item_bronze", "$item_bronzescrap"
    };

    /// <inheritdoc/>
    protected override Color BodyColor => new(0.55f, 0.33f, 0.18f);

    /// <inheritdoc/>
    protected override Color RuneColor => new(1f, 0.7f, 0.35f);

    /// <inheritdoc/>
    public override bool Enabled => true;
}
