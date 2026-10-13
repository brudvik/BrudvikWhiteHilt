using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.Ballista;

/// <summary>
/// Builds the ballista part of a ship prefab: the vanilla ballista's own meshes with a hover box and a place for the
/// gunner to stand. The part stays switched off until the ship has the upgrade. Its place on the deck is found when it
/// is first shown (see <see cref="ShipBallista"/>), so the same part serves the White Hilt Ship and Skidbladnir.
/// </summary>
internal static class BallistaModel
{
    // The vanilla ballista, whose Base, Neck, Body and Bolt meshes are copied. Its network, wear, targeting and
    // colliders are left behind: the ship's own network object carries the part.
    private const string TemplatePrefab = "piece_turret";

    // The vanilla ballista is about 2.5 m long, which is much for the forecastle; this is the longest side of the copy.
    private const float Size = 2f;

    // Where the gunner stands: this far behind the back of the ballista.
    private const float GunnerBehind = 0.6f;

    /// <summary>
    /// Adds the ballista part to a ship prefab. A failure leaves the ship without it and logs a warning.
    /// </summary>
    /// <param name="root">The ship prefab's root.</param>
    /// <returns>The ballista on the new part, or null when the part could not be built.</returns>
    internal static ShipBallista Build(Transform root)
    {
        GameObject part = new(ShipBallista.ObjectName) { layer = WhiteHiltShipUpgradeSetup.CrateLayer(root) };
        part.transform.SetParent(root, false);
        try
        {
            ShipBallista ballista = part.AddComponent<ShipBallista>();
            Vector3 behind = new(0f, 0f, -(Size / 2f + GunnerBehind));
            if (!VisualHelper.IsHeadless)
            {
                behind = AddLook(part, ballista);
            }

            GameObject gunner = new("GunnerPoint");
            gunner.transform.SetParent(part.transform, false);
            gunner.transform.localPosition = behind;
            ballista.m_attachPoint = gunner.transform;
            part.SetActive(false);
            return ballista;
        }
        catch (Exception ex)
        {
            UnityEngine.Object.DestroyImmediate(part);
            Jotunn.Logger.LogWarning($"{root.name}: the ship ballista has no part: {ex.Message}");
            return null;
        }
    }

    // Copies the meshes under the vanilla ballista's New object, keeping its Neck and Body pivots so yaw and pitch can
    // turn them, then fits the copy to Size, stands it on the deck and gives the part a hover box. Returns where the
    // gunner stands, in the part's space. Never runs on a dedicated server, which draws nothing.
    private static Vector3 AddLook(GameObject part, ShipBallista ballista)
    {
        GameObject turret = PrefabManager.Instance.GetPrefab(TemplatePrefab)
            ?? throw new InvalidOperationException($"the vanilla {TemplatePrefab} was not found");
        Transform source = turret.transform.Find("New") ?? throw new InvalidOperationException($"{TemplatePrefab}/New not found");

        Transform visual = new GameObject("Visual").transform;
        visual.SetParent(part.transform, false);
        CopyMesh(Find(source, "Base"), visual);
        Transform neckPivot = CopyEmpty(Find(source, "NeckRotation"), visual);
        CopyMesh(Find(source, "NeckRotation/Neck"), neckPivot);
        Transform bodyPivot = CopyEmpty(Find(source, "BodyRotation"), visual);
        CopyMesh(Find(source, "BodyRotation/Body"), bodyPivot);
        Transform bolt = CopyMesh(Find(source, "BodyRotation/Bolt Black Metal"), bodyPivot);
        Transform muzzle = CopyEmpty(Find(source, "BodyRotation/Eye"), bodyPivot);

        // Fitted without the loaded bolt, which sticks out in front. The Visual is still unscaled here.
        Bounds bounds = default;
        bool first = true;
        foreach (MeshFilter filter in visual.GetComponentsInChildren<MeshFilter>(true).Where(filter => filter.transform != bolt))
        {
            Bounds mesh = WhiteHiltShipUpgradeSetup.RootBounds(visual, filter);
            if (first)
            {
                bounds = mesh;
                first = false;
            }
            else
            {
                bounds.Encapsulate(mesh);
            }
        }

        float scale = Size / Mathf.Max(bounds.size.x, bounds.size.z);
        visual.localScale = Vector3.one * scale;
        visual.localPosition = new Vector3(0f, -bounds.min.y * scale, 0f);

        BoxCollider box = part.AddComponent<BoxCollider>();
        box.center = new Vector3(bounds.center.x * scale, bounds.size.y * scale / 2f, bounds.center.z * scale);
        box.size = bounds.size * scale;

        ballista.m_neck = neckPivot;
        ballista.m_body = bodyPivot;
        ballista.m_muzzle = muzzle;
        ballista.m_bolt = bolt.gameObject;
        return new Vector3(0f, 0f, box.center.z - box.size.z / 2f - GunnerBehind);
    }

    private static Transform Find(Transform source, string path)
    {
        return source.Find(path) ?? throw new InvalidOperationException($"{TemplatePrefab}/New/{path} not found");
    }

    // An empty object with the original's place under its parent.
    private static Transform CopyEmpty(Transform original, Transform parent)
    {
        Transform copy = new GameObject(original.name).transform;
        copy.SetParent(parent, false);
        copy.localPosition = original.localPosition;
        copy.localRotation = original.localRotation;
        copy.localScale = original.localScale;
        return copy;
    }

    // A copy of one mesh with the original's mesh and materials, and its place under its parent.
    private static Transform CopyMesh(Transform original, Transform parent)
    {
        MeshFilter filter = original.GetComponent<MeshFilter>() ?? throw new InvalidOperationException($"{original.name} has no mesh");
        MeshRenderer renderer = original.GetComponent<MeshRenderer>() ?? throw new InvalidOperationException($"{original.name} has no renderer");
        Transform copy = CopyEmpty(original, parent);
        copy.gameObject.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
        MeshRenderer target = copy.gameObject.AddComponent<MeshRenderer>();
        target.sharedMaterials = renderer.sharedMaterials;
        target.shadowCastingMode = renderer.shadowCastingMode;
        target.receiveShadows = renderer.receiveShadows;
        return copy;
    }
}
