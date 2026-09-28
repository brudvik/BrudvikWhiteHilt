using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Runes.IronRune;

/// <summary>
/// Lets a portal carry iron.
/// </summary>
public class IronRune : WhiteHiltRuneBase
{
    /// <summary>
    /// Constructor for the IronRune class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public IronRune(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override int Index => 1;

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltIronRune";

    /// <inheritdoc/>
    protected override string FullName => "Iron Rune";

    /// <inheritdoc/>
    protected override string Description => "An iron ring carved with runes of the Swamp. Hung on a rune post by a portal, it lets iron through.";

    /// <inheritdoc/>
    protected override string[] UnlockedItems => new[]
    {
        "$item_ironore", "$item_iron", "$item_ironscrap", "$item_ironpit"
    };

    /// <inheritdoc/>
    protected override Color BodyColor => new(0.35f, 0.35f, 0.37f);

    /// <inheritdoc/>
    protected override Color RuneColor => new(0.8f, 0.8f, 0.85f);

    /// <inheritdoc/>
    public override bool Enabled => true;
}
