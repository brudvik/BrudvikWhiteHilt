using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Rock over the ground: the static_solid layer the zone generator's block check sees. Objects placed from the world's
/// network data only know the other objects' positions, and the Mistlands' cliffs and arches reach tens of metres from
/// theirs, so a check by distance alone put things under them.
/// </summary>
public static class RockCover
{
    private const float CheckHeight = 200f;

    private static readonly Dictionary<int, Bounds[]> footprints = new();
    private static readonly RaycastHit[] hits = new RaycastHit[16];
    private static int rockLayer = -1;
    private static int rockMask;

    /// <summary>
    /// Whether an object's rock covers a point, from its network data: its prefab's rock colliders, turned and scaled as
    /// the object is. Boxes round each collider, so the inside of an arch counts as covered.
    /// </summary>
    /// <param name="zdo">The object.</param>
    /// <param name="point">The point.</param>
    /// <param name="margin">Metres the point must also keep from the rock.</param>
    /// <returns>True when the point lies under or within the rock.</returns>
    public static bool Covers(ZDO zdo, Vector3 point, float margin)
    {
        Bounds[] parts = Footprint(zdo.GetPrefab());
        if (parts.Length == 0)
        {
            return false;
        }

        // As ZNetView reads it: a scale vector, else a single factor, else the prefab's own.
        Vector3 scale = zdo.GetVec3(ZDOVars.s_scaleHash, Vector3.zero);
        if (scale == Vector3.zero)
        {
            float uniform = zdo.GetFloat(ZDOVars.s_scaleScalarHash, ZNetScene.instance.GetPrefab(zdo.GetPrefab()).transform.localScale.x);
            scale = new Vector3(uniform, uniform, uniform);
        }

        if (scale.x <= 0f || scale.z <= 0f)
        {
            return false;
        }

        Vector3 local = Quaternion.Inverse(zdo.GetRotation()) * (point - zdo.GetPosition());
        local = new Vector3(local.x / scale.x, local.y, local.z / scale.z);
        float marginX = margin / scale.x;
        float marginZ = margin / scale.z;
        foreach (Bounds part in parts)
        {
            if (Mathf.Abs(local.x - part.center.x) <= part.extents.x + marginX && Mathf.Abs(local.z - part.center.z) <= part.extents.z + marginZ)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether loaded rock lies over a point. Rock that has not loaded yet is not seen, and neither are player pieces,
    /// trees or loose objects, so nothing under a roof counts as covered.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <param name="self">An object whose own colliders do not count, or null.</param>
    /// <returns>True when rock lies over the point.</returns>
    public static bool IsUnder(Vector3 point, Transform self)
    {
        if (rockMask == 0)
        {
            rockMask = LayerMask.GetMask("static_solid");
        }

        int count = Physics.RaycastNonAlloc(point + Vector3.up * CheckHeight, Vector3.down, hits, CheckHeight - 0.1f, rockMask);
        for (int i = 0; i < count; i++)
        {
            Collider collider = hits[i].collider;
            if (collider != null && !collider.attachedRigidbody && (self == null || !collider.transform.IsChildOf(self)))
            {
                return true;
            }
        }

        return false;
    }

    // The boxes, in the prefab's own space, round its solid colliders on the rock layer; cached per prefab.
    private static Bounds[] Footprint(int hash)
    {
        if (footprints.TryGetValue(hash, out Bounds[] parts))
        {
            return parts;
        }

        if (rockLayer < 0)
        {
            rockLayer = LayerMask.NameToLayer("static_solid");
        }

        List<Bounds> found = new();
        GameObject prefab = ZNetScene.instance.GetPrefab(hash);
        if (prefab != null)
        {
            foreach (Collider collider in prefab.GetComponentsInChildren<Collider>(true))
            {
                if (collider.isTrigger || collider.gameObject.layer != rockLayer || !LocalBounds(collider, out Bounds own))
                {
                    continue;
                }

                Bounds bounds = default;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 offset = Vector3.Scale(own.extents, new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                    Vector3 point = prefab.transform.InverseTransformPoint(collider.transform.TransformPoint(own.center + offset));
                    if (corner == 0)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                    }
                    else
                    {
                        bounds.Encapsulate(point);
                    }
                }

                found.Add(bounds);
            }
        }

        parts = found.ToArray();
        footprints[hash] = parts;
        return parts;
    }

    // A collider's box in its own space; prefabs are not in the scene, so Collider.bounds is empty for them.
    private static bool LocalBounds(Collider collider, out Bounds bounds)
    {
        switch (collider)
        {
            case BoxCollider box:
                bounds = new Bounds(box.center, box.size);
                return true;
            case SphereCollider sphere:
                bounds = new Bounds(sphere.center, Vector3.one * sphere.radius * 2f);
                return true;
            case CapsuleCollider capsule:
                Vector3 size = Vector3.one * capsule.radius * 2f;
                size[capsule.direction] = Mathf.Max(capsule.height, capsule.radius * 2f);
                bounds = new Bounds(capsule.center, size);
                return true;
            case MeshCollider mesh when mesh.sharedMesh != null:
                bounds = mesh.sharedMesh.bounds;
                return true;
            default:
                bounds = default;
                return false;
        }
    }
}
