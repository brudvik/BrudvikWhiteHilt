using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Patches.Crafting;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Smithing.ChainBench;

/// <summary>
/// A forge extension: a stump with a smith's vise and hanging chains. While it stands next to the forge,
/// the forge can make chains from iron.
/// </summary>
public class ChainBench : IWhiteHiltCustomPiece
{
    /// <summary>
    /// Prefab name of the chain bench.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_chainbench";

    /// <summary>
    /// Name of the forge recipe for chains that needs the chain bench.
    /// </summary>
    public const string ChainRecipeName = "Recipe_WhiteHiltChain";

    private const string FullName = "Chain Bench";
    private const string Description = "A stump with a smith's vise for bending and closing chain links. Place it next to the forge to make chains from iron.";

    private readonly PieceManager instance;

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
    /// Constructor for the ChainBench class. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public ChainBench(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the chain bench to the hammer and the chain recipe to the forge.
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
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Wood", Amount = 10, Recover = true },
                    new() { Item = "Iron", Amount = 4, Recover = true },
                    new() { Item = "Stone", Amount = 4, Recover = true }
                }
            };

            // The anvil extension is about the same size as the vise and already extends the forge.
            CustomPiece piece = new(PrefabName, "forge_ext2", pieceConfig);
            TryApplyVisual(piece);
            instance.AddPiece(piece);

            ItemManager.Instance.AddRecipe(new CustomRecipe(new RecipeConfig
            {
                Name = ChainRecipeName,
                Item = "Chain",
                Amount = 1,
                CraftingStation = CraftingStations.Forge,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Iron", Amount = 2 },
                    new() { Item = "Coal", Amount = 1 }
                }
            }));
            ChainBenchRecipePatch.Register(ChainRecipeName, PrefabName);

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private static void TryApplyVisual(CustomPiece piece)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            Transform anvil = piece.PiecePrefab.transform.Find("new") ?? throw new InvalidOperationException("the anvil new was not found");
            GameObject vise = VisualHelper.ReplaceMesh(anvil.gameObject, ForagingAssets.LoadMesh("chainvise"), ForagingAssets.LoadTexture("chainvise_albedo"));

            // Unity mirrors x when importing OBJ, so the stump sits on +x. The chains hang on its front, turned to face out,
            // and are lightened from the model's near-black to iron grey.
            // The stump's front face spans x -0.01..0.30 at z 0.23 and its top is at 0.75; the chains fit inside that.
            Texture2D chains = VisualHelper.RecolorTexture(ForagingAssets.LoadTexture("chains_albedo"), _ => new Color32(95, 95, 100, 255));
            VisualHelper.AddMesh(vise, ForagingAssets.LoadMesh("chains"), chains, new Vector3(0.145f, 0.32f, 0.245f), 0.4f, Quaternion.Euler(0f, 90f, 0f));

            Sprite icon = VisualHelper.RenderIcon(piece.PiecePrefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }
}
