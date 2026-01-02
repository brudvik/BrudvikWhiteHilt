using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;

namespace BrudvikWhiteHilt.Items.Indestructible;

/// <summary>
/// This class defines an indestructible piece.
/// </summary>
public class IndestructiblePiece : CustomPiece
{
    /// <summary>
    /// Constructor for the IndestructiblePiece class.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="basePrefabName"></param>
    /// <param name="itemConfig"></param>
    public IndestructiblePiece(string name, string basePrefabName, PieceConfig itemConfig) : base(name, basePrefabName, itemConfig)
    {
        var wearNTear = Piece.GetComponent<WearNTear>();
        WearNTearHelper.MakeIndestructible(wearNTear);
    }
}
