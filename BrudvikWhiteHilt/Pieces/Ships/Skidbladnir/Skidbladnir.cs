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
    /// <summary>Furthest camera distance below deck, in metres; 0 keeps the vanilla camera. Local.</summary>
    public static ConfigEntry<float> LowerDeckCameraDistance { get; private set; }

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
        LowerDeckCameraDistance = WhiteHiltConfig.BindLocal("Ships.Skidbladnir", "LowerDeckCameraDistance", 2f,
            "Below deck the camera comes in to this distance, in metres, and stays inside the hull. 0 keeps the vanilla camera.",
            new AcceptableValueRange<float>(0f, 8f));
    }
}

internal static class SkidbladnirModel
{
    internal static Vector3 At(float sideways, float height, float length) =>
        new(sideways, height + SkidbladnirSettings.WaterlineOffset.Value, length);
    internal static Vector3 PortalPosition => At(1.35f, 3.06f, -1.6f);
    // The lower room between its floor, side walls, bulkheads and the waist deck above.
    internal static Bounds LowerDeck => new(At(0f, 0.85f, 2f), new Vector3(5.68f, 2.9f, 11.1f));
    private static readonly string[] UpgradeObjects =
    {
        WhiteHiltShipUpgrades.BrazierName, WhiteHiltShipUpgrades.AnchorName, WhiteHiltShipUpgrades.MastWispName, ShipTentColliders.ObjectName
    };
    // Slopes of the source model's quarterdeck and poop deck, both rising towards the stern.
    internal static readonly Quaternion QuarterdeckTilt = Quaternion.Euler(5.06f, 0f, 0f);
    private static readonly Quaternion PoopTilt = Quaternion.Euler(14.45f, 0f, 0f);
    // Two stern lanterns on their posts and three lamps under the lower deck's ceiling beams, lit with the ship lantern.
    private static readonly Vector3[] ExtraLamps =
    {
        new(1.85f, 6.45f, -7.55f), new(-1.85f, 6.45f, -7.55f), new(-1.2f, 2.18f, 2f), new(0.9f, 2.18f, -3f), new(-1f, 2.18f, 7f)
    };
    // The longship's water effects follow its waterline, about z -7 to 10.4 with a half-beam of 2.5 m;
    // Skidbladnir's runs from z -6.2 to 8.3 with a half-beam of 2.4 m.
    private const float LongshipWaterlineCentre = 1.7f;
    private const float WaterlineCentre = 1.05f;
    private const float WaterlineLengthRatio = 0.83f;
    private const float WaterlineBeamRatio = 0.96f;
    private const float RudderWakeZ = -6.6f;
    // The longship's stools (their base is 9 cm below the box), placed on the measured decks: two by the port rail of the
    // quarterdeck, one by the helm and two facing each other on the forecastle. The fifth is a copy.
    private static readonly (string name, Vector3 at, float yaw)[] Seats =
    {
        ("sit_box", new(-1.95f, 2.913f, -1f), 90f), ("sit_box (1)", new(-1.95f, 2.992f, -1.9f), 90f),
        ("sit_box (2)", new(-1.6f, 4.443f, -6f), 0f), ("sit_box (3)", new(-1.5f, 3.38f, 8.3f), 90f),
        ("sit_box (5)", new(1.5f, 3.38f, 8.3f), -90f)
    };
    // The longship's mast holdfast keeps its offset from the mast; its bow holdfast moves to the forecastle's front rail.
    private static readonly Vector3 MastHoldfast = new(0f, 2.662f, 0.63f);
    private static readonly Vector3 BowHoldfast = new(0f, 3.3f, 10.75f);
    private static readonly Vector3 BowHoldfastStand = new(0f, 3.84f, 10.35f);

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
            // Includes the longship's water mask: it does not fit this hull, whose lower floor already covers the water inside.
            if (!IsUpgradePart(root, renderer.transform)) renderer.enabled = false;
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
                Transform parent = geometry;
                if (part.rudder && assets.rudder != null)
                {
                    Transform hinge = new GameObject("SkidbladnirRudderHinge").transform;
                    hinge.SetParent(geometry, false);
                    hinge.localPosition = ToVector(assets.rudder.hinge);
                    hinge.localRotation = Quaternion.Euler(assets.rudder.tilt, 0f, 0f);
                    parent = new GameObject("SkidbladnirRudder").transform;
                    parent.SetParent(hinge, false);
                    ship.m_rudderObject = parent.gameObject;
                }
                GameObject model = VisualHelper.CreateModel(parent, ForagingAssets.LoadMesh(part.mesh), ForagingAssets.LoadTexture(part.texture),
                    template, ToVector(part.pivot), Quaternion.identity, 1f);
                if (part.sail) sails.Add(model.transform);
            }
        }
        behaviour.m_sails = sails.ToArray();
        foreach (Collider collider in prefab.GetComponentsInChildren<Collider>(true))
        {
            if (!IsUpgradePart(root, collider.transform) && collider != ship.m_floatCollider && !collider.isTrigger)
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
        {
            Transform box = AddBox(geometry, block.name, ToVector(block.centre), ToVector(block.size));
            if (block.forward != null) box.localRotation = Quaternion.LookRotation(ToVector(block.forward), ToVector(block.up));
            if (block.name == "ColliderMainMast") box.gameObject.AddComponent<ShipMastHover>();
        }
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
        ship.m_shipControlls.m_attachPoint.position = root.TransformPoint(At(0.6f, 4.61f, -6.3f));
        ship.m_controlGuiPos.position = root.TransformPoint(At(0.6f, 5.8f, -6.8f));
        foreach (Collider control in ship.m_shipControlls.GetComponentsInChildren<Collider>(true)) control.enabled = true;
        AddPlank(geometry, "Helm", new Vector3(0.6f, 5.05f, -6.8f), new Vector3(1.2f, 0.12f, 0.45f), template, false);
        Move(root, WhiteHiltShipUpgrades.ChestName, At(1.6f, 4.38f, -5.4f), PoopTilt);
        Move(root, WhiteHiltShipUpgrades.BrazierName, At(-1.65f, 2.5f, 5.85f));
        Move(root, WhiteHiltShipUpgrades.AnchorName, At(3.05f, 3.6f, 8.6f));
        Move(root, WhiteHiltShipUpgrades.MastWispName, At(0f, 21.15f, 0.63f));
        Move(root, ShipPortal.ObjectName, PortalPosition, QuarterdeckTilt);
        Move(root, "TraderLamp", At(0f, 4.8f, 0.93f));
        AddLamps(root);
        FitWaterEffects(root);
        if (!VisualHelper.IsHeadless) AddWaterMask(root, geometry);
        PlaceSeats(root);
        Move(root, "piece_chest", At(0.1f, 2.5f, 5.3f));
        Container hold = prefab.GetComponentsInChildren<Container>(true).First(container => container.GetComponent<ShipChest>() == null);
        hold.transform.position = root.TransformPoint(At(0.1f, 2.5f, 5.3f));
        foreach (Renderer renderer in hold.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        AddPlank(geometry, "CargoHatch", new Vector3(0.1f, 2.52f, 5.3f), new Vector3(1.4f, 0.04f, 1.4f), template, false);
        RelocateUpgrades(root);
        for (int segment = 0; segment < 16; segment++)
        {
            float angle = (segment + 0.5f) * Mathf.PI * 2f / 16f;
            Transform rail = AddBox(geometry, "LookoutRail", new Vector3(Mathf.Cos(angle) * 1.45f, 19.35f,
                0.63f + Mathf.Sin(angle) * 1.45f), new Vector3(0.6f, 1.1f, 0.08f));
            rail.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
        }
        Transform ladder = AddBox(geometry, "MastLadderInteraction", new Vector3(0.55f, 10.6f, 0.63f), new Vector3(0.75f, 16.4f, 0.12f));
        ladder.gameObject.AddComponent<global::BrudvikWhiteHilt.Pieces.Defenses.DefenseLadder>().m_stops = new[]
        {
            At(0.55f, 2.55f, 1.0f), At(-0.55f, 11.85f, 0.63f), At(-0.7f, 18.85f, 0.63f)
        };
        // Amidships at the gap in the port rail; the hull bulges to 3.5 m at the waterline, so the ladder hangs outside it.
        Transform boarding = AddBox(geometry, "BoardingLadder", new Vector3(-3.78f, 0.95f, 1.9f), new Vector3(0.3f, 4.9f, 0.8f));
        for (int side = -1; side <= 1; side += 2)
        {
            AddPlank(geometry, "BoardingLadderRail", new Vector3(-3.78f, 0.95f, 1.9f + side * 0.32f),
                new Vector3(0.08f, 4.9f, 0.08f), template, false);
            AddPlank(geometry, "BoardingLadderHook", new Vector3(-3.265f, 2.65f, 1.9f + side * 0.32f),
                new Vector3(1.03f, 0.08f, 0.08f), template, false);
            AddPlank(geometry, "BoardingLadderBracket", new Vector3(-3.44f, -0.7f, 1.9f + side * 0.32f),
                new Vector3(0.68f, 0.08f, 0.08f), template, false);
        }
        for (int rung = 0; rung < 14; rung++)
            AddPlank(geometry, "BoardingLadderRung", new Vector3(-3.78f, -1.3f + rung * 0.3f, 1.9f),
                new Vector3(0.08f, 0.06f, 0.7f), template, false);
        // The lower stop lies below a swimmer's feet, so Use from the water climbs straight to the deck.
        boarding.gameObject.AddComponent<global::BrudvikWhiteHilt.Pieces.Defenses.DefenseLadder>().m_stops = new[]
        {
            At(-4.3f, -2.8f, 1.9f), At(-2.2f, 2.55f, 1.9f)
        };
        if (!VisualHelper.IsHeadless)
        {
            GameObject net = new("SkidbladnirFishingNet");
            net.transform.SetParent(root, false);
            net.transform.localPosition = At(3.5f, 1.7f, 4f);
            // The net's length is its local x; turned a quarter it hangs along the hull.
            net.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
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

    // The lower room inside its walls, bulkheads, floor and the waist deck (from the asset's colliders), in the
    // structure's space: what the water mask fills.
    private static readonly Bounds LowerRoomInside = new(new Vector3(0f, 0.85f, 2f), new Vector3(5.44f, 2.86f, 10.96f));

    /// <summary>
    /// Keeps the sea out of the lower room in waves, seen from the deck: a box filling the room, drawn with the
    /// longship's own water mask material, which hides the water surface behind it. The longship's mask does not fit
    /// this hull. From inside the room <see cref="LowerDeckWater"/> hides the water instead, as no mask can from within.
    /// </summary>
    private static void AddWaterMask(Transform root, Transform geometry)
    {
        Material mask = root.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => renderer.name.IndexOf("mask", StringComparison.OrdinalIgnoreCase) >= 0
                || (renderer.sharedMaterial?.shader?.name?.IndexOf("mask", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0)
            .Select(renderer => renderer.sharedMaterial).FirstOrDefault(material => material != null);
        if (mask == null)
        {
            Jotunn.Logger.LogWarning("The longship has no water mask; Skidbladnir's lower room gets none.");
            return;
        }

        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
        box.name = "SkidbladnirWaterMask";
        box.layer = root.gameObject.layer;
        box.transform.SetParent(geometry, false);
        box.transform.localPosition = LowerRoomInside.center;
        box.transform.localScale = LowerRoomInside.size;
        MeshRenderer renderer = box.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = mask;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // The prefab sits in Jotunn's inactive container, so parent lookups must include inactive objects.
    private static bool IsUpgradePart(Transform root, Transform part) =>
        part.IsChildOf(root.Find("ship/visual/Customize"))
        || part.GetComponentInParent<ShipChest>(true) != null || part.GetComponentInParent<ShipPortal>(true) != null
        || part.GetComponentInParent<Container>(true) != null
        || part.GetComponentsInParent<Transform>(true).Any(parent => UpgradeObjects.Contains(parent.name));

    private static void PlaceSeats(Transform root)
    {
        Transform interactive = root.Find("interactive");
        Transform template = interactive?.Find("sit_box (3)");
        if (template == null) return;
        UnityEngine.Object.Instantiate(template.gameObject, interactive).name = "sit_box (5)";
        foreach (var seat in Seats)
        {
            Transform target = interactive.Find(seat.name);
            if (target == null) continue;
            target.localPosition = At(seat.at.x, seat.at.y, seat.at.z);
            target.localRotation = Quaternion.Euler(0f, seat.yaw, 0f);
            Enable(target);
        }
        Transform mast = interactive.Find("mast");
        if (mast != null)
        {
            mast.localPosition = At(MastHoldfast.x, MastHoldfast.y, MastHoldfast.z);
            Enable(mast);
        }
        Transform bow = interactive.Find("front");
        if (bow != null)
        {
            bow.localPosition = At(BowHoldfast.x, BowHoldfast.y, BowHoldfast.z);
            Transform stand = bow.Find("attachpoint");
            if (stand != null) stand.SetPositionAndRotation(root.TransformPoint(At(BowHoldfastStand.x, BowHoldfastStand.y, BowHoldfastStand.z)), root.rotation);
            Enable(bow);
        }
    }

    private static void Enable(Transform target)
    {
        foreach (Collider collider in target.GetComponentsInChildren<Collider>(true)) collider.enabled = true;
        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
    }

    private static void AddLamps(Transform root)
    {
        Transform lamp = root.Find("ship/visual/Customize/TraderLamp");
        if (lamp == null || VisualHelper.IsHeadless) return;
        // Copies hang under the main lamp so the lantern upgrade shows, lights and switches them all together.
        List<GameObject> copies = ExtraLamps.Select(spot =>
        {
            GameObject copy = UnityEngine.Object.Instantiate(lamp.gameObject, lamp.parent);
            copy.name = "SkidbladnirLamp";
            copy.transform.position = root.TransformPoint(At(spot.x, spot.y, spot.z));
            return copy;
        }).ToList();
        foreach (GameObject copy in copies) copy.transform.SetParent(lamp, true);
    }

    private static void FitWaterEffects(Transform root)
    {
        foreach (string name in new[] { "watereffects", "ashdamageeffects" })
        {
            Transform effects = root.Find(name);
            if (effects == null) continue;
            Transform surface = effects.Find("WaterSurface");
            foreach (Transform child in effects.GetComponentsInChildren<Transform>(true))
            {
                if (child == effects || (surface != null && child != surface && child.IsChildOf(surface))) continue;
                Vector3 position = child.localPosition;
                if (Mathf.Approximately(position.x, 0f) && Mathf.Approximately(position.z, 0f) && child != surface) continue;
                child.localPosition = new Vector3(position.x * WaterlineBeamRatio, position.y,
                    (position.z - LongshipWaterlineCentre) * WaterlineLengthRatio + WaterlineCentre);
            }
            if (surface != null)
                surface.localScale = Vector3.Scale(surface.localScale, new Vector3(WaterlineBeamRatio, 1f, WaterlineLengthRatio));
        }
        Transform rudder = root.Find("watereffects/SpeedWake/rudder");
        if (rudder != null) rudder.localPosition = new Vector3(0f, rudder.localPosition.y, RudderWakeZ);
    }

    private static void Move(Transform root, string name, Vector3 position, Quaternion? tilt = null)
    {
        Transform target = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == name);
        if (target == null) return;
        if (name != "TraderLamp") target.SetParent(root, true);
        target.position = root.TransformPoint(position);
        if (tilt.HasValue) target.localRotation = tilt.Value * target.localRotation;
    }

    private static void RelocateUpgrades(Transform root)
    {
        Transform customize = root.Find("ship/visual/Customize");
        // The longship's trader dressing is switched off as a whole; the upgrades switch the lantern, barrels and tent.
        customize.gameObject.SetActive(true);
        Transform lantern = customize.Find("TraderLamp");
        if (lantern != null) lantern.gameObject.SetActive(false);
        Transform storage = customize.Find("storage");
        if (storage != null)
        {
            // The rail shields would stretch the fitted group along the whole hull.
            // The lone barrel stands 1.5 m off the rest and would halve the fitted size.
            foreach (Transform shield in storage.Cast<Transform>().Where(child => child.name.StartsWith("Shield") || child.name == "barrell (1)").ToArray())
                UnityEngine.Object.DestroyImmediate(shield.gameObject);
            // Along the port rail under the tent, between the boarding ladder and the brazier.
            FitGroup(root, storage, At(-1.97f, 2.5f, 3.9f), new Vector2(1.4f, 2.9f));
            foreach (Transform barrel in storage) barrel.gameObject.SetActive(false);
        }
        Transform[] parts = customize.Cast<Transform>().Where(child => child.name.StartsWith("ShipTen")
            && child.name != "ShipTentLeft" && child.name != "ShipTentRight").ToArray();
        if (parts.Length == 0) return;
        Transform tent = new GameObject("ShipTen2_Skidbladnir").transform;
        tent.SetParent(customize, false);
        // Aligned with the ship, so its length can be scaled along the ridge alone.
        tent.rotation = root.rotation;
        foreach (Transform part in parts) part.SetParent(tent, true);
        Transform colliders = root.Find(ShipTentColliders.ObjectName);
        if (colliders != null)
        {
            // Switched with the tent group from here on.
            colliders.SetParent(tent, true);
            colliders.gameObject.SetActive(true);
        }
        // Longship width and height, shortened to fit between the boarding ladder's gap (z 2.4) and the foot of the forecastle stair (z 5.9).
        FitGroup(root, tent, At(0f, 2.5f, 4.1f), new Vector2(6.4f, 3.4f), true);
        tent.gameObject.SetActive(false);
        WhiteHiltShipUpgrades upgrades = root.GetComponent<WhiteHiltShipUpgrades>();
        upgrades.m_tentCenter = At(0f, 3.7f, 4.1f);
        upgrades.m_tentSize = new Vector3(5.4f, 2.4f, 3.25f);
    }

    private static void FitGroup(Transform root, Transform group, Vector3 position, Vector2 footprint, bool squashLength = false)
    {
        Vector3[] points = group.GetComponentsInChildren<MeshFilter>(true).Where(filter => filter.sharedMesh != null)
            .SelectMany(filter => Enumerable.Range(0, 8).Select(corner => filter.transform.TransformPoint(filter.sharedMesh.bounds.center
                + Vector3.Scale(filter.sharedMesh.bounds.extents, new Vector3((corner & 1) == 0 ? -1f : 1f,
                    (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f)))))
            .Select(root.InverseTransformPoint).ToArray();
        if (points.Length == 0) return;
        Bounds bounds = new(points[0], Vector3.zero);
        foreach (Vector3 point in points) bounds.Encapsulate(point);
        float scale = Mathf.Min(1f, footprint.x / bounds.size.x);
        if (!squashLength) scale = Mathf.Min(scale, footprint.y / bounds.size.z);
        // Squashing scales the group's own z, which the caller has aligned with the ship's length.
        float length = squashLength ? Mathf.Min(1f, footprint.y / (bounds.size.z * scale)) : 1f;
        Vector3 basePoint = new(bounds.center.x, bounds.min.y, bounds.center.z);
        Vector3 localBase = group.InverseTransformPoint(root.TransformPoint(basePoint));
        group.localScale = Vector3.Scale(group.localScale * scale, new Vector3(1f, 1f, length));
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
        /// <summary>Hinge of the turning rudder.</summary>
        public Rudder rudder { get; set; }
    }
    /// <summary>Where the rudder hangs on the sternpost.</summary>
    public class Rudder
    {
        /// <summary>Lowest hinge point in model metres.</summary>
        public float[] hinge { get; set; }
        /// <summary>Lean of the sternpost in degrees; negative tips the top aft.</summary>
        public float tilt { get; set; }
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
        /// <summary>Whether this group is the rudder, modelled upright around its hinge.</summary>
        public bool rudder { get; set; }
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
        /// <summary>Ship-space direction of the box's local forward axis, or null when axis-aligned.</summary>
        public float[] forward { get; set; }
        /// <summary>Ship-space direction of the box's local up axis.</summary>
        public float[] up { get; set; }
    }
    /// <summary>A convex platform slab.</summary>
    public class Prism
    {
        /// <summary>Six vertices of one triangular platform slab.</summary>
        public float[] vertices { get; set; }
    }
}