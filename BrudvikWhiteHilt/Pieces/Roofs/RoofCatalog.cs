using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Roofs;

/// <summary>
/// Creates the White Hilt roofs: every covering in every shape and pitch of the vanilla wooden roof, the smoke hole
/// pieces and the dragon gables. Each piece is a clone of the vanilla roof piece (snap points, station, build menu
/// group), with a mesh built in code by <see cref="RoofMeshBuilder"/>.
/// </summary>
public static class RoofCatalog
{
    private const string GablePrefix = "piece_whitehilt_roof_gable_";

    private static readonly RoofShape[] shapes = { RoofShape.Slope, RoofShape.Ridge, RoofShape.InnerCorner, RoofShape.OuterCorner, RoofShape.SmokeHole };
    private static readonly RoofPitch[] pitches = { RoofPitch.Low, RoofPitch.Medium, RoofPitch.Steep };
    private static readonly List<Entry> entries = new();
    private static readonly List<GameObject> gables = new();
    private static readonly Dictionary<RoofCovering, Material[]> materials = new();
    private static List<(Mesh Mesh, Material[] Materials, Matrix4x4 Matrix)> plantModel;
    private static bool orderHooked;

    /// <summary>
    /// Registers the translations and creates the pieces once the vanilla prefabs can be cloned. Call from the plugin's
    /// Awake, after <see cref="RoofSettings.Initialize"/>.
    /// </summary>
    public static void Initialize()
    {
        RoofFamily.RegisterTranslations();
        PrefabManager.OnVanillaPrefabsAvailable += Create;
    }

    /// <summary>
    /// Prefab name of a roof piece.
    /// </summary>
    /// <param name="covering">The covering.</param>
    /// <param name="shape">The shape.</param>
    /// <param name="pitch">The pitch.</param>
    /// <returns>The prefab name.</returns>
    public static string PrefabName(RoofCovering covering, RoofShape shape, RoofPitch pitch)
    {
        return $"piece_whitehilt_roof_{RoofCoverings.Id(covering)}_{ShapeId(shape)}_{RoofMeshBuilder.Degrees(pitch)}";
    }

    /// <summary>
    /// Applies changed or server-synced config values: which coverings can be built, their recipes and health.
    /// </summary>
    public static void ApplyConfig()
    {
        foreach (Entry entry in entries)
        {
            string recipe = RoofSettings.Recipe(entry.Covering);
            if (entry.Shape == RoofShape.SmokeHole)
            {
                recipe += "," + RoofSettings.SmokeHoleExtra.Value;
            }

            Apply(entry.Prefab, RoofSettings.IsEnabled(entry.Covering), recipe, RoofSettings.Health(entry.Covering));
        }

        foreach (GameObject gable in gables)
        {
            Apply(gable, RoofSettings.GableEnabled.Value, RoofSettings.GableRecipe.Value, null);
        }

        TarKiln.ApplyConfig();
        SoapstoneHearth.ApplyConfig();
    }

    private static void Create()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= Create;
        foreach (RoofCovering covering in RoofCoverings.All)
        {
            foreach (RoofShape shape in shapes)
            {
                foreach (RoofPitch pitch in pitches)
                {
                    Try($"{covering} roof {shape} {RoofMeshBuilder.Degrees(pitch)}", () => CreateRoof(covering, shape, pitch));
                }
            }
        }

        foreach (RoofPitch pitch in pitches)
        {
            Try($"Dragon gable {RoofMeshBuilder.Degrees(pitch)}", () => CreateGable(pitch));
        }

        if (!orderHooked)
        {
            orderHooked = true;
            PieceManager.OnPiecesRegistered += PlaceAfterVanillaRoofs;
        }

        Jotunn.Logger.LogInfo($"{entries.Count} roof pieces and {gables.Count} gables added!");
    }

    private static void Try(string what, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{what} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private static void CreateRoof(RoofCovering covering, RoofShape shape, RoofPitch pitch)
    {
        RoofFamily family = RoofFamily.Get(covering);
        string recipe = RoofSettings.Recipe(covering);
        string description = Translations.Token($"{family.NameKey}_description");
        if (shape == RoofShape.SmokeHole)
        {
            recipe += "," + RoofSettings.SmokeHoleExtra.Value;
            description += " " + Translations.Token("whitehilt_roofshape_smokehole_description");
        }

        string name = Translations.Token(family.NameKey)
            + (shape == RoofShape.Slope ? string.Empty : " " + Translations.Token($"whitehilt_roofshape_{ShapeId(shape)}"))
            + $" {RoofMeshBuilder.Degrees(pitch)}°";
        PieceConfig config = new()
        {
            Name = name,
            Description = description,
            PieceTable = PieceTables.Hammer,
            Requirements = ToConfig(recipe)
        };

        CustomPiece piece = new(PrefabName(covering, shape, pitch), VanillaName(shape, pitch), config);
        GameObject prefab = piece.PiecePrefab;
        Strip(prefab);

        RoofStyle style = RoofCoverings.Style(covering);
        Mesh mesh = RoofMeshBuilder.Build(shape, pitch, style);
        GameObject visual = new("New") { layer = prefab.layer };
        visual.transform.SetParent(prefab.transform, false);
        GameObject collider = new("collider") { layer = prefab.layer };
        collider.transform.SetParent(prefab.transform, false);
        collider.AddComponent<MeshCollider>().sharedMesh = mesh;

        RoofInfo info = prefab.AddComponent<RoofInfo>();
        info.m_covering = covering;
        info.m_shape = shape;
        info.m_pitch = pitch;

        SetUpWearNTear(prefab, visual, family);
        if (shape == RoofShape.SmokeHole)
        {
            AddHatch(prefab, pitch);
        }

        if (covering == RoofCovering.Turf && shape == RoofShape.Slope)
        {
            AddGarden(prefab, pitch);
        }

        if (!VisualHelper.IsHeadless)
        {
            Material[] shared = GetMaterials(covering);
            AddModel(visual.transform, "roof", mesh, shared);
            AddEaves(prefab, shape, pitch, style, shared);
            Sprite icon = VisualHelper.RenderIcon(prefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
            }
        }

        piece.Piece.m_enabled = RoofSettings.IsEnabled(covering);
        PieceManager.Instance.AddPiece(piece);
        entries.Add(new Entry(prefab, covering, shape));
    }

    // Keeps only the snap points of the vanilla piece; its look and colliders are replaced.
    private static void Strip(GameObject prefab)
    {
        foreach (Transform child in prefab.transform.Cast<Transform>().ToList())
        {
            if (!child.CompareTag("snappoint"))
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        foreach (Component component in prefab.GetComponents<Collider>().Cast<Component>().Concat(prefab.GetComponents<LODGroup>()))
        {
            UnityEngine.Object.DestroyImmediate(component);
        }
    }

    private static void SetUpWearNTear(GameObject prefab, GameObject visual, RoofFamily family)
    {
        WearNTear wearNTear = prefab.GetComponent<WearNTear>() ?? throw new InvalidOperationException($"{prefab.name} has no WearNTear.");
        wearNTear.m_new = visual;
        wearNTear.m_worn = visual;
        wearNTear.m_broken = visual;
        wearNTear.m_wet = null;
        wearNTear.m_snow = null;
        wearNTear.m_snowWorn = null;
        wearNTear.m_snowBroken = null;
        wearNTear.m_fragmentRoots = null;
        wearNTear.m_health = RoofSettings.Health(family.Covering);
        if (family.Fireproof)
        {
            wearNTear.m_burnable = false;
            wearNTear.m_damages.m_fire = HitData.DamageModifier.VeryResistant;
        }
    }

    private static void AddHatch(GameObject prefab, RoofPitch pitch)
    {
        HatchPlacement hatch = RoofMeshBuilder.Hatch(pitch);
        GameObject pivot = new("hatch") { layer = prefab.layer };
        pivot.transform.SetParent(prefab.transform, false);
        pivot.transform.localPosition = hatch.Hinge;
        pivot.transform.localRotation = hatch.Rotation;

        Vector3 center = new(0f, RoofMeshBuilder.HatchThickness / 2f, hatch.Size / 2f);
        Vector3 size = new(hatch.Size, RoofMeshBuilder.HatchThickness, hatch.Size);
        GameObject lid = new("lid") { layer = prefab.layer };
        lid.transform.SetParent(pivot.transform, false);
        BoxCollider box = lid.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
        if (!VisualHelper.IsHeadless)
        {
            Material[] shared = GetMaterials(RoofCovering.Shingle);
            lid.AddComponent<MeshFilter>().sharedMesh = RoofMeshBuilder.Box(center, size, 1f);
            lid.AddComponent<MeshRenderer>().sharedMaterial = shared[RoofMeshBuilder.TrimMaterial];
        }

        SmokeHoleHatch component = prefab.AddComponent<SmokeHoleHatch>();
        component.m_pivot = pivot.transform;
        component.m_lidCollider = box;
        component.m_closedRotation = hatch.Rotation;
    }

    private static void AddEaves(GameObject prefab, RoofShape shape, RoofPitch pitch, RoofStyle style, Material[] shared)
    {
        RoofPart[] parts = RoofMeshBuilder.Eaves(shape);
        if (parts.Length == 0)
        {
            return;
        }

        RoofEave eave = prefab.AddComponent<RoofEave>();
        eave.m_eaves = new GameObject[parts.Length];
        eave.m_probes = new Vector3[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            GameObject holder = new(parts[i].ToString()) { layer = prefab.layer };
            holder.transform.SetParent(prefab.transform, false);
            AddModel(holder.transform, "eave", RoofMeshBuilder.Build(shape, pitch, style, parts[i]), shared);
            eave.m_eaves[i] = holder;

            // Just past the eave, on the plane of the roof, where a piece further down would be.
            float x = parts[i] == RoofPart.EaveX ? 1.7f : 0f;
            float z = parts[i] == RoofPart.EaveX ? 0f : 1.7f;
            eave.m_probes[i] = new Vector3(x, RoofMeshBuilder.Height(shape, pitch, x, z) - 0.05f, z);
        }
    }

    private static GameObject AddModel(Transform parent, string name, Mesh mesh, Material[] shared)
    {
        GameObject model = new(name) { layer = parent.gameObject.layer };
        model.transform.SetParent(parent, false);
        model.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = model.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = shared;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;
        return model;
    }

    // Covering, underside, edge and trim: copies of the vanilla wall material with the roof textures.
    private static Material[] GetMaterials(RoofCovering covering)
    {
        if (materials.TryGetValue(covering, out Material[] cached))
        {
            return cached;
        }

        Material wood = FindWoodMaterial();
        string texture = RoofCoverings.Texture(covering);
        Material top = MakeMaterial(wood, texture);
        string edgeTexture = RoofCoverings.EdgeTexture(covering);
        Material edge = edgeTexture == null ? wood : edgeTexture == texture ? top : MakeMaterial(wood, edgeTexture);
        Material[] result = { top, wood, edge, wood };
        materials[covering] = result;
        return result;
    }

    private static Material FindWoodMaterial()
    {
        GameObject roof = PrefabManager.Instance.GetPrefab("wood_roof") ?? throw new InvalidOperationException("wood_roof not found");
        Material[] all = roof.GetComponentsInChildren<Renderer>(true).SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null).ToArray();
        return all.FirstOrDefault(material => material.name.StartsWith("woodwall", StringComparison.Ordinal) && !material.name.Contains("worn"))
            ?? all.FirstOrDefault()
            ?? throw new InvalidOperationException("wood_roof has no material");
    }

    private static Material MakeMaterial(Material template, string texture)
    {
        Material material = VisualHelper.CreateTexturedMaterial(template, ForagingAssets.LoadTexture($"{texture}_albedo"), texture);
        material.mainTextureScale = Vector2.one;
        material.mainTextureOffset = Vector2.zero;
        try
        {
            material.SetTexture("_BumpMap", ForagingAssets.LoadTexture($"{texture}_normal"));
            material.EnableKeyword("_NORMALMAP");
        }
        catch (InvalidOperationException)
        {
            // Not every texture has relief (the turf edge has none).
        }

        return material;
    }

    // Three roseroot plants standing on the slope, hidden until something is planted. A server keeps only the garden.
    private static void AddGarden(GameObject prefab, RoofPitch pitch)
    {
        TurfGarden garden = prefab.AddComponent<TurfGarden>();
        List<(Mesh Mesh, Material[] Materials, Matrix4x4 Matrix)> model = VisualHelper.IsHeadless ? null : GetPlantModel();
        if (model == null || model.Count == 0)
        {
            return;
        }

        GameObject plants = new("garden") { layer = prefab.layer };
        plants.transform.SetParent(prefab.transform, false);
        Vector2[] spots = { new(-0.5f, -0.35f), new(0.45f, 0.05f), new(-0.1f, 0.55f) };
        for (int i = 0; i < spots.Length; i++)
        {
            Vector2 spot = spots[i];
            GameObject pivot = new($"plant {i}") { layer = prefab.layer };
            pivot.transform.SetParent(plants.transform, false);
            pivot.transform.localPosition = new Vector3(spot.x, RoofMeshBuilder.Height(RoofShape.Slope, pitch, spot.x, spot.y) - 0.03f, spot.y);
            pivot.transform.localRotation = Quaternion.Euler(0f, i * 117f, 0f);
            foreach ((Mesh mesh, Material[] shared, Matrix4x4 matrix) in model)
            {
                GameObject part = new("roseroot") { layer = prefab.layer };
                part.transform.SetParent(pivot.transform, false);
                part.transform.localPosition = matrix.GetColumn(3);
                part.transform.localRotation = matrix.rotation;
                part.transform.localScale = matrix.lossyScale;
                part.AddComponent<MeshFilter>().sharedMesh = mesh;
                part.AddComponent<MeshRenderer>().sharedMaterials = shared;
            }
        }

        plants.SetActive(false);
        garden.m_plants = plants;
    }

    // The roseroot look: the vanilla thistle with its purple flowers turned yellow, as the forageable has it.
    private static List<(Mesh, Material[], Matrix4x4)> GetPlantModel()
    {
        if (plantModel != null)
        {
            return plantModel;
        }

        plantModel = new List<(Mesh, Material[], Matrix4x4)>();
        GameObject thistle = PrefabManager.Instance.GetPrefab("Pickable_Thistle");
        if (thistle == null)
        {
            return plantModel;
        }

        Dictionary<Material, Material> recoloured = new();
        foreach (MeshRenderer renderer in thistle.GetComponentsInChildren<MeshRenderer>(true))
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || !VisualHelper.IsActiveBelow(renderer.transform, thistle.transform))
            {
                continue;
            }

            Material[] shared = renderer.sharedMaterials.Select(source =>
            {
                if (source == null || source.mainTexture == null)
                {
                    return source;
                }

                if (!recoloured.TryGetValue(source, out Material material))
                {
                    material = new Material(source) { mainTexture = VisualHelper.RecolorTexture(source.mainTexture, YellowFlowers) };
                    recoloured[source] = material;
                }

                return material;
            }).ToArray();
            plantModel.Add((filter.sharedMesh, shared, thistle.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix));
        }

        return plantModel;
    }

    private static Color32 YellowFlowers(Color32 pixel)
    {
        Color.RGBToHSV(pixel, out float hue, out float saturation, out float value);
        if (hue < 0.72f || hue > 0.95f || saturation < 0.25f || value < 0.1f)
        {
            return pixel;
        }

        Color32 result = Color.HSVToRGB(0.13f, saturation, Mathf.Clamp01(value * 1.3f));
        result.a = pixel.a;
        return result;
    }

    private static void CreateGable(RoofPitch pitch)
    {
        PieceConfig config = new()
        {
            Name = Translations.Token("whitehilt_roof_gable") + $" {RoofMeshBuilder.Degrees(pitch)}°",
            Description = Translations.Token("whitehilt_roof_gable_description"),
            PieceTable = PieceTables.Hammer,
            Requirements = ToConfig(RoofSettings.GableRecipe.Value)
        };

        CustomPiece piece = new(GablePrefix + RoofMeshBuilder.Degrees(pitch), "wood_pole2", config);
        GameObject prefab = piece.PiecePrefab;
        foreach (Transform child in prefab.transform.Cast<Transform>().ToList())
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        foreach (Component component in prefab.GetComponents<Collider>().Cast<Component>().Concat(prefab.GetComponents<LODGroup>()))
        {
            UnityEngine.Object.DestroyImmediate(component);
        }

        Mesh boards = RoofMeshBuilder.GableBoards(pitch, out (Vector3 Position, Vector3 Outward, Vector3 Up)[] tips);
        GameObject visual = new("New") { layer = prefab.layer };
        visual.transform.SetParent(prefab.transform, false);
        GameObject collider = new("collider") { layer = prefab.layer };
        collider.transform.SetParent(prefab.transform, false);
        BoxCollider box = collider.AddComponent<BoxCollider>();
        box.center = boards.bounds.center;
        box.size = boards.bounds.size;

        GameObject snap = new("$hud_snappoint") { tag = "snappoint" };
        snap.transform.SetParent(prefab.transform, false);
        snap.SetActive(false);

        WearNTear wearNTear = prefab.GetComponent<WearNTear>();
        if (wearNTear != null)
        {
            wearNTear.m_new = visual;
            wearNTear.m_worn = visual;
            wearNTear.m_broken = visual;
            wearNTear.m_wet = null;
            wearNTear.m_snow = null;
            wearNTear.m_fragmentRoots = null;
        }

        if (!VisualHelper.IsHeadless)
        {
            Material wood = GetMaterials(RoofCovering.Shingle)[RoofMeshBuilder.TrimMaterial];
            AddModel(visual.transform, "boards", boards, new[] { wood });
            AddDragonHeads(visual.transform, tips);
            Sprite icon = VisualHelper.RenderIcon(prefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
            }
        }

        piece.Piece.m_enabled = RoofSettings.GableEnabled.Value;
        PieceManager.Instance.AddPiece(piece);
        gables.Add(prefab);
    }

    // The carved dragon head the longship keeps among its unused parts.
    private static void AddDragonHeads(Transform parent, (Vector3 Position, Vector3 Outward, Vector3 Up)[] tips)
    {
        Transform source = PrefabManager.Instance.GetPrefab("VikingShip")?.transform.Find("ship/visual/unused/dragon_head");
        MeshFilter filter = source != null ? source.GetComponent<MeshFilter>() : null;
        MeshRenderer renderer = source != null ? source.GetComponent<MeshRenderer>() : null;
        if (filter == null || filter.sharedMesh == null || renderer == null)
        {
            Jotunn.Logger.LogWarning("Dragon gable: the longship's dragon head was not found; the gable has boards only.");
            return;
        }

        foreach ((Vector3 tip, Vector3 outward, Vector3 up) in tips)
        {
            GameObject head = new("dragon_head") { layer = parent.gameObject.layer };
            head.transform.SetParent(parent, false);
            head.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            head.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
            RoofMeshBuilder.PlaceGableHead(head.transform, filter.sharedMesh.bounds, tip, outward, up);
        }
    }

    private static void Apply(GameObject prefab, bool enabled, string recipe, float? health)
    {
        if (prefab == null)
        {
            return;
        }

        Piece piece = prefab.GetComponent<Piece>();
        if (piece != null)
        {
            piece.m_enabled = enabled;
            if (ObjectDB.instance != null)
            {
                Piece.Requirement[] requirements = Resolve(recipe);
                if (requirements.Length > 0)
                {
                    piece.m_resources = requirements;
                }
            }
        }

        if (!health.HasValue)
        {
            return;
        }

        WearNTear template = prefab.GetComponent<WearNTear>();
        if (template == null || Mathf.Approximately(template.m_health, health.Value))
        {
            return;
        }

        template.m_health = health.Value;
        foreach (WearNTear placed in WearNTear.GetAllInstances())
        {
            if (placed != null && global::Utils.GetPrefabName(placed.gameObject) == prefab.name)
            {
                placed.m_health = health.Value;
            }
        }
    }

    private static RequirementConfig[] ToConfig(string recipe)
    {
        return Parse(recipe).Select(pair => new RequirementConfig { Item = pair.Key, Amount = pair.Value, Recover = true }).ToArray();
    }

    private static Piece.Requirement[] Resolve(string recipe)
    {
        List<Piece.Requirement> result = new();
        foreach (KeyValuePair<string, int> pair in Parse(recipe))
        {
            ItemDrop item = ObjectDB.instance.GetItemPrefab(pair.Key)?.GetComponent<ItemDrop>();
            if (item == null)
            {
                Jotunn.Logger.LogWarning($"Roofs: unknown item '{pair.Key}' in a recipe, skipped.");
                continue;
            }

            result.Add(new Piece.Requirement { m_resItem = item, m_amount = pair.Value, m_recover = true });
        }

        return result.ToArray();
    }

    // Prefab:Amount pairs, comma separated; the same item twice adds up.
    private static Dictionary<string, int> Parse(string recipe)
    {
        Dictionary<string, int> result = new();
        foreach (string part in (recipe ?? string.Empty).Split(','))
        {
            string[] fields = part.Split(':');
            string name = fields[0].Trim();
            if (name.Length == 0)
            {
                continue;
            }

            int amount = fields.Length > 1 && int.TryParse(fields[1].Trim(), out int parsed) ? parsed : 1;
            result[name] = (result.TryGetValue(name, out int sum) ? sum : 0) + Math.Max(1, amount);
        }

        return result;
    }

    private static string VanillaName(RoofShape shape, RoofPitch pitch)
    {
        string suffix = pitch switch { RoofPitch.Low => string.Empty, RoofPitch.Medium => "_45", _ => "_67" };
        return shape switch
        {
            RoofShape.Ridge => "wood_roof_top" + suffix,
            RoofShape.InnerCorner => "wood_roof_icorner" + suffix,
            RoofShape.OuterCorner => "wood_roof_ocorner" + suffix,
            _ => "wood_roof" + suffix
        };
    }

    private static string ShapeId(RoofShape shape)
    {
        return shape switch
        {
            RoofShape.Ridge => "ridge",
            RoofShape.InnerCorner => "icorner",
            RoofShape.OuterCorner => "ocorner",
            RoofShape.SmokeHole => "smokehole",
            _ => "slope"
        };
    }

    // The roofs follow the vanilla roofs in the hammer, covering by covering, the gables last.
    private static void PlaceAfterVanillaRoofs()
    {
        PieceTable table = PieceManager.Instance.GetPieceTable(PieceTables.Hammer);
        if (table == null)
        {
            return;
        }

        List<GameObject> pieces = table.m_pieces;
        HashSet<GameObject> ours = new(entries.Select(entry => entry.Prefab).Concat(gables));
        List<GameObject> moved = pieces.Where(ours.Contains).ToList();
        if (moved.Count == 0)
        {
            return;
        }

        pieces.RemoveAll(ours.Contains);
        int last = pieces.FindLastIndex(prefab => prefab != null && (prefab.name.StartsWith("wood_roof", StringComparison.Ordinal) || prefab.name.StartsWith("darkwood_roof", StringComparison.Ordinal)));
        pieces.InsertRange(last < 0 ? pieces.Count : last + 1, moved);
    }

    private sealed class Entry
    {
        public Entry(GameObject prefab, RoofCovering covering, RoofShape shape)
        {
            Prefab = prefab;
            Covering = covering;
            Shape = shape;
        }

        public GameObject Prefab { get; }

        public RoofCovering Covering { get; }

        public RoofShape Shape { get; }
    }
}
