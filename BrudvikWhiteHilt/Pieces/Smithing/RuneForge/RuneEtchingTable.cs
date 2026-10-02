using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;

namespace BrudvikWhiteHilt.Pieces.Smithing.RuneForge;

/// <summary>
/// A <see cref="RuneForge"/> extension: the game's rune table, where a rune is etched into a trophy-bound White Hilt
/// weapon to give it fire, frost, poison, lightning, a web or the grip of the deep.
/// </summary>
public class RuneEtchingTable
{
    /// <summary>
    /// Prefab name of the rune etching table.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_runeetchingtable";

    private const string FullName = "Rune Etching Table";
    private const string Description = "A table for etching a rune into a White Hilt weapon that has a black trophy bound to it. Each rune, with its materials, gives the weapon its own power. Place it next to the Rune Forge.";

    /// <summary>
    /// Constructor for the RuneEtchingTable class. Registers the English text.
    /// </summary>
    public RuneEtchingTable()
    {
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
        RuneEtchingTableComponent.RegisterTranslations();
    }

    /// <summary>
    /// Adds the rune etching table to the hammer.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    /// <param name="runeForge">The crafting station of the rune forge.</param>
    public void Add(PieceManager instance, CraftingStation runeForge)
    {
        try
        {
            PieceConfig pieceConfig = new()
            {
                Name = Translations.Token(PrefabName),
                Description = Translations.Token($"{PrefabName}_description"),
                PieceTable = PieceTables.Hammer,
                CraftingStation = RuneForge.PrefabName,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "FineWood", Amount = 10, Recover = true },
                    new() { Item = "Iron", Amount = 4, Recover = true },
                    new() { Item = "Resin", Amount = 6, Recover = false }
                }
            };

            // The Galdr table's rune table keeps its own look; only its station changes.
            CustomPiece piece = new(PrefabName, "piece_magetable_ext", pieceConfig);
            piece.PiecePrefab.GetComponent<StationExtension>().m_craftingStation = runeForge;
            piece.PiecePrefab.AddComponent<RuneEtchingTableComponent>();
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
