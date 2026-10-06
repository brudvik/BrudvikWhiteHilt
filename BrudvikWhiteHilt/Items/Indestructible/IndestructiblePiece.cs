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
    /// Clones a vanilla piece and makes the clone indestructible.
    /// </summary>
    /// <param name="name">Prefab name of the new piece.</param>
    /// <param name="basePrefabName">Vanilla piece to clone.</param>
    /// <param name="itemConfig">Name, description, piece table and requirements.</param>
    public IndestructiblePiece(string name, string basePrefabName, PieceConfig itemConfig) : base(name, basePrefabName, itemConfig)
    {
        var wearNTear = Piece.GetComponent<WearNTear>();
        WearNTearHelper.MakeIndestructible(wearNTear);
    }
}
