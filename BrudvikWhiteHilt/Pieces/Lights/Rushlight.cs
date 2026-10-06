using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Foraging.Cattail;
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
/// The Rushlight: a cattail rush dipped in fat, held in an iron rush nip on a stump. A small, cheap light that burns
/// cattails instead of resin.
/// </summary>
public class Rushlight : IWhiteHiltCustomPiece
{
    /// <summary>
    /// Prefab name of the rushlight.
    /// </summary>
    public const string PrefabName = "piece_whitehilt_rushlight";

    private const string FullName = "Rushlight";
    private const string Description = "A cattail rush dipped in fat, held slanted in an iron nip. A small, smoky light; feed it cattails.";

    // Height in metres; the tip of the rush is the top of the model, right above its centre.
    private const float Size = 1.1f;
    private const float LightScale = 0.7f;
    private const float FuelSeconds = 2f;

    private readonly PieceManager instance;
    private Fireplace fireplace;

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
    public Rushlight(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the rushlight to the hammer.
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
                CraftingStation = CraftingStations.Workbench,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Wood", Amount = 2, Recover = true },
                    new() { Item = "Iron", Amount = 1, Recover = true },
                    new() { Item = Cattail.PrefabName, Amount = 2, Recover = false }
                }
            };

            CustomPiece piece = new(PrefabName, "piece_groundtorch_wood", pieceConfig);
            fireplace = piece.PiecePrefab.GetComponent<Fireplace>();
            fireplace.m_name = Translations.Token(PrefabName);
            fireplace.m_secPerFuel *= FuelSeconds;
            TryApplyVisual(piece, fireplace);
            instance.AddPiece(piece);

            // The cattail is a mod item, so it is looked up once ObjectDB holds it.
            ItemManager.OnItemsRegistered += SetFuel;
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private void SetFuel()
    {
        ItemDrop cattail = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(Cattail.PrefabName)?.GetComponent<ItemDrop>() : null;
        if (fireplace != null && cattail != null)
        {
            fireplace.m_fuelItem = cattail;
        }
    }

    // Hides the cloned torch's wood (not its flames) and puts the rushlight model in its place, moving the flame, its
    // light and its smoke down to the tip of the rush. Should anything be missing, the vanilla look stays.
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

            Mesh mesh = ForagingAssets.LoadMesh("rushlight");
            float scale = Size / mesh.bounds.size.y;
            Vector3 position = -new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z) * scale;
            VisualHelper.CreateModel(root, mesh, ForagingAssets.LoadTexture("rushlight_albedo"), torch[0], position, Quaternion.identity, scale);

            // The flame, its light and its smoke move down from the torch head to the tip of the rush.
            Vector3 drop = Vector3.up * (Size - torchTop);
            foreach (Transform flame in flames)
            {
                flame.localPosition += drop;
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
