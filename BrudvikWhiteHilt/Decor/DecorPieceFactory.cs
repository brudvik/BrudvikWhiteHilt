using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BrudvikWhiteHilt.Decor;

/// <summary>
/// Turns a <see cref="DecorEntry"/> into a build piece of the Decor Hammer.
/// </summary>
/// <remarks>
/// Every decoration is a clone of the vanilla wood pole, which brings the piece, health and network parts; its look
/// and colliders are replaced. The look comes from one of three places:
/// <list type="bullet">
/// <item>A vanilla prefab's visible parts. Only meshes, LODs, cloth, lights and particles are copied, never its
/// Destructible, Pickable or drops, so a decorative bush cannot be chopped for wood or picked for berries. Vegetation
/// keeps Valheim's own materials, and with them its wind.</item>
/// <item>A model from the decor bundle. Plants get a copy of a vanilla bush's vegetation material, which sways in the
/// wind, bends when walked through and gets wet and snowy like the bushes around it; everything else gets a vanilla
/// piece material, so it weathers like the building it stands in.</item>
/// <item>A model the main bundle already holds for another piece.</item>
/// </list>
/// The collider is a box round what is visible. Plants, cloth and small things are on the non-solid piece layer, so
/// players walk through them but the hammer can still remove them.
/// </remarks>
public static class DecorPieceFactory
{
    private const string BasePrefab = "wood_pole2";
    private const string VegetationTemplate = "Bush01";

    // The vanilla bush's material is made for a bush of this height; sway and push are scaled from it.
    private const float TemplateHeight = 3f;

    private static readonly Type[] visualComponents =
    {
        typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer), typeof(SkinnedMeshRenderer), typeof(LODGroup),
        typeof(Cloth), typeof(Light), typeof(ParticleSystem), typeof(ParticleSystemRenderer), typeof(LightFlicker), typeof(LightLod)
    };

    private static readonly Dictionary<string, Renderer> templates = new();
    private static readonly Dictionary<Texture2D, Material> vegetationMaterials = new();

    /// <summary>
    /// Builds the piece of an entry and adds it to the hammer's table.
    /// </summary>
    /// <param name="entry">The catalogue entry.</param>
    /// <param name="table">Name of the hammer's piece table.</param>
    /// <returns>Whether the piece was added; false when its look could not be found.</returns>
    public static bool Add(DecorEntry entry, string table)
    {
        // Plants and things from the wild can be placed anywhere; furniture and tools need a workbench, as in vanilla.
        bool wild = entry.Category == "Garden" || entry.Category == "Wilds";
        CustomPiece piece = new(entry.PrefabName, BasePrefab, new PieceConfig
        {
            Name = Translations.Token(entry.PrefabName),
            Description = Translations.Token($"{entry.PrefabName}_description"),
            PieceTable = table,
            Category = entry.Category,
            CraftingStation = wild ? string.Empty : CraftingStations.Workbench,
            Requirements = entry.Requirements
        });

        GameObject prefab = piece.PiecePrefab;
        Strip(prefab);
        GameObject visual = new("New") { layer = prefab.layer };
        visual.transform.SetParent(prefab.transform, false);
        if (!BuildLook(entry, visual.transform))
        {
            UnityEngine.Object.DestroyImmediate(prefab);
            return false;
        }

        visual.transform.localScale *= entry.Scale;
        Bounds bounds = LocalBounds(prefab.transform);
        AddCollider(prefab.transform, bounds, entry.Solid);
        SetUpWearNTear(prefab, visual, entry);
        SetUpPiece(piece.Piece, wild);
        AddLight(entry, prefab.transform, bounds);
        if (entry.Seat > 0f)
        {
            AddSeat(prefab, bounds, entry.Seat);
        }

        Sprite icon = VisualHelper.RenderIcon(prefab);
        if (icon != null)
        {
            piece.Piece.m_icon = icon;
        }

        PieceManager.Instance.AddPiece(piece);
        return true;
    }

    private static bool BuildLook(DecorEntry entry, Transform visual)
    {
        try
        {
            switch (entry.Look)
            {
                case DecorLook.Vanilla:
                    return CopyVanilla(entry.VanillaPrefab, visual);
                case DecorLook.Bundle:
                    return AddModel(entry, visual, ForagingAssets.LoadMesh(entry.BundleMesh), ForagingAssets.LoadTexture($"{entry.BundleMesh}_albedo"));
                default:
                    return AddModel(entry, visual, DecorAssets.LoadMesh(entry.MeshName), DecorAssets.LoadTexture($"{entry.MeshName}_albedo"));
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Decor: {entry.Id} is left out: {ex.Message}");
            return false;
        }
    }

    // Copies the visible parts of a vanilla prefab under the piece's visual and removes every other component, so the
    // copy only shows: it cannot be chopped, picked, mined or looted.
    private static bool CopyVanilla(string prefabName, Transform visual)
    {
        GameObject source = PrefabManager.Instance.GetPrefab(prefabName);
        if (source == null)
        {
            Jotunn.Logger.LogWarning($"Decor: the vanilla prefab {prefabName} was not found.");
            return false;
        }

        visual.localScale = source.transform.localScale;
        foreach (Transform child in source.transform)
        {
            GameObject copy = UnityEngine.Object.Instantiate(child.gameObject, visual, false);
            copy.name = child.name;
        }

        // Some props carry their mesh on the root itself.
        if (source.GetComponent<MeshFilter>() is MeshFilter rootFilter && source.GetComponent<MeshRenderer>() is MeshRenderer rootRenderer)
        {
            GameObject root = new("root") { layer = source.layer };
            root.transform.SetParent(visual, false);
            root.AddComponent<MeshFilter>().sharedMesh = rootFilter.sharedMesh;
            MeshRenderer renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = rootRenderer.sharedMaterials;
            renderer.shadowCastingMode = rootRenderer.shadowCastingMode;
        }

        // Unity refuses to remove a component another one requires, so this runs until nothing more goes: the dependent
        // one is removed in one pass, the one it needed in the next.
        for (int pass = 0; pass < 4; pass++)
        {
            List<Component> unwanted = visual.GetComponentsInChildren<Component>(true)
                .Where(component => component != null && !visualComponents.Any(type => type.IsInstanceOfType(component)))
                .ToList();
            if (unwanted.Count == 0)
            {
                break;
            }

            foreach (Component component in Enumerable.Reverse(unwanted))
            {
                UnityEngine.Object.DestroyImmediate(component);
            }
        }

        // A network object left inside a piece would register a second object in the world for every copy placed.
        if (visual.GetComponentsInChildren<ZNetView>(true).Length > 0 || visual.GetComponentsInChildren<Collider>(true).Length > 0)
        {
            Jotunn.Logger.LogWarning($"Decor: {prefabName} keeps parts that cannot be removed, so it is left out.");
            return false;
        }

        CopyLods(source, visual);
        return visual.GetComponentsInChildren<Renderer>(true).Length > 0;
    }

    // A LOD group on the vanilla root would be left behind, and every level of detail would then show at once. The
    // group is rebuilt on the visual with the copied renderers, found by their path under the root.
    private static void CopyLods(GameObject source, Transform visual)
    {
        LODGroup group = source.GetComponent<LODGroup>();
        if (group == null)
        {
            return;
        }

        LOD[] lods = group.GetLODs().Select(lod => new LOD(lod.screenRelativeTransitionHeight,
            lod.renderers.Where(renderer => renderer != null)
                .Select(renderer => FindCopy(source.transform, renderer.transform, visual)?.GetComponent<Renderer>())
                .Where(renderer => renderer != null)
                .ToArray())).ToArray();
        LODGroup copy = visual.gameObject.AddComponent<LODGroup>();
        copy.SetLODs(lods);
        copy.RecalculateBounds();
    }

    private static Transform FindCopy(Transform sourceRoot, Transform original, Transform copyRoot)
    {
        if (original == sourceRoot)
        {
            return copyRoot.Find("root");
        }

        List<string> path = new();
        for (Transform step = original; step != null && step != sourceRoot; step = step.parent)
        {
            path.Insert(0, step.name);
        }

        return copyRoot.Find(string.Join("/", path));
    }

    // A model from a bundle, normalised by convert_glb.py to a height of 1 with its base at the origin.
    private static bool AddModel(DecorEntry entry, Transform visual, Mesh mesh, Texture2D texture)
    {
        if (mesh == null || texture == null)
        {
            Jotunn.Logger.LogWarning($"Decor: the model of {entry.Id} is not in the bundle.");
            return false;
        }

        float height = entry.Height > 0f ? entry.Height : 1f;
        Renderer template = Template(entry.Material);
        GameObject model = VisualHelper.CreateModel(visual, mesh, texture, template, Vector3.zero, Quaternion.identity, height);
        MeshRenderer renderer = model.GetComponent<MeshRenderer>();
        if (entry.Wind)
        {
            renderer.sharedMaterial = VegetationMaterial(texture, height);
        }

        // Small things on a table cast no shadow worth its cost.
        if (height < 0.25f)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        return true;
    }

    // A copy of the vanilla bush's material with the plant's texture. Its sway and push are scaled to the plant's height,
    // and it culls back faces, as convert_glb.py has already made the mesh double-sided.
    private static Material VegetationMaterial(Texture2D texture, float height)
    {
        if (vegetationMaterials.TryGetValue(texture, out Material cached))
        {
            return cached;
        }

        Material template = PrefabManager.Instance.GetPrefab(VegetationTemplate)?.GetComponentInChildren<MeshRenderer>(true)?.sharedMaterial
            ?? throw new InvalidOperationException($"the vanilla {VegetationTemplate} was not found");
        Material material = VisualHelper.CreateTexturedMaterial(template, texture, $"{texture.name}_vegetation");
        float share = Mathf.Clamp(height / TemplateHeight, 0.05f, 2f);
        Scale(material, "_Height", height / Mathf.Max(0.01f, material.GetFloat("_Height")));
        Scale(material, "_SwayDistance", share);
        Scale(material, "_PushDistance", Mathf.Max(share, 0.25f));
        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", 2f);
        }

        vegetationMaterials[texture] = material;
        return material;
    }

    private static void Scale(Material material, string property, float factor)
    {
        if (material.HasProperty(property))
        {
            material.SetFloat(property, material.GetFloat(property) * factor);
        }
    }

    // A vanilla piece's renderer, whose material gives the model Valheim's lighting, rain and snow.
    private static Renderer Template(string material)
    {
        string prefabName = material switch
        {
            "stone" => "stone_wall_1x1",
            "metal" => "iron_grate",
            _ => BasePrefab
        };
        if (templates.TryGetValue(prefabName, out Renderer cached) && cached != null)
        {
            return cached;
        }

        // Not simply the first renderer: that is wood_pole2's snow cap, which made the decorations see-through and shiny.
        GameObject prefab = PrefabManager.Instance.GetPrefab(prefabName)
            ?? throw new InvalidOperationException($"the vanilla {prefabName} was not found");
        Renderer renderer = VisualHelper.PieceTemplate(prefab)
            ?? throw new InvalidOperationException($"the vanilla {prefabName} has no visible part");
        templates[prefabName] = renderer;
        return renderer;
    }

    private static void Strip(GameObject prefab)
    {
        foreach (Transform child in prefab.transform.Cast<Transform>().ToList())
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        foreach (Component component in prefab.GetComponents<Collider>().Cast<Component>().Concat(prefab.GetComponents<LODGroup>()))
        {
            UnityEngine.Object.DestroyImmediate(component);
        }
    }

    // The bounds of everything visible, in the prefab's space.
    private static Bounds LocalBounds(Transform root)
    {
        Bounds bounds = new(Vector3.zero, Vector3.zero);
        bool first = true;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : renderer.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null || renderer is ParticleSystemRenderer)
            {
                continue;
            }

            Matrix4x4 toRoot = root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            Vector3 centre = mesh.bounds.center;
            Vector3 extents = mesh.bounds.extents;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = toRoot.MultiplyPoint3x4(centre + Vector3.Scale(extents, new Vector3(
                    (corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f)));
                if (first)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }

        return bounds;
    }

    private static void AddCollider(Transform root, Bounds bounds, bool solid)
    {
        GameObject collider = new("collider") { layer = LayerMask.NameToLayer(solid ? "piece" : "piece_nonsolid") };
        collider.transform.SetParent(root, false);
        BoxCollider box = collider.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = Vector3.Max(bounds.size, Vector3.one * 0.1f);
    }

    private static void SetUpWearNTear(GameObject prefab, GameObject visual, DecorEntry entry)
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
        // Decorations do not carry anything and may hang from a beam or stand on a shelf without support.
        wear.m_noSupportWear = true;
        wear.m_noRoofWear = true;
        wear.m_supports = false;
        wear.m_health = entry.Wind ? 30f : entry.Material switch { "stone" => 300f, "metal" => 300f, "cloth" => 50f, _ => 150f };
        wear.m_materialType = entry.Material switch
        {
            "stone" => WearNTear.MaterialType.Stone,
            "metal" => WearNTear.MaterialType.Iron,
            _ => WearNTear.MaterialType.Wood
        };

        // The dust and sound of breaking stone or metal, from a vanilla piece of that material.
        string effectsFrom = entry.Material switch { "stone" => "stone_wall_1x1", "metal" => "iron_grate", _ => null };
        WearNTear effects = effectsFrom != null ? PrefabManager.Instance.GetPrefab(effectsFrom)?.GetComponent<WearNTear>() : null;
        if (effects != null)
        {
            wear.m_destroyedEffect = effects.m_destroyedEffect;
            wear.m_hitEffect = effects.m_hitEffect;
        }
    }

    private static void SetUpPiece(Piece piece, bool wild)
    {
        piece.m_groundPiece = false;
        piece.m_groundOnly = false;
        piece.m_cultivatedGroundOnly = false;
        piece.m_allowedInDungeons = true;
        piece.m_noInWater = false;
        piece.m_comfort = 0;
        // Plants and stones may sink a little into the ground, as the vanilla ones do.
        piece.m_clipGround = wild;
        piece.m_clipEverything = wild;
    }

    // A flame borrowed from the vanilla campfire, sized for the piece. Candles and lanterns keep the light short and quiet.
    private static void AddLight(DecorEntry entry, Transform root, Bounds bounds)
    {
        if (entry.Light == DecorLight.None)
        {
            return;
        }

        (float height, float scale, float range, float intensity) = entry.Light switch
        {
            DecorLight.Candle => (0.92f, 0.05f, 3f, 1.1f),
            DecorLight.Lantern => (0.5f, 0.07f, 6f, 1.3f),
            _ => (0.12f, 0.55f, 9f, 1.5f)
        };
        Vector3 position = new(bounds.center.x, Mathf.Lerp(bounds.min.y, bounds.max.y, entry.LightAt >= 0f ? entry.LightAt : height), bounds.center.z);
        Transform flames = FireEffects.AddFlames(root, "WhiteHiltDecorFlame", position, scale);
        foreach (Light light in flames.GetComponentsInChildren<Light>(true))
        {
            light.range = range;
            light.intensity = intensity;
        }

        if (entry.Light != DecorLight.Fire)
        {
            foreach (AudioSource sound in flames.GetComponentsInChildren<AudioSource>(true))
            {
                UnityEngine.Object.DestroyImmediate(sound.gameObject);
            }
        }
    }

    // A chair like the vanilla one, sitting at the seat height in the middle of the piece, facing its front.
    private static void AddSeat(GameObject prefab, Bounds bounds, float seat)
    {
        Chair vanilla = PrefabManager.Instance.GetPrefab("piece_chair")?.GetComponent<Chair>();
        if (vanilla == null)
        {
            return;
        }

        Chair chair = prefab.AddComponent<Chair>();
        foreach (FieldInfo field in typeof(Chair).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            field.SetValue(chair, field.GetValue(vanilla));
        }

        GameObject attach = new("attach");
        attach.transform.SetParent(prefab.transform, false);
        attach.transform.localPosition = new Vector3(bounds.center.x, seat, bounds.center.z);
        chair.m_attachPoint = attach.transform;
    }
}
