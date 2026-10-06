using System;

namespace BrudvikWhiteHilt.Items.Runes;

/// <summary>
/// A rune smithed only to be etched into a trophy-bound weapon at the Rune Etching Table. It has no place on the rune
/// post and lets nothing through portals.
/// </summary>
public abstract class EtchingRuneBase : WhiteHiltRuneBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    protected EtchingRuneBase(Jotunn.Managers.ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    public override int Index => -1;

    /// <inheritdoc/>
    protected override string[] UnlockedItems => Array.Empty<string>();

    /// <inheritdoc/>
    protected override int IronCost => 4;
}
