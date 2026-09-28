using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes.BlackMetalRune;

/// <summary>
/// Lets a portal carry black metal.
/// </summary>
public class BlackMetalRune : WhiteHiltRuneBase
{
    /// <summary>
    /// Constructor for the BlackMetalRune class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public BlackMetalRune(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override int Index => 3;

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltBlackMetalRune";

    /// <inheritdoc/>
    protected override string FullName => "Black Metal Rune";

    /// <inheritdoc/>
    protected override string Description => "An iron ring carved with runes of the Plains. Hung on a rune post by a portal, it lets black metal through.";

    /// <inheritdoc/>
    protected override string[] UnlockedItems => new[]
    {
        "$item_blackmetalscrap", "$item_blackmetal"
    };

    /// <inheritdoc/>
    protected override Color BodyColor => new(0.1f, 0.1f, 0.12f);

    /// <inheritdoc/>
    protected override Color RuneColor => new(0.6f, 0.9f, 0.45f);

    /// <inheritdoc/>
    public override bool Enabled => true;
}
