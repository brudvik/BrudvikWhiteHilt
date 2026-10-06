using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Smithing.RuneForge;

/// <summary>
/// A smithing bench for the portal runes. It can only be built next to a forge.
/// </summary>
public class RuneForge : IWhiteHiltCustomPiece
{
    /// <summary>
    /// Prefab name of the rune forge, used as the crafting station of the runes.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_runeforge";

    private const string FullName = "Rune Forge";
    private const string Description = "A heavy smithing bench for carving runes into iron rings. It must stand next to a forge.";
    private const float Size = 2.2f;

    private static readonly string[] looks = { "New", "Worn", "Broken" };

    private readonly PieceManager instance;
    private readonly BindingStone bindingStone = new();
    private readonly RuneEtchingTable etchingTable = new();

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(PrefabName);

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public RuneForge(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the rune forge to the hammer.
    /// </summary>
    public void Add()
    {
        try
        {
            PieceConfig pieceConfig = new()
            {
                Name = Translations.Token(PrefabName),
                Description = Translations.Token($"{PrefabName}_description"),
                PieceTable = PieceTables.Hammer,
                CraftingStation = CraftingStations.Forge,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "FineWood", Amount = 6, Recover = true },
                    new() { Item = "Iron", Amount = 6, Recover = true },
                    new() { Item = "Stone", Amount = 10, Recover = true }
                }
            };

            CustomPiece piece = new(PrefabName, "piece_workbench", pieceConfig);

            // Recipes match their station by name, so a unique name keeps the workbench recipes off the rune forge.
            CraftingStation station = piece.PiecePrefab.GetComponent<CraftingStation>();
            station.m_name = Translations.Token(PrefabName);
            station.m_craftRequireRoof = false;
            station.m_craftRequireFire = false;
            station.m_showBasicRecipies = false;

            TryApplyVisual(piece, station);
            instance.AddPiece(piece);
            bindingStone.Add(instance, station);
            etchingTable.Add(instance, station);

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private static void TryApplyVisual(CustomPiece piece, CraftingStation station)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            Transform root = piece.PiecePrefab.transform;
            Mesh mesh = ForagingAssets.LoadMesh("runebench");
            Texture2D texture = ForagingAssets.LoadTexture("runebench_albedo");

            // The workbench swaps between a new, worn and broken look as it takes damage, so all three get the bench.
            GameObject newLook = null;
            foreach (string look in looks)
            {
                Transform lookRoot = root.Find(look) ?? throw new InvalidOperationException($"the look {look} was not found");
                GameObject model = VisualHelper.ReplaceMesh(lookRoot.gameObject, mesh, texture, size: Size);
                newLook ??= model;
            }

            VisualHelper.FitBoxColliders(root, newLook);

            PieceFragments.Apply(piece.PiecePrefab);
            Sprite icon = VisualHelper.RenderIcon(piece.PiecePrefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
                station.m_icon = icon;
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }
}
