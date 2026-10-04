using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Roofing;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Lights;

/// <summary>
/// The Soapstone Lamp: a small carved bowl of soapstone with a wick in resin, as the Norse lit their houses. Smaller and
/// cosier than a torch, for tables and shelves. Carved near a Stonecutter.
/// </summary>
public class SoapstoneLamp : IWhiteHiltCustomPiece
{
    /// <summary>
    /// Prefab name of the lamp.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_soapstonelamp";

    private const string FullName = "Soapstone Lamp";
    private const string Description = "A small bowl carved out of soapstone, with a wick burning in resin. A soft, cosy light for tables and shelves; feed it resin.";

    // The bowl: the Stone Pot's model, 0.3 m across and 0.12 m high (the model is 2.04 m across and 1 m high).
    private const float Width = 0.3f;
    private const float Height = 0.12f;
    private const float ModelWidth = 2.04f;
    private const float FlameLift = 0.03f;
    private const float LightScale = 0.55f;
    private const float FuelSeconds = 3f;

    private readonly PieceManager instance;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(PrefabName);

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Constructor for the SoapstoneLamp class. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public SoapstoneLamp(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the lamp to the hammer.
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
                Category = PieceCategories.Furniture,
                CraftingStation = CraftingStations.Stonecutter,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = RoofMaterials.Soapstone, Amount = 2, Recover = true },
                    new() { Item = "Resin", Amount = 2, Recover = false }
                }
            };

            CustomPiece piece = new(PrefabName, "piece_groundtorch_wood", pieceConfig);
            Fireplace fireplace = piece.PiecePrefab.GetComponent<Fireplace>();
            fireplace.m_name = Translations.Token(PrefabName);
            fireplace.m_secPerFuel *= FuelSeconds;

            // A lamp stands on tables and shelves as well as on the floor.
            piece.Piece.m_groundOnly = false;
            piece.Piece.m_groundPiece = false;
            piece.Piece.m_noInWater = true;

            ReplaceColliders(piece.PiecePrefab);
            TryApplyVisual(piece, fireplace);
            instance.AddPiece(piece);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // The torch's colliders are a tall pole; the lamp is a small bowl. Done on the server too, so both agree.
    private static void ReplaceColliders(GameObject prefab)
    {
        foreach (Collider collider in prefab.GetComponentsInChildren<Collider>(true).Where(collider => !collider.isTrigger))
        {
            UnityEngine.Object.DestroyImmediate(collider);
        }

        GameObject solid = new("collider") { layer = LayerMask.NameToLayer("piece") };
        solid.transform.SetParent(prefab.transform, false);
        BoxCollider box = solid.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, Height / 2f, 0f);
        box.size = new Vector3(Width, Height, Width);
    }

    private static void TryApplyVisual(CustomPiece piece, Fireplace fireplace)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            Transform root = piece.PiecePrefab.transform;
            List<Transform> flames = new[] { fireplace.m_enabledObject, fireplace.m_enabledObjectLow, fireplace.m_enabledObjectHigh }
                .Where(flame => flame != null).Select(flame => flame.transform).Distinct().ToList();

            // The torch's own wood and the top it burns from.
            MeshRenderer[] torch = root.GetComponentsInChildren<MeshRenderer>(true)
                .Where(renderer => !flames.Any(flame => renderer.transform.IsChildOf(flame)) && renderer.GetComponent<MeshFilter>()?.sharedMesh != null)
                .ToArray();
            if (torch.Length == 0)
            {
                throw new InvalidOperationException("the torch has no mesh");
            }

            float torchTop = torch.Max(renderer => Top(root, renderer));
            foreach (MeshRenderer renderer in torch)
            {
                renderer.enabled = false;
            }

            Mesh mesh = ForagingAssets.LoadMesh("stonepot");
            GameObject bowl = VisualHelper.CreateModel(root, mesh, ForagingAssets.LoadTexture("stonepot_albedo"), torch[0], Vector3.zero, Quaternion.identity, Width / ModelWidth);
            bowl.transform.localScale = new Vector3(bowl.transform.localScale.x, Height, bowl.transform.localScale.z);
            VisualHelper.Recolor(bowl, pixel =>
            {
                Color.RGBToHSV(pixel, out _, out _, out float value);
                Color result = Color.Lerp(new Color(value, value, value), new Color(0.58f, 0.66f, 0.6f) * (0.5f + value * 0.7f), 0.75f);
                result.a = pixel.a / 255f;
                return result;
            });

            // The flame, its light and its smoke move down from the torch head to the wick.
            Vector3 drop = Vector3.up * (Height + FlameLift - torchTop);
            foreach (Transform flame in flames)
            {
                flame.localPosition += drop;
                flame.localScale *= 0.6f;
                foreach (Light light in flame.GetComponentsInChildren<Light>(true))
                {
                    light.range *= LightScale;
                    light.intensity *= LightScale;
                }
            }

            if (fireplace.m_smokeSpawner != null && !flames.Any(flame => fireplace.m_smokeSpawner.transform.IsChildOf(flame)))
            {
                fireplace.m_smokeSpawner.transform.localPosition += drop;
            }

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

    // Highest point of a renderer's mesh, in the root's local space.
    private static float Top(Transform root, MeshRenderer renderer)
    {
        Bounds bounds = renderer.GetComponent<MeshFilter>().sharedMesh.bounds;
        float top = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            top = Mathf.Max(top, root.InverseTransformPoint(renderer.transform.TransformPoint(corner)).y);
        }

        return top;
    }
}
