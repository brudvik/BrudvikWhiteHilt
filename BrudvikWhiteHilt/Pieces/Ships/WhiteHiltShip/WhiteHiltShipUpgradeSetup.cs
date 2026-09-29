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
    private const float AnchorOutside = 0.12f;
    private const float AnchorRingBelowRail = 0.15f;
    private const float RailBand = 0.4f;

    // On the foredeck planks, in front of the tent (which ends about 2.7 m before the mast). Measured on the longship.
    private static readonly Vector3 BrazierPosition = new(0f, 0.73f, 4.2f);

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

            BrightenLantern(ship.transform);

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

    // Surt's Brazier model on the foredeck, in front of the tent, with the campfire's flames and warmth.
    private static void AddBrazier(Transform root)
    {
        Renderer template = root.Find("ship/visual")?.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(renderer => renderer.sharedMaterial != null)
            ?? throw new InvalidOperationException("no hull renderer under ship/visual");
        Mesh mesh = ForagingAssets.LoadMesh("eternalfire");

        GameObject brazier = new(WhiteHiltShipUpgrades.BrazierName);
        brazier.transform.SetParent(root, false);
        brazier.transform.localPosition = BrazierPosition;

        float scale = BrazierHeight / mesh.bounds.size.y;
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
        Renderer template = root.Find("ship/visual")?.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(renderer => renderer.sharedMaterial != null)
            ?? throw new InvalidOperationException("no hull renderer under ship/visual");
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

    // The vanilla trader lamp is dim for a whole deck; its flicker keeps whatever intensity the light starts with.
    private static void BrightenLantern(Transform root)
    {
        Light light = root.Find("ship/visual/Customize/TraderLamp")?.GetComponentInChildren<Light>(true);
        if (light == null)
        {
            Jotunn.Logger.LogWarning("White Hilt Ship: the lantern light was not found");
            return;
        }

        float brightness = WhiteHiltConfig.BindLocal("Ships", "LanternBrightness", 2.5f,
            "How many times brighter the Ship Lantern shines than the vanilla lamp. Needs a restart.").Value;
        float reach = WhiteHiltConfig.BindLocal("Ships", "LanternRange", 2f,
            "How many times further the Ship Lantern reaches than the vanilla lamp. Needs a restart.").Value;
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
        Renderer template = root.Find("ship/visual")?.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(renderer => renderer.sharedMaterial != null)
            ?? throw new InvalidOperationException("no hull renderer under ship/visual");
        Mesh mesh = ForagingAssets.LoadMesh("shipanchor");
        Vector3 ring = FindStarboardRail(root);

        GameObject anchor = new(WhiteHiltShipUpgrades.AnchorName);
        anchor.transform.SetParent(root, false);
        anchor.transform.localPosition = new Vector3(ring.x + AnchorOutside, ring.y - AnchorRingBelowRail - AnchorHeight, ring.z);

        // The anchor is flat along z; turned a quarter, its broad side faces out from the hull.
        Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);
        float scale = AnchorHeight / mesh.bounds.size.y;
        Vector3 pivot = -(rotation * (new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z) * scale));
        VisualHelper.CreateModel(anchor.transform, mesh, ForagingAssets.LoadTexture("shipanchor_albedo"), template, pivot, rotation, scale);
        anchor.SetActive(false);
    }

    // The top of the starboard rail near the bow, in ship root space, read from the hull mesh: the widest vertex in a
    // band across the hull, and the highest vertex along that side.
    private static Vector3 FindStarboardRail(Transform root)
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
        return new Vector3(x, y, z);
    }

    private static float Footprint(Transform root, MeshFilter filter)
    {
        Vector3 size = root.InverseTransformVector(filter.transform.TransformVector(filter.sharedMesh.bounds.size));
        return Mathf.Abs(size.x * size.z);
    }
}
