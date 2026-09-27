using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;

namespace BrudvikWhiteHilt.Pieces.Cooking.StonePot;

/// <summary>
/// An extension for the <see cref="StonePot"/>. Placed next to the pot, it raises the pot to level 2.
/// </summary>
public class HerbRack
{
    /// <summary>
    /// Prefab name of the herb rack.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_herbrack";

    private const string FullName = "Herb Rack";
    private const string Description = "Dried herbs and spices within reach of the Stone Pot. Place it next to the pot to cook the more demanding dishes.";

    /// <summary>
    /// Constructor for the HerbRack class. Registers the English text.
    /// </summary>
    public HerbRack()
    {
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the herb rack to the hammer.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    /// <param name="stonePot">The crafting station of the stone pot.</param>
    public void Add(PieceManager instance, CraftingStation stonePot)
    {
        try
        {
            PieceConfig pieceConfig = new()
            {
                Name = Translations.Token(PrefabName),
                Description = Translations.Token($"{PrefabName}_description"),
                PieceTable = PieceTables.Hammer,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "FineWood", Amount = 4, Recover = true },
                    new() { Item = "Bronze", Amount = 2, Recover = true },
                    new() { Item = "Thistle", Amount = 3, Recover = false }
                }
            };

            CustomPiece piece = new(PrefabName, "cauldron_ext1_spice", pieceConfig);
            piece.PiecePrefab.GetComponent<StationExtension>().m_craftingStation = stonePot;
            instance.AddPiece(piece);

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }
}
