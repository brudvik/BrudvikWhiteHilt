#nullable enable annotations

namespace BrudvikWhiteHilt.Chests.Constants
{
    /// <summary>
    /// Decides whether the items of the Treasure chest (coins, gems and everything else with a trade value) can become
    /// unlimited, so coins can be made something to work for.
    /// </summary>
    public enum UnlimitedValuablesMode
    {
        /// <summary>Off in <see cref="ChestMode.Linear"/>, on in the other modes.</summary>
        Auto,

        /// <summary>Valuables become unlimited like every other item.</summary>
        On,

        /// <summary>Valuables never become unlimited; full stacks of them stay ordinary items.</summary>
        Off
    }
}
