using BrudvikWhiteHilt.Helpers;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.Ram;

/// <summary>
/// The look of the Ram Prow: an iron-shod stem, made from the vanilla iron wall's plate, on the bow of the White Hilt Ship or
/// Skidbladnir. The part is switched on by the upgrade bit like the other upgrade parts. It is a look only: no collider, so
/// it neither blocks nor adds to the ship's hull. Where it stands is found when it is first shown, from the ship's own
/// colliders, so the same part serves both ships.
/// </summary>
public class RamProw : MonoBehaviour
{
    /// <summary>
    /// Name of the prow object added to the ship prefab.
    /// </summary>
    public const string ObjectName = "WhiteHiltRamProw";

    // The vanilla iron wall piece, whose plate mesh and material are copied. Its network, wear and colliders are left behind.
    private const string TemplatePrefab = "iron_wall_1x1";
    private const string TemplatePlate = "New/default";

    // Where the stem is looked for: the ship's front holdfast gives the height and the length, as it does for the ballista.
    private const string HoldfastPath = "interactive/front";
    private const string HullLayer = "vehicle";
    private const float TopBelowHoldfast = 0.6f;
    private const float BottomBelowHoldfast = 2.4f;
    private const float RayAhead = 4f;
    private const float RayLength = 10f;

    // The prow stands a little proud of the planks, and keeps to the holdfast's place when the stem is not found.
    private const float ProudOfHull = 0.08f;
    private const float FallbackAhead = 0.4f;

    // Plates of the prow, in metres: two side plates that sweep back from the stem at a half-angle, a fin along the stem and a
    // beak on the waterline.
    private const float Height = BottomBelowHoldfast - TopBelowHoldfast;
    private const float SidePlateWidth = 0.9f;
    private const float SweepHalfAngle = 25f;
    private const float PlateThickness = 0.12f;
    private const float FinLength = 0.6f;
    private const float BeakWidth = 0.5f;
    private const float BeakLength = 0.9f;

    /// <summary>
    /// Adds the prow to a ship prefab, switched off until the ship has the upgrade. A dedicated server draws nothing, so it
    /// gets no part; a failure leaves the ship without the look and logs a warning.
    /// </summary>
    /// <param name="root">The ship prefab's root.</param>
    public static void Build(Transform root)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        GameObject part = new(ObjectName) { layer = root.gameObject.layer };
        part.transform.SetParent(root, false);
        try
        {
            GameObject template = PrefabManager.Instance.GetPrefab(TemplatePrefab)
                ?? throw new InvalidOperationException($"the vanilla {TemplatePrefab} was not found");
            Transform source = template.transform.Find(TemplatePlate) ?? throw new InvalidOperationException($"{TemplatePrefab}/{TemplatePlate} not found");
            MeshFilter filter = source.GetComponent<MeshFilter>() ?? throw new InvalidOperationException($"{TemplatePrefab}/{TemplatePlate} has no mesh");
            MeshRenderer renderer = source.GetComponent<MeshRenderer>() ?? throw new InvalidOperationException($"{TemplatePrefab}/{TemplatePlate} has no renderer");

            Transform side = new GameObject("Plates").transform;
            side.SetParent(part.transform, false);
            float inwards = SidePlateWidth / 2f;
            float sweep = SweepHalfAngle * Mathf.Deg2Rad;
            Vector3 back = new(Mathf.Sin(sweep) * inwards, 0f, -Mathf.Cos(sweep) * inwards);
            Vector3 plate = new(SidePlateWidth, Height, PlateThickness);

            // Each side plate lies along a line that sweeps back from the stem: its width runs at 90 degrees less the half-angle.
            AddPlate(side, filter.sharedMesh, renderer, back, Quaternion.Euler(0f, 90f - SweepHalfAngle, 0f), plate);
            AddPlate(side, filter.sharedMesh, renderer, new Vector3(-back.x, 0f, back.z), Quaternion.Euler(0f, SweepHalfAngle - 90f, 0f), plate);
            AddPlate(side, filter.sharedMesh, renderer, new Vector3(0f, 0f, FinLength / 2f), Quaternion.Euler(0f, 90f, 0f),
                new Vector3(FinLength, Height, PlateThickness));
            AddPlate(side, filter.sharedMesh, renderer, new Vector3(0f, -Height / 2f + PlateThickness, BeakLength / 2f - 0.1f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(BeakWidth, BeakLength, PlateThickness));

            part.AddComponent<RamProw>();
            part.SetActive(false);
        }
        catch (Exception ex)
        {
            DestroyImmediate(part);
            Jotunn.Logger.LogWarning($"{root.name}: the ram prow has no look: {ex.Message}");
        }
    }

    // One copy of the iron plate, sized to the given extents (width, height, thickness along its own axes) with its middle on
    // the given point of the part.
    private static void AddPlate(Transform parent, Mesh mesh, MeshRenderer original, Vector3 middle, Quaternion rotation, Vector3 size)
    {
        Bounds bounds = mesh.bounds;
        Vector3 scale = new(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
        GameObject plate = new("Plate");
        plate.transform.SetParent(parent, false);
        plate.transform.localRotation = rotation;
        plate.transform.localScale = scale;
        plate.transform.localPosition = middle - rotation * Vector3.Scale(bounds.center, scale);
        plate.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer target = plate.AddComponent<MeshRenderer>();
        target.sharedMaterials = original.sharedMaterials;
        target.shadowCastingMode = original.shadowCastingMode;
        target.receiveShadows = original.receiveShadows;
    }

    // Shown for the first time: finds the stem and stands the prow on it, on every client.
    private void Start()
    {
        try
        {
            Place();
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{transform.root.name}: the ram prow could not be placed: {ex.Message}");
        }
    }

    // Casts rays towards the ship from ahead of the bow at two heights below the front holdfast, onto the hull's own
    // colliders, and stands the prow on the stem between the two hits, leaning along it. A ship where no hit is found keeps the
    // prow at the holdfast's length, upright.
    private void Place()
    {
        Transform root = transform.parent;
        Transform holdfast = root.Find(HoldfastPath);
        Rigidbody body = root.GetComponent<Rigidbody>();
        if (holdfast == null || body == null)
        {
            return;
        }

        Vector3 hold = root.InverseTransformPoint(holdfast.position);
        float? top = StemAt(root, body, hold, hold.y - TopBelowHoldfast);
        float? bottom = StemAt(root, body, hold, hold.y - BottomBelowHoldfast);
        float topZ = top ?? bottom ?? hold.z + FallbackAhead;
        float bottomZ = bottom ?? top ?? hold.z + FallbackAhead;

        Vector3 upper = new(0f, hold.y - TopBelowHoldfast, topZ);
        Vector3 lower = new(0f, hold.y - BottomBelowHoldfast, bottomZ);
        Vector3 up = (upper - lower).normalized;
        Vector3 outward = Vector3.Cross(Vector3.right, up);
        transform.localRotation = Quaternion.LookRotation(outward, up);
        transform.localPosition = (upper + lower) / 2f + outward * ProudOfHull;
        Jotunn.Logger.LogInfo($"Ram Prow on {root.name}: stem at z {bottomZ:F2} (y {lower.y:F2}) to {topZ:F2} (y {upper.y:F2})" +
            (top == null || bottom == null ? ", partly guessed" : string.Empty));
    }

    // The z of the stem on the ship's centre line at a height, in the ship's space; null when the hull is not found there.
    private static float? StemAt(Transform root, Rigidbody body, Vector3 hold, float height)
    {
        Vector3 origin = root.TransformPoint(new Vector3(0f, height, hold.z + RayAhead));
        RaycastHit[] hits = Physics.RaycastAll(origin, -root.forward, RayLength, LayerMask.GetMask(HullLayer), QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.attachedRigidbody == body)
            {
                return root.InverseTransformPoint(hit.point).z;
            }
        }

        return null;
    }
}
