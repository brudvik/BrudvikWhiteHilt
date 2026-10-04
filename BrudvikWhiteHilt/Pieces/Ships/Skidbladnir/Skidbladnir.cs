using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.Skidbladnir;

/// <summary>Freyr's indestructible sailing home, with an empty lower deck for player-built furnishings.</summary>
public class Skidbladnir : WhiteHiltShipBase
{
    /// <summary>Persistent prefab identifier.</summary>
    public const string PrefabName = "WhiteHiltSkidbladnir";

    /// <inheritdoc/>
    protected override string BaseName => PrefabName;
    /// <inheritdoc/>
    protected override string FullName => "Skidbladnir";
    /// <inheritdoc/>
    protected override string Description => "Freyr's enduring sailing home. Furnish the lower deck and climb the mast to watch the sea.";
    /// <inheritdoc/>
    protected override string CopyFrom => "VikingShip";
    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new[]
    {
        new RequirementConfig { Item = "FineWood", Amount = 100, Recover = false },
        new RequirementConfig { Item = "IronNails", Amount = 200, Recover = false },
        new RequirementConfig { Item = "ElderBark", Amount = 50, Recover = false },
        new RequirementConfig { Item = "DeerHide", Amount = 40, Recover = false }
    };
    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <summary>Creates the ship's registration descriptor.</summary>
    /// <param name="manager">Piece registry.</param>
    public Skidbladnir(PieceManager manager) : base(manager)
    {
        Translations.AddEnglish("whitehilt_skidbladnir_empty", "Remove the furnishings before taking Skidbladnir apart");
    }

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab)
    {
        WhiteHiltShipUpgradeSetup.Prepare(prefab);
        SkidbladnirModel.Build(prefab);
    }
}

/// <summary>Server-synchronized settings for Skidbladnir.</summary>
public static class SkidbladnirSettings
{
    /// <summary>Model elevation relative to the ship's buoyancy plane, in metres; requires restart.</summary>
    public static ConfigEntry<float> WaterlineOffset { get; private set; }
    /// <summary>Maximum share of the White Hilt ship's reference full-sail speed.</summary>
    public static ConfigEntry<float> SpeedShare { get; private set; }
    /// <summary>Whether players may attach building pieces to this ship.</summary>
    public static ConfigEntry<bool> Building { get; private set; }
    /// <summary>Maximum speed in metres per second while building.</summary>
    public static ConfigEntry<float> BuildMaxSpeed { get; private set; }
    /// <summary>Seconds between ownership-side furniture sector updates.</summary>
    public static ConfigEntry<float> FurnitureSyncSeconds { get; private set; }
    /// <summary>Seconds to deploy or furl the individual sails.</summary>
    public static ConfigEntry<float> SailSeconds { get; private set; }

    /// <summary>Binds the sailing home's adjustable rules.</summary>
    public static void Initialize()
    {
        WaterlineOffset = WhiteHiltConfig.BindAdminOnly("Ships.Skidbladnir", "WaterlineOffset", 1.2f,
            "Ship model elevation above the buoyancy plane in metres. Restart required.", new AcceptableValueRange<float>(0.6f, 2.5f));
        SpeedShare = WhiteHiltConfig.BindAdminOnly("Ships.Skidbladnir", "SpeedShare", 0.5f,
            "Maximum fraction of the White Hilt Ship's ideal full-sail speed. Never above one half.", new AcceptableValueRange<float>(0.1f, 0.5f));
        Building = WhiteHiltConfig.BindAdminOnly("Ships.Skidbladnir", "Building", true,
            "Players can build ordinary furnishings and crafting stations on Skidbladnir.");
        BuildMaxSpeed = WhiteHiltConfig.BindAdminOnly("Ships.Skidbladnir", "BuildMaxSpeed", 0.25f,
            "Maximum ship speed in metres per second while placing furnishings.", new AcceptableValueRange<float>(0f, 1f));
        FurnitureSyncSeconds = WhiteHiltConfig.BindAdminOnly("Ships.Skidbladnir", "FurnitureSyncSeconds", 1f,
            "Seconds between saved world-position updates for attached furniture.", new AcceptableValueRange<float>(0.1f, 5f));
        SailSeconds = WhiteHiltConfig.BindAdminOnly("Ships.Skidbladnir", "SailSeconds", 2f,
            "Seconds to deploy or furl the sails.", new AcceptableValueRange<float>(0.5f, 10f));
    }
}

internal static class SkidbladnirModel
{
    internal static Vector3 At(float sideways, float height, float length) =>
        new(sideways, height + SkidbladnirSettings.WaterlineOffset.Value, length);
    internal static Vector3 PortalPosition => At(1.75f, 2.5f, 2.7f);

    internal static void Build(GameObject prefab)
    {
        Ship ship = prefab.GetComponent<Ship>();
        Transform root = prefab.transform;
        Renderer template = root.Find("ship/visual/hull_new/hull").GetComponent<Renderer>();
        using Stream stream = typeof(Skidbladnir).Assembly.GetManifestResourceStream("BrudvikWhiteHilt.Skidbladnir.assets.json")
            ?? throw new InvalidOperationException("Skidbladnir asset manifest is missing");
        using StreamReader reader = new(stream);
        Assets assets = SimpleJson.SimpleJson.DeserializeObject<Assets>(reader.ReadToEnd());
        foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            bool upgrade = renderer.transform.IsChildOf(root.Find("ship/visual/Customize"))
                || renderer.GetComponentInParent<ShipChest>() != null || renderer.GetComponentInParent<ShipPortal>() != null
                || renderer.transform.IsChildOf(root.Find(WhiteHiltShipUpgrades.BrazierName));
            bool wisp = renderer.GetComponentsInParent<Transform>(true).Any(parent => parent.name == WhiteHiltShipUpgrades.MastWispName);
            bool mask = renderer.sharedMaterial != null && renderer.sharedMaterial.name.ToLowerInvariant().Contains("watermask");
            if (!upgrade && !wisp && !mask) renderer.enabled = false;
        }
        Transform geometry = new GameObject("SkidbladnirStructure").transform;
        geometry.SetParent(root, false);
        geometry.localPosition = At(0f, 0f, 0f);
        SkidbladnirShip behaviour = prefab.AddComponent<SkidbladnirShip>();
        List<Transform> sails = new();
        if (!VisualHelper.IsHeadless)
        {
            foreach (Part part in assets.parts)
            {
                GameObject model = VisualHelper.CreateModel(geometry, ForagingAssets.LoadMesh(part.mesh), ForagingAssets.LoadTexture(part.texture),
                    template, ToVector(part.pivot), Quaternion.identity, 1f);
                if (part.sail) sails.Add(model.transform);
            }
        }
        behaviour.m_sails = sails.ToArray();
        foreach (Collider collider in prefab.GetComponentsInChildren<Collider>(true))
        {
            bool upgrade = collider.transform.IsChildOf(root.Find("ship/visual/Customize"))
                || collider.GetComponentInParent<ShipChest>() != null || collider.GetComponentInParent<ShipPortal>() != null
                || collider.GetComponentInParent<Container>() != null
                || collider.GetComponentsInParent<Transform>(true).Any(parent => parent.name == ShipTentColliders.ObjectName);
            if (!upgrade && collider != ship.m_floatCollider && !collider.isTrigger)
                collider.enabled = false;
        }
        Transform onboard = root.Find("OnboardTrigger");
        onboard.localPosition = At(0f, 10f, 2f);
        onboard.localRotation = Quaternion.identity;
        onboard.localScale = Vector3.one;
        BoxCollider trigger = onboard.GetComponent<BoxCollider>();
        trigger.center = Vector3.zero;
        trigger.size = new Vector3(8f, 24f, 23f);
        foreach (Block block in assets.colliders)
            AddBox(geometry, block.name, ToVector(block.centre), ToVector(block.size));
        foreach (Prism prism in assets.prisms)
        {
            Vector3[] vertices = Enumerable.Range(0, 6).Select(index => new Vector3(prism.vertices[index * 3],
                prism.vertices[index * 3 + 1], prism.vertices[index * 3 + 2])).ToArray();
            Mesh mesh = new() { name = "SkidbladnirPlatform", vertices = vertices,
                triangles = new[] { 0, 1, 2, 5, 4, 3, 0, 3, 4, 0, 4, 1, 1, 4, 5, 1, 5, 2, 2, 5, 3, 2, 3, 0 } };
            mesh.RecalculateBounds();
            GameObject platform = new("LookoutFloorCollider") { layer = LayerMask.NameToLayer("vehicle") };
            platform.transform.SetParent(geometry, false);
            MeshCollider collider = platform.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = true;
        }
        ship.m_floatCollider.transform.SetParent(root, false);
        ship.m_floatCollider.transform.localPosition = new Vector3(0f, 0f, 2f);
        ship.m_floatCollider.transform.localRotation = Quaternion.identity;
        ship.m_floatCollider.transform.localScale = Vector3.one;
        ship.m_floatCollider.center = Vector3.zero;
        ship.m_floatCollider.size = new Vector3(6.2f, 0.5f, 18f);
        ship.m_waterLevelOffset = 0f;
        prefab.GetComponent<Rigidbody>().centerOfMass = new Vector3(0f, -0.5f, 2f);
        ship.m_hasSail = false;
        ship.m_shipControlls.transform.position = root.TransformPoint(At(0.6f, 4.7f, -6.8f));
        ship.m_shipControlls.m_attachPoint.position = root.TransformPoint(At(0.6f, 4.56f, -6.3f));
        ship.m_controlGuiPos.position = root.TransformPoint(At(0.6f, 5.5f, -6.8f));
        foreach (Collider control in ship.m_shipControlls.GetComponentsInChildren<Collider>(true)) control.enabled = true;
        AddPlank(geometry, "Helm", new Vector3(0.6f, 4.9f, -6.8f), new Vector3(1.2f, 0.12f, 0.45f), template, false);
        Move(root, WhiteHiltShipUpgrades.ChestName, At(1.4f, 4.54f, -6f));
        Move(root, WhiteHiltShipUpgrades.BrazierName, At(-1.65f, 2.5f, 4.8f));
        Move(root, WhiteHiltShipUpgrades.AnchorName, At(3.55f, 2.5f, 6.4f));
        Move(root, WhiteHiltShipUpgrades.MastWispName, At(0f, 21.15f, 0.63f));
        Move(root, ShipPortal.ObjectName, PortalPosition);
        Move(root, "TraderLamp", At(0f, 3.2f, 0.63f));
        Move(root, "piece_chest", At(0f, 2.5f, 6.6f));
        Container hold = prefab.GetComponentsInChildren<Container>(true).First(container => container.GetComponent<ShipChest>() == null);
        hold.transform.position = root.TransformPoint(At(0f, 2.5f, 6.6f));
        foreach (Renderer renderer in hold.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        AddPlank(geometry, "CargoHatch", new Vector3(0f, 2.52f, 6.6f), new Vector3(1.4f, 0.04f, 1.4f), template, false);
        RelocateUpgrades(root);
        AddBox(geometry, "SternDeck", new Vector3(0f, 4.44f, -6f), new Vector3(5.2f, 0.2f, 3.1f));
        AddBox(geometry, "BowDeck", new Vector3(0f, 4.44f, 11.4f), new Vector3(3.8f, 0.2f, 4.2f));
        for (int side = -1; side <= 1; side += 2)
        {
            AddBox(geometry, "LowerFloorMargin", new Vector3(side * 2.85f, -0.66f, 2f), new Vector3(0.5f, 0.12f, 11f));
            AddBox(geometry, "LowerHullWall", new Vector3(side * 3.1f, 0.95f, 2f), new Vector3(0.2f, 3.1f, 11f));
            AddBox(geometry, "DeckRail", new Vector3(side * 3.25f, 2.85f, 2f), new Vector3(0.15f, 0.7f, 11f));
        }
        for (int segment = 0; segment < 16; segment++)
        {
            float angle = (segment + 0.5f) * Mathf.PI * 2f / 16f;
            Transform rail = AddBox(geometry, "LookoutRail", new Vector3(Mathf.Cos(angle) * 1.45f, 19.35f,
                0.63f + Mathf.Sin(angle) * 1.45f), new Vector3(0.6f, 1.1f, 0.08f));
            rail.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
        }
        for (int step = 0; step < 10; step++)
        {
            float height = 2.5f + 2.04f * (step + 1) / 10f;
            AddPlank(geometry, "SternStair", new Vector3(0f, height - 0.06f, -3.4f - 2.4f * (step + 0.5f) / 10f),
                new Vector3(1.2f, 0.12f, 0.24f), template);
            AddPlank(geometry, "BowStair", new Vector3(1.3f, height - 0.06f, 6.8f + 2.6f * (step + 0.5f) / 10f),
                new Vector3(1.2f, 0.12f, 0.26f), template);
        }
        Transform mast = AddBox(geometry, "UpgradeMast", new Vector3(0f, 1.75f, 0.63f), new Vector3(0.4f, 2f, 0.4f));
        mast.gameObject.AddComponent<ShipMastHover>();
        Transform ladder = AddBox(geometry, "MastLadderInteraction", new Vector3(0.55f, 10.6f, 0.63f), new Vector3(0.75f, 16.4f, 0.12f));
        ladder.gameObject.AddComponent<global::BrudvikWhiteHilt.Pieces.Defenses.DefenseLadder>().m_stops = new[]
        {
            At(0.55f, 2.55f, 0.63f), At(-0.55f, 11.85f, 0.63f), At(-0.7f, 18.85f, 0.63f)
        };
        Transform boarding = AddBox(geometry, "BoardingLadder", new Vector3(-3.3f, 1f, -1.5f), new Vector3(0.25f, 3f, 0.7f));
        for (int side = -1; side <= 1; side += 2)
            AddPlank(geometry, "BoardingLadderRail", new Vector3(-3.4f, 1f, -1.5f + side * 0.32f),
                new Vector3(0.08f, 3f, 0.08f), template, false);
        for (int rung = 0; rung < 10; rung++)
            AddPlank(geometry, "BoardingLadderRung", new Vector3(-3.4f, -0.35f + rung * 0.3f, -1.5f),
                new Vector3(0.08f, 0.06f, 0.7f), template, false);
        boarding.gameObject.AddComponent<global::BrudvikWhiteHilt.Pieces.Defenses.DefenseLadder>().m_stops = new[]
        {
            At(-3.6f, -1.1f, -1.5f), At(-2.8f, 2.55f, -1.5f)
        };
        if (!VisualHelper.IsHeadless)
        {
            GameObject net = new("SkidbladnirFishingNet");
            net.transform.SetParent(root, false);
            net.transform.localPosition = At(3.5f, 1.7f, 4f);
            Mesh mesh = ForagingAssets.LoadMesh("fishnet");
            VisualHelper.CreateModel(net.transform, mesh, ForagingAssets.LoadTexture("fishnet_albedo"), template,
                Vector3.zero, Quaternion.identity, 1.5f / mesh.bounds.size.y);
            behaviour.m_fishingNet = net;
            net.SetActive(false);
        }
    }

    internal static Transform AddBox(Transform parent, string name, Vector3 centre, Vector3 size)
    {
        GameObject target = new(name) { layer = LayerMask.NameToLayer("vehicle") };
        target.transform.SetParent(parent, false);
        target.transform.localPosition = centre;
        target.AddComponent<BoxCollider>().size = size;
        return target.transform;
    }

    private static Vector3 ToVector(float[] values) => new(values[0], values[1], values[2]);

    private static void Move(Transform root, string name, Vector3 position)
    {
        Transform target = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == name);
        if (target == null) return;
        if (name != "TraderLamp") target.SetParent(root, true);
        target.position = root.TransformPoint(position);
    }

    private static void RelocateUpgrades(Transform root)
    {
        Transform customize = root.Find("ship/visual/Customize");
        Transform storage = customize.Find("storage");
        if (storage != null) FitGroup(root, storage, At(1.8f, 2.5f, 5.7f), new Vector2(1.6f, 1.6f));
        Transform[] parts = customize.Cast<Transform>().Where(child => child.name.StartsWith("ShipTen")
            && child.name != "ShipTentLeft" && child.name != "ShipTentRight").ToArray();
        if (parts.Length == 0) return;
        Transform tent = new GameObject("ShipTen2_Skidbladnir").transform;
        tent.SetParent(customize, false);
        foreach (Transform part in parts) part.SetParent(tent, true);
        Transform colliders = root.Find(ShipTentColliders.ObjectName);
        if (colliders != null) colliders.SetParent(tent, true);
        FitGroup(root, tent, At(-1.65f, 2.5f, 4.2f), new Vector2(2.4f, 2.4f));
        WhiteHiltShipUpgrades upgrades = root.GetComponent<WhiteHiltShipUpgrades>();
        upgrades.m_tentCenter = At(-1.65f, 3.5f, 4.2f);
        upgrades.m_tentSize = new Vector3(2.4f, 2.4f, 2.4f);
    }

    private static void FitGroup(Transform root, Transform group, Vector3 position, Vector2 footprint)
    {
        Vector3[] points = group.GetComponentsInChildren<MeshFilter>(true).Where(filter => filter.sharedMesh != null)
            .SelectMany(filter => Enumerable.Range(0, 8).Select(corner => filter.transform.TransformPoint(filter.sharedMesh.bounds.center
                + Vector3.Scale(filter.sharedMesh.bounds.extents, new Vector3((corner & 1) == 0 ? -1f : 1f,
                    (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f)))))
            .Select(root.InverseTransformPoint).ToArray();
        if (points.Length == 0) return;
        Bounds bounds = new(points[0], Vector3.zero);
        foreach (Vector3 point in points) bounds.Encapsulate(point);
        float scale = Mathf.Min(footprint.x / bounds.size.x, footprint.y / bounds.size.z);
        Vector3 basePoint = new(bounds.center.x, bounds.min.y, bounds.center.z);
        Vector3 localBase = group.InverseTransformPoint(root.TransformPoint(basePoint));
        group.localScale *= scale;
        group.position += root.TransformPoint(position) - group.TransformPoint(localBase);
    }

    private static void AddPlank(Transform parent, string name, Vector3 position, Vector3 size, Renderer template, bool collision = true)
    {
        if (collision) AddBox(parent, name, position, size);
        if (VisualHelper.IsHeadless) return;
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name + "Visual";
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = size;
        UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
        cube.GetComponent<Renderer>().sharedMaterial = new Material(template.sharedMaterial)
        {
            mainTexture = ForagingAssets.LoadTexture("sailing_ship_1_albedo")
        };
    }

    /// <summary>Skidbladnir's exported structural manifest.</summary>
    public class Assets
    {
        /// <summary>Exported material groups.</summary>
        public Part[] parts { get; set; }
        /// <summary>Exported structural boxes.</summary>
        public Block[] colliders { get; set; }
        /// <summary>Convex platform slabs preserving the ladder openings.</summary>
        public Prism[] prisms { get; set; }
    }
    /// <summary>One material group in the asset bundle.</summary>
    public class Part
    {
        /// <summary>Bundle mesh identifier.</summary>
        public string mesh { get; set; }
        /// <summary>Bundle texture identifier.</summary>
        public string texture { get; set; }
        /// <summary>Whether this group contains sails.</summary>
        public bool sail { get; set; }
        /// <summary>Local model pivot in source metres.</summary>
        public float[] pivot { get; set; }
    }
    /// <summary>One structural box collider.</summary>
    public class Block
    {
        /// <summary>Structure identifier.</summary>
        public string name { get; set; }
        /// <summary>Ship-space centre in metres.</summary>
        public float[] centre { get; set; }
        /// <summary>Box dimensions in metres.</summary>
        public float[] size { get; set; }
    }
    /// <summary>A convex platform slab.</summary>
    public class Prism
    {
        /// <summary>Six vertices of one triangular platform slab.</summary>
        public float[] vertices { get; set; }
    }
}