using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.EternalFire;

/// <summary>
/// Surt's Brazier, a workbench extension: an iron brazier holding an ember of Surt. In linear progression, fires across
/// the whole world are eternal while one stands anywhere (see <see cref="EternalFireRules"/>).
/// </summary>
public class SurtsBrazier : IWhiteHiltCustomPiece
{
    /// <summary>
    /// Prefab name of the brazier.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_surtsbrazier";

    private const string FullName = "Surt's Brazier";
    private const string Description = "An iron brazier holding an ember of Surt. While one burns anywhere in the world, every fire, torch, oven and hot tub burns without fuel and stays lit in rain. Place it next to a workbench.";

    // Height in metres; the model is about twice as tall as it is wide.
    private const float Size = 1.3f;
    private const float FlameScale = 0.45f;

    // The top of the coals in the bowl, measured on the converted model (height 1).
    private static readonly Vector3 coalsInMesh = new(0f, 0.85f, 0f);
    private static readonly Color emberGlow = new(1f, 0.55f, 0.2f);

    private readonly PieceManager instance;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.BlackForest;

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
    public SurtsBrazier(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the brazier to the hammer.
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
                    new() { Item = "Stone", Amount = 10, Recover = true },
                    new() { Item = "Bronze", Amount = 4, Recover = true },
                    new() { Item = "SurtlingCore", Amount = 3, Recover = true },
                    new() { Item = "Resin", Amount = 5, Recover = false }
                }
            };

            // The adze extension is small and already extends the workbench.
            CustomPiece piece = new(PrefabName, "piece_workbench_ext3", pieceConfig);
            TryApplyVisual(piece);
            instance.AddPiece(piece);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // Replaces the cloned look with the brazier model, makes its coals glow from the model's emission map, and adds
    // flames on the coals. Should anything be missing, the vanilla look stays.
    private static void TryApplyVisual(CustomPiece piece)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            Transform root = piece.PiecePrefab.transform;
            Transform look = root.Find("new") ?? throw new InvalidOperationException("the extension's look new was not found");
            GameObject model = VisualHelper.ReplaceMesh(look.gameObject, ForagingAssets.LoadMesh("eternalfire"), ForagingAssets.LoadTexture("eternalfire_albedo"), size: Size);
            VisualHelper.FitBoxColliders(root, model);

            Material material = model.GetComponent<MeshRenderer>().sharedMaterial;
            if (material.HasProperty("_EmissionMap"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetTexture("_EmissionMap", ForagingAssets.LoadTexture("eternalfire_emission"));
                material.SetColor("_EmissionColor", emberGlow * 1.5f);
            }

            FireEffects.AddFlames(root, "WhiteHiltSurtsFlame", root.InverseTransformPoint(model.transform.TransformPoint(coalsInMesh)), FlameScale);

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
