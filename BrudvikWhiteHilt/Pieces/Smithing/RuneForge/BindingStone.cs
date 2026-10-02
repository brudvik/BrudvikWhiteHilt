using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Portals.ValkyrieStone;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Smithing.RuneForge;

/// <summary>
/// A <see cref="RuneForge"/> extension: a blood-red runestone where black beast trophies are bound to White Hilt
/// weapons and shields.
/// </summary>
public class BindingStone
{
    /// <summary>
    /// Prefab name of the binding stone.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_bindingstone";

    private const string FullName = "Binding Stone";
    private const string Description = "A runestone where the trophy of a black beast is bound to a White Hilt weapon or shield, which then strikes or blocks harder. Place it next to the Rune Forge.";

    // bindingstone.glb in metres: 1.46 high from the bottom of its footing stone, which reaches 0.04 below the ground.
    // The carved side with the Surtling Core faces +z.
    private const float ModelHeight = 1.46f;
    private const float Sink = 0.04f;
    private const float GlowStrength = 1.6f;

    private static readonly Vector3 colliderSize = new(1f, 1.42f, 0.46f);
    private static readonly Color lightColor = new(1f, 0.25f, 0.1f);

    /// <summary>
    /// Constructor for the BindingStone class. Registers the English text.
    /// </summary>
    public BindingStone()
    {
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
        Translations.AddEnglish("whitehilt_runeforge_ext_noforge", "Needs a Rune Forge nearby");
        BindingStoneComponent.RegisterTranslations();
    }

    /// <summary>
    /// Adds the binding stone to the hammer.
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
                    new() { Item = "Stone", Amount = 20, Recover = true },
                    new() { Item = "Chain", Amount = 2, Recover = true },
                    new() { Item = "SurtlingCore", Amount = 1, Recover = false }
                }
            };

            // The forge cooler is a forge extension with no other parts; it gets stone behaviour and our look below.
            CustomPiece piece = new(PrefabName, "forge_ext5", pieceConfig);
            piece.PiecePrefab.GetComponent<StationExtension>().m_craftingStation = runeForge;
            piece.PiecePrefab.AddComponent<BindingStoneComponent>();
            ValkyrieStone.MakeStone(piece);
            ReplaceCollider(piece.PiecePrefab);
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

    // The cooler's mesh collider has the shape of the cooling bath; one box round the stone replaces it, on the server too.
    private static void ReplaceCollider(GameObject prefab)
    {
        int layer = prefab.layer;
        foreach (MeshCollider meshCollider in prefab.GetComponentsInChildren<MeshCollider>(true))
        {
            layer = meshCollider.gameObject.layer;
            UnityEngine.Object.DestroyImmediate(meshCollider);
        }

        GameObject holder = new("collider") { layer = layer };
        holder.transform.SetParent(prefab.transform, false);
        BoxCollider box = holder.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, colliderSize.y / 2f, 0f);
        box.size = colliderSize;
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
            // The cooler's material is metallic with its own metal map, so the table top's plain piece material is used instead.
            Renderer template = PrefabManager.Instance.GetPrefab("piece_table")?.transform.Find("new/high")?.GetComponent<MeshRenderer>()
                ?? throw new InvalidOperationException("the table's renderer new/high was not found");

            Mesh mesh = ForagingAssets.LoadMesh("bindingstone");
            float scale = ModelHeight / mesh.bounds.size.y;
            Vector3 pivot = -new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z) * scale + Vector3.down * Sink;
            GameObject stone = VisualHelper.CreateModel(prefab.transform, mesh, ForagingAssets.LoadTexture("bindingstone_albedo"), template, pivot, Quaternion.identity, scale);

            Material material = stone.GetComponent<MeshRenderer>().sharedMaterial;
            if (material.HasProperty("_EmissionMap"))
            {
                // The emission map already holds the red of the runes and the orange of the core.
                material.EnableKeyword("_EMISSION");
                material.SetTexture("_EmissionMap", ForagingAssets.LoadTexture("bindingstone_emission"));
                material.SetColor("_EmissionColor", Color.white * GlowStrength);
            }

            AddLight(prefab.transform);

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

    private static void AddLight(Transform root)
    {
        GameObject glow = new("BindingGlow");
        glow.transform.SetParent(root, false);
        glow.transform.localPosition = new Vector3(0f, 0.85f, 0.5f);

        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = lightColor;
        light.range = 2.5f;
        light.intensity = 0.6f;
        light.shadows = LightShadows.None;
    }
}
