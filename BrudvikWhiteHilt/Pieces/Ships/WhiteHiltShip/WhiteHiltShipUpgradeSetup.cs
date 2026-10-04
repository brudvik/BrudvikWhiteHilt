using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;
using BrudvikWhiteHilt.Progression;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;

/// <summary>
/// Prepares the White Hilt Ship prefab for its upgrades.
/// </summary>
public static class WhiteHiltShipUpgradeSetup
{
    // Players stand on the deck below the tent cloth, so the sheltered area reaches this far below it.
    private const float TentHeadroom = 2.5f;
    private const float WispAboveMast = 0.15f;
    private const float AnchorHeight = 1.1f;

    // Where the anchor hangs on the starboard rail: towards the bow as a fraction of the hull's half length, just outside
    // the planks, with its ring a little below the rail top.
    private const float AnchorTowardsBow = 0.55f;
    private const float AnchorOutside = 0.03f;
    private const float AnchorRingBelowRail = 0.15f;
    private const float AnchorMaxTilt = 30f;
    private const float RailBand = 0.4f;

    // On the port deck just forward of the cargo crates, out of the walkway; used if the crates are not found.
    private static readonly Vector3 BrazierFallback = new(-0.9f, 0.64f, -2.2f);
    private const float DeckHeight = 0.64f;
    private const float BrazierClearance = 0.15f;

    // On the starboard deck just forward of the helm; the cargo crates stand to port.
    private static readonly Vector3 ChestPosition = new(0.9f, 0.64f, -4.6f);
    private const float BrazierHeight = 0.9f;
    private const float BrazierFlameScale = 0.35f;
    private const float PortalLift = 0.02f;

    // The top of the coals in Surt's Brazier model, as a fraction of its height.
    private const float BrazierCoals = 0.85f;

    private static readonly string[] wispParts = { "demister_ball (2)", "effects", "Particle System Force Field" };

    /// <summary>
    /// Adds the upgrade logic, enlarges the cargo hold, measures the tent and hangs the mast wisp.
    /// Must run on the server too, so the hold size and upgrades match for everyone.
    /// </summary>
    /// <param name="ship">The cloned ship prefab.</param>
    public static void Prepare(GameObject ship)
    {
        WhiteHiltShipUpgrades upgrades = ship.AddComponent<WhiteHiltShipUpgrades>();

        Container container = ship.GetComponentInChildren<Container>(true) ?? throw new InvalidOperationException("the ship has no cargo hold");
        container.m_width = WhiteHiltShipUpgrades.LargeHold.x;
        container.m_height = WhiteHiltShipUpgrades.LargeHold.y;

        Transform mast = ship.transform.Find("ship/colliders/mast") ?? throw new InvalidOperationException("ship/colliders/mast not found");
        foreach (Collider collider in mast.GetComponentsInChildren<Collider>(true))
        {
            collider.gameObject.AddComponent<ShipMastHover>();
        }

        MeasureTent(ship.transform, upgrades);
        AddChest(ship.transform, container);
        Transform portal = AddPortal(ship.transform);
        AddLanternInteraction(ship.transform);

        if (!VisualHelper.IsHeadless)
        {
            try
            {
                AddPortalLook(ship.transform, portal);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"White Hilt Ship: the ship portal has no look: {ex.Message}");
            }

            try
            {
                AddMastWisp(ship.transform);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"White Hilt Ship: the mast wisp has no look: {ex.Message}");
            }

            try
            {
                AddAnchor(ship.transform);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"White Hilt Ship: the drift anchor has no look: {ex.Message}");
            }

            ConfigureLantern(ship.transform);

            try
            {
                AddBrazier(ship.transform);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"White Hilt Ship: the deck brazier has no look: {ex.Message}");
            }
        }
    }

    // Surt's Brazier model on the deck under the tent, with the campfire's flames and warmth.
    private static void AddBrazier(Transform root)
    {
        Renderer template = HullTemplate(root);
        Mesh mesh = ForagingAssets.LoadMesh("eternalfire");

        float scale = BrazierHeight / mesh.bounds.size.y;
        float radius = Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.z) * scale / 2f;

        GameObject brazier = new(WhiteHiltShipUpgrades.BrazierName);
        brazier.transform.SetParent(root, false);
        brazier.transform.localPosition = InFrontOfPortCrates(root, radius);

        Vector3 pivot = -(new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z) * scale);
        VisualHelper.CreateModel(brazier.transform, mesh, ForagingAssets.LoadTexture("eternalfire_albedo"), template, pivot, Quaternion.identity, scale);

        Vector3 coals = Vector3.up * (BrazierCoals * BrazierHeight);
        FireEffects.AddFlames(brazier.transform, "WhiteHiltBrazierFlame", coals, BrazierFlameScale);
        FireEffects.AddWarmth(brazier.transform, "WhiteHiltBrazierWarmth", coals);

        GameObject block = new("collider") { layer = CrateLayer(root) };
        block.transform.SetParent(brazier.transform, false);
        BoxCollider box = block.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, BrazierHeight / 2f, 0f);
        box.size = new Vector3(mesh.bounds.size.x * scale, BrazierHeight, mesh.bounds.size.z * scale);
        brazier.SetActive(false);
    }

    // On the deck just towards the bow from the port-side cargo crates, in ship root space.
    private static Vector3 InFrontOfPortCrates(Transform root, float radius)
    {
        Transform storage = root.Find("ship/visual/Customize/storage");
        Bounds[] crates = storage == null ? Array.Empty<Bounds>() : storage.GetComponentsInChildren<MeshFilter>(true)
            .Where(filter => filter.sharedMesh != null)
            .Select(filter => RootBounds(root, filter))
            .Where(bounds => bounds.center.x < 0f)
            .ToArray();
        if (crates.Length == 0)
        {
            Jotunn.Logger.LogWarning("White Hilt Ship: no port crates found, the brazier uses its fallback spot");
            return BrazierFallback;
        }

        Bounds all = crates[0];
        foreach (Bounds bounds in crates.Skip(1))
        {
            all.Encapsulate(bounds);
        }

        return new Vector3(all.center.x, DeckHeight, all.max.z + BrazierClearance + radius);
    }

    private static Bounds RootBounds(Transform root, MeshFilter filter)
    {
        Bounds mesh = filter.sharedMesh.bounds;
        Bounds result = new(root.InverseTransformPoint(filter.transform.TransformPoint(mesh.center)), Vector3.zero);
        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 local = mesh.center + Vector3.Scale(mesh.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
            result.Encapsulate(root.InverseTransformPoint(filter.transform.TransformPoint(local)));
        }

        return result;
    }

    // The deck portal: a thin box on the deck planks that players hover and stand on, on the layer of the deck crates.
    // The collider is off until the upgrade is on.
    private static Transform AddPortal(Transform root)
    {
        GameObject portal = new(ShipPortal.ObjectName) { layer = CrateLayer(root) };
        portal.transform.SetParent(root, false);
        portal.transform.localPosition = ShipPortal.DeckPosition;

        BoxCollider area = portal.AddComponent<BoxCollider>();
        area.center = new Vector3(0f, 0.03f, 0f);
        area.size = new Vector3(ShipPortal.Diameter * 0.9f, 0.06f, ShipPortal.Diameter * 0.9f);
        area.enabled = false;
        portal.AddComponent<ShipPortal>();
        return portal.transform;
    }

    // The rune circle of the ground portal, small enough for the deck.
    private static void AddPortalLook(Transform root, Transform portal)
    {
        Renderer template = HullTemplate(root);
        GameObject visual = new("visual");
        visual.transform.SetParent(portal, false);
        WhiteHiltGroundPortal.AddRuneCircle(visual.transform, template, ShipPortal.Diameter, PortalLift);
        visual.SetActive(false);
    }

    // A copy of one of the longship's deck crates by the helm, made into a second chest on the ship's network object.
    private static void AddChest(Transform root, Container hold)
    {
        Transform crate = root.Find("ship/visual/Customize/storage")?.Cast<Transform>().FirstOrDefault(child => child.GetComponent<MeshFilter>() != null)
            ?? throw new InvalidOperationException("no crate under Customize/storage");

        GameObject chest = UnityEngine.Object.Instantiate(crate.gameObject);
        chest.SetActive(false);
        chest.name = WhiteHiltShipUpgrades.ChestName;
        chest.transform.SetParent(root, false);
        chest.transform.localPosition = ChestPosition;
        chest.transform.localRotation = Quaternion.Inverse(root.rotation) * crate.rotation;
        chest.transform.localScale = crate.lossyScale;

        chest.AddComponent<ShipChest>();
        Container container = chest.AddComponent<Container>();
        container.m_name = "$item_whitehiltshipchest";
        container.m_width = 4;
        container.m_height = 2;
        container.m_bkg = hold.m_bkg;
        container.m_openEffects = hold.m_openEffects;
        container.m_closeEffects = hold.m_closeEffects;
        container.m_privacy = Container.PrivacySetting.Public;
        container.m_rootObjectOverride = root.GetComponent<ZNetView>();
    }

    private static void AddLanternInteraction(Transform root)
    {
        Transform lamp = root.Find("ship/visual/Customize/TraderLamp");
        if (lamp == null)
            return;
        Bounds[] meshes = lamp.GetComponentsInChildren<MeshFilter>(true)
            .Where(filter => filter.sharedMesh != null)
            .Select(filter => RootBounds(lamp, filter)).ToArray();
        if (meshes.Length == 0)
        {
            Jotunn.Logger.LogWarning("White Hilt Ship: no lantern mesh found for interaction");
            return;
        }
        Bounds bounds = meshes[0];
        foreach (Bounds mesh in meshes.Skip(1))
            bounds.Encapsulate(mesh);
        GameObject target = new("WhiteHiltLanternSwitch") { layer = CrateLayer(root) };
        target.transform.SetParent(lamp, false);
        BoxCollider collider = target.AddComponent<BoxCollider>();
        collider.center = bounds.center;
        collider.size = bounds.size;
        target.AddComponent<ShipLanternHover>();
    }

    private static void ConfigureLantern(Transform root)
    {
        Light light = root.Find("ship/visual/Customize/TraderLamp")?.GetComponentInChildren<Light>(true);
        if (light == null)
        {
            Jotunn.Logger.LogWarning("White Hilt Ship: the lantern light was not found");
            return;
        }

        float brightness = ShipSettings.LanternBrightness.Value;
        float reach = ShipSettings.LanternRange.Value;
        light.intensity *= Mathf.Max(0f, brightness);
        light.range *= Mathf.Max(0.1f, reach);
    }

    private static void MeasureTent(Transform root, WhiteHiltShipUpgrades upgrades)
    {
        Transform customize = root.Find("ship/visual/Customize") ?? throw new InvalidOperationException("Customize not found");
        MeshFilter[] cloth = customize.Cast<Transform>()
            .Where(child => child.name.StartsWith("ShipTen2"))
            .SelectMany(child => child.GetComponentsInChildren<MeshFilter>(true))
            .Where(filter => filter.sharedMesh != null)
            .ToArray();
        if (cloth.Length == 0)
        {
            throw new InvalidOperationException("no tent cloth found");
        }

        Bounds area = default;
        bool first = true;
        foreach (MeshFilter filter in cloth)
        {
            Bounds mesh = filter.sharedMesh.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 local = mesh.center + Vector3.Scale(mesh.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                Vector3 point = root.InverseTransformPoint(filter.transform.TransformPoint(local));
                if (first)
                {
                    area = new Bounds(point, Vector3.zero);
                    first = false;
                }
                else
                {
                    area.Encapsulate(point);
                }
            }
        }

        area.min = new Vector3(area.min.x, area.min.y - TentHeadroom, area.min.z);
        upgrades.m_tentCenter = area.center;
        upgrades.m_tentSize = area.size;

        try
        {
            // The beam under the ridge is not part of the cloth's surface.
            Transform beam = customize.Find("ShipTen2_beam");
            ShipTentColliders.Build(root, cloth.Where(filter => beam == null || !filter.transform.IsChildOf(beam)), CrateLayer(root));
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"White Hilt Ship: the tent cannot be stood on: {ex.Message}");
        }
    }

    // The hull's renderer, whose material and shadow mode the added models copy. The first renderer under ship/visual
    // is the mast's inactive cloth-collider helper, not a visible part of the ship.
    private static Renderer HullTemplate(Transform root)
    {
        Renderer hull = root.Find("ship/visual/hull_new/hull")?.GetComponent<MeshRenderer>();
        if (hull != null && hull.sharedMaterial != null)
        {
            return hull;
        }

        return root.Find("ship/visual")?.GetComponentsInChildren<MeshRenderer>()
            .FirstOrDefault(renderer => renderer.sharedMaterial != null && renderer.sharedMaterial.mainTexture != null)
            ?? throw new InvalidOperationException("no hull renderer under ship/visual");
    }

    // Deck parts block players like the longship's own crates do.
    private static int CrateLayer(Transform root)
    {
        Transform crate = root.Find("ship/visual/Customize/storage")?.GetComponentsInChildren<Collider>(true).FirstOrDefault()?.transform;
        return crate != null ? crate.gameObject.layer : root.gameObject.layer;
    }

    // The light, particles and mist force field of the vanilla wisplight ball, without its network root.
    private static void AddMastWisp(Transform root)
    {
        MeshFilter mast = root.GetComponentsInChildren<MeshFilter>(true)
            .FirstOrDefault(filter => filter.name == "mast" && filter.sharedMesh != null)
            ?? throw new InvalidOperationException("the mast mesh was not found");
        GameObject demister = PrefabManager.Instance.GetPrefab("Demister") ?? throw new InvalidOperationException("the vanilla Demister was not found");

        Bounds bounds = mast.sharedMesh.bounds;
        GameObject wisp = new(WhiteHiltShipUpgrades.MastWispName);
        wisp.transform.SetParent(mast.transform, false);
        wisp.transform.localPosition = new Vector3(bounds.center.x, bounds.max.y + WispAboveMast, bounds.center.z);

        // The ship is scaled up; cancel that so the force field keeps the vanilla 15 m reach the mist code expects.
        Vector3 parentScale = mast.transform.lossyScale;
        wisp.transform.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z);

        foreach (string partName in wispParts)
        {
            Transform part = demister.transform.Find(partName) ?? throw new InvalidOperationException($"Demister/{partName} not found");
            GameObject copy = UnityEngine.Object.Instantiate(part.gameObject, wisp.transform);
            copy.name = part.name;
            copy.transform.localPosition = part.localPosition;
            copy.transform.localRotation = part.localRotation;
            copy.transform.localScale = part.localScale;
        }

        wisp.SetActive(false);
    }

    // The Harbour Anchor model, hung on the outside of the starboard rail near the bow. The ship root's +z is the bow.
    private static void AddAnchor(Transform root)
    {
        Renderer template = HullTemplate(root);
        Mesh mesh = ForagingAssets.LoadMesh("shipanchor");
        Vector3 ring = FindStarboardRail(root, out float tilt);

        // Hung from its ring and tilted so it lies along the hull as the planks slope in below the rail.
        GameObject anchor = new(WhiteHiltShipUpgrades.AnchorName);
        anchor.transform.SetParent(root, false);
        anchor.transform.localPosition = new Vector3(ring.x + AnchorOutside, ring.y - AnchorRingBelowRail, ring.z);
        anchor.transform.localRotation = Quaternion.Euler(0f, 0f, -tilt);

        // The anchor is flat along z; turned a quarter, its broad side faces out from the hull.
        Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);
        float scale = AnchorHeight / mesh.bounds.size.y;
        Vector3 pivot = -(rotation * (new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z) * scale)) + Vector3.down * AnchorHeight;
        VisualHelper.CreateModel(anchor.transform, mesh, ForagingAssets.LoadTexture("shipanchor_albedo"), template, pivot, rotation, scale);
        anchor.SetActive(false);
    }

    // The top of the starboard rail near the bow, in ship root space, read from the hull mesh: the widest vertex in a
    // band across the hull, and the highest vertex along that side. Tilt is the hull's inward slope over the anchor's
    // height, in degrees.
    private static Vector3 FindStarboardRail(Transform root, out float tilt)
    {
        Transform visual = root.Find("ship/visual") ?? throw new InvalidOperationException("ship/visual not found");
        Transform tent = visual.Find("Customize");
        MeshFilter hull = visual.GetComponentsInChildren<MeshFilter>(true)
            .Where(filter => filter.sharedMesh != null && filter.sharedMesh.isReadable && (tent == null || !filter.transform.IsChildOf(tent)))
            .OrderByDescending(filter => Footprint(root, filter))
            .FirstOrDefault()
            ?? throw new InvalidOperationException("no readable hull mesh under ship/visual");

        Vector3[] points = hull.sharedMesh.vertices.Select(vertex => root.InverseTransformPoint(hull.transform.TransformPoint(vertex))).ToArray();
        float minZ = points.Min(point => point.z);
        float maxZ = points.Max(point => point.z);
        float z = (minZ + maxZ) / 2f + (maxZ - minZ) / 2f * AnchorTowardsBow;
        Vector3[] band = points.Where(point => Mathf.Abs(point.z - z) < RailBand).ToArray();
        if (band.Length == 0)
        {
            throw new InvalidOperationException("the hull mesh has no vertices near the anchor");
        }

        float x = band.Max(point => point.x);
        float y = band.Where(point => point.x > x - RailBand).Max(point => point.y);

        float bottom = y - AnchorRingBelowRail - AnchorHeight;
        Vector3[] low = band.Where(point => point.x > 0f && Mathf.Abs(point.y - bottom) < RailBand).ToArray();
        tilt = low.Length == 0 ? 0f
            : Mathf.Clamp(Mathf.Atan2(x - low.Max(point => point.x), y - bottom) * Mathf.Rad2Deg, 0f, AnchorMaxTilt);
        return new Vector3(x, y, z);
    }

    private static float Footprint(Transform root, MeshFilter filter)
    {
        Vector3 size = root.InverseTransformVector(filter.transform.TransformVector(filter.sharedMesh.bounds.size));
        return Mathf.Abs(size.x * size.z);
    }
}
