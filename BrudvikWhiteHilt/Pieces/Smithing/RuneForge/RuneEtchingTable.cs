using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Smithing.RuneForge;

/// <summary>
/// A <see cref="RuneForge"/> extension: a carver's table where a rune is etched into a trophy-bound White Hilt weapon
/// to
/// give it fire, frost, poison, lightning, a web or the grip of the deep.
/// </summary>
public class RuneEtchingTable
{
    /// <summary>
    /// Prefab name of the rune etching table.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_runeetchingtable";

    private const string FullName = "Rune Etching Table";
    private const string Description = "A table for etching a rune into a White Hilt weapon that has a black trophy bound to it. Each rune, with its materials, gives the weapon its own power. Place it next to the Rune Forge.";

    // runeetchingtable.glb in metres: the table top is at 0.82, the mallet lying on it reaches 0.904.
    private const float ModelHeight = 0.904f;

    private static readonly Vector3 colliderSize = new(1.15f, 0.86f, 0.62f);

    /// <summary>
    /// Creates the extension and registers its English text. The <see cref="RuneForge"/> creates it and adds it
    /// to the game together with itself, as it only works next to it.
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

            CustomPiece piece = new(PrefabName, "piece_magetable_ext", pieceConfig);
            GameObject prefab = piece.PiecePrefab;
            prefab.GetComponent<StationExtension>().m_craftingStation = runeForge;
            prefab.AddComponent<RuneEtchingTableComponent>();

            // Without this the table would break into the pieces of the Galdr rune table.
            WearNTear wearNTear = prefab.GetComponent<WearNTear>();
            if (wearNTear != null)
            {
                wearNTear.m_fragmentRoots = Array.Empty<GameObject>();
            }

            FitColliders(prefab.transform);
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

    // One box round the table, on the server as well, so it blocks and can be hit like it looks.
    private static void FitColliders(Transform root)
    {
        foreach (BoxCollider collider in root.GetComponentsInChildren<BoxCollider>(true))
        {
            if (collider.transform.parent != root)
            {
                continue;
            }

            collider.transform.localPosition = Vector3.zero;
            collider.transform.localRotation = Quaternion.identity;
            collider.transform.localScale = Vector3.one;
            collider.center = new Vector3(0f, colliderSize.y / 2f, 0f);
            collider.size = colliderSize;
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
            GameObject prefab = piece.PiecePrefab;
            VisualHelper.HideRenderers(prefab);
            Renderer template = PrefabManager.Instance.GetPrefab("piece_table")?.transform.Find("new/high")?.GetComponent<MeshRenderer>()
                ?? throw new InvalidOperationException("the table's renderer new/high was not found");

            Mesh mesh = ForagingAssets.LoadMesh("runeetchingtable");
            float scale = ModelHeight / mesh.bounds.size.y;
            Vector3 pivot = -new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z) * scale;
            VisualHelper.CreateModel(prefab.transform, mesh, ForagingAssets.LoadTexture("runeetchingtable_albedo"), template, pivot, Quaternion.identity, scale);

            PieceFragments.Apply(prefab);
            Sprite icon = VisualHelper.RenderIcon(prefab);
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
