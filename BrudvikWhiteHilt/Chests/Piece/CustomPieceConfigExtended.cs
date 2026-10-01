#nullable enable annotations

using BrudvikWhiteHilt.Chests.Constants;
using Jotunn.Configs;

namespace BrudvikWhiteHilt.Chests.Piece
{
    public class CustomPieceConfigExtended : PieceConfig
    {
        public string? PluginName { get; set; }
        public ChestCategory ItemCategory { get; set; } = ChestCategory.None;
    }
}
