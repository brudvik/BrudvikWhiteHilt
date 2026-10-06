#nullable enable annotations

using BrudvikWhiteHilt.Chests.Constants;
using Jotunn.Configs;

namespace BrudvikWhiteHilt.Chests.Piece
{
    /// <summary>
    /// Jotunn's piece config with what a restocking chest needs besides: the category it holds and where its embedded
    /// resources are.
    /// </summary>
    public class CustomPieceConfigExtended : PieceConfig
    {
        /// <summary>
        /// The root namespace of the embedded resources, e.g. the chest icons, which is the start of their resource
        /// names; null when the icon paths are given in full.
        /// </summary>
        public string? PluginName { get; set; }

        /// <summary>
        /// The item category the chest is filled with; <see cref="ChestCategory.None"/> for a chest that only restocks
        /// what players put in.
        /// </summary>
        public ChestCategory ItemCategory { get; set; } = ChestCategory.None;
    }
}
