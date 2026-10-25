using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces;
using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// The Storm Bar as a building piece: a heavy beam resting in the forks of two posts, to stand inside by the door.
/// Put across in a storm, it bars every door and window of the house (<see cref="StormBar"/>).
/// </summary>
public class StormBarPiece : IWhiteHiltCustomPiece
{
    /// <summary>Prefab name of the Storm Bar.</summary>
    public const string PrefabName = "piece_whitehilt_stormbom";

    private const string FullName = "Storm Bar";
    private const string Description = "A heavy bar on two posts, to stand inside by the door. Where the house is homely enough, put it across while a storm is coming or raging: every door and window within reach slams shut and is barred, for those inside as for those outside, and no gust tears them open. Only someone at the bar can lift it; it lifts by itself when the storm has passed.";
    private const string BasePrefab = "wood_pole2";
    private const string PoleMesh = "wood_pole2";
    private const string BeamMesh = "wood_beam";

    // The shape, in metres: the posts stand across x, the beam rests on them.
    private const float HalfWidth = 0.75f;
    private const float PostHeight = 1.25f;
    private const float BeamHeight = 1.1f;

    private readonly PieceManager instance;

    /// <summary>
    /// Creates the definition and registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public StormBarPiece(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <inheritdoc/>
    public bool Enabled => StormSettings.BarEnabled.Value;

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
    /// Adds the Storm Bar to the hammer.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomPiece piece = new(PrefabName, BasePrefab, new PieceConfig
            {
                Name = Translations.Token(PrefabName),
                Description = Translations.Token($"{PrefabName}_description"),
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Misc,
                CraftingStation = CraftingStations.Workbench,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "FineWood", Amount = 8, Recover = true },
                    new() { Item = "RoundLog", Amount = 4, Recover = true },
                    new() { Item = "BronzeNails", Amount = 6, Recover = true }
                }
            });

            GameObject prefab = piece.PiecePrefab;
            StormLeanTo.Strip(prefab);
            GameObject visual = new("New") { layer = prefab.layer };
            visual.transform.SetParent(prefab.transform, false);
            if (!VisualHelper.IsHeadless)
            {
                DefenseModelBuilder.Build(visual.transform, new Dictionary<string, Transform>(), Placements());
            }

            AddCollider(prefab.transform);
            SetUp(prefab, visual, piece.Piece);
            prefab.AddComponent<StormBar>();
            Sprite icon = VisualHelper.RenderIcon(prefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
            }

            instance.AddPiece(piece);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // Two posts, a short crosspiece on each for a fork, and the heavy beam laid across.
    private static List<DefenseModelBuilder.Placement> Placements()
    {
        List<DefenseModelBuilder.Placement> placements = new();
        foreach (float side in new[] { -1f, 1f })
        {
            float x = side * HalfWidth;
            placements.Add(StormLeanTo.Span(PoleMesh, new Vector3(x, 0f, 0f), new Vector3(x, PostHeight, 0f), 1.2f));
            placements.Add(StormLeanTo.Span(PoleMesh, new Vector3(x, BeamHeight - 0.12f, -0.18f), new Vector3(x, BeamHeight - 0.12f, 0.18f), 0.7f));
        }

        placements.Add(StormLeanTo.Span(BeamMesh, new Vector3(-HalfWidth - 0.35f, BeamHeight, 0f), new Vector3(HalfWidth + 0.35f, BeamHeight, 0f), 1.3f));
        return placements;
    }

    private static void AddCollider(Transform root)
    {
        GameObject box = new("collider") { layer = LayerMask.NameToLayer("piece") };
        box.transform.SetParent(root, false);
        box.transform.localPosition = new Vector3(0f, PostHeight / 2f, 0f);
        box.AddComponent<BoxCollider>().size = new Vector3(HalfWidth * 2f + 0.8f, PostHeight, 0.35f);
    }

    private static void SetUp(GameObject prefab, GameObject visual, Piece piece)
    {
        WearNTear wear = prefab.GetComponent<WearNTear>() ?? throw new InvalidOperationException("the wood pole has no WearNTear");
        wear.m_new = visual;
        wear.m_worn = visual;
        wear.m_broken = visual;
        wear.m_wet = null;
        wear.m_snow = null;
        wear.m_snowWorn = null;
        wear.m_snowBroken = null;
        wear.m_fragmentRoots = null;
        wear.m_noSupportWear = true;
        wear.m_supports = false;
        wear.m_health = 400f;
        wear.m_materialType = WearNTear.MaterialType.Wood;
        piece.m_groundPiece = false;
        piece.m_groundOnly = false;
        piece.m_allowedInDungeons = false;
        piece.m_noInWater = true;
        piece.m_comfort = 0;
        piece.m_clipGround = true;
    }
}
