using BrudvikWhiteHilt.Helpers;
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

        if (!VisualHelper.IsHeadless)
        {
            try
            {
                AddMastWisp(ship.transform);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"White Hilt Ship: the mast wisp has no look: {ex.Message}");
            }
        }
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
}
