using System;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Copies the campfire's flames, light, crackle and smoke onto other pieces. Warmth and burning are left out.
/// </summary>
public static class FireEffects
{
    private const string CampfirePath = "_enabled_high";
    private static readonly string[] flameParts = { "Particles/flare", "Particles/flames (1)", "Particles/sparcs (1)", "Point light", "sfx_fire_loop" };

    /// <summary>
    /// Adds the campfire's flames at a point.
    /// </summary>
    /// <param name="parent">Object the flames belong to.</param>
    /// <param name="name">Name of the new child.</param>
    /// <param name="localPosition">Where the flames burn, in the parent's space.</param>
    /// <param name="scale">Size relative to the campfire.</param>
    /// <returns>The new child.</returns>
    public static Transform AddFlames(Transform parent, string name, Vector3 localPosition, float scale)
    {
        Transform campfire = GetCampfire();
        Transform fire = CreateAnchor(parent, name, localPosition);
        fire.localScale = Vector3.one * scale;
        foreach (string part in flameParts)
        {
            CopyPart(campfire, part, fire);
        }

        return fire;
    }

    /// <summary>
    /// Adds the campfire's rising smoke at a point.
    /// </summary>
    /// <param name="parent">Object the smoke belongs to.</param>
    /// <param name="name">Name of the new child.</param>
    /// <param name="localPosition">Where the smoke starts, in the parent's space.</param>
    /// <returns>The new child.</returns>
    public static Transform AddSmoke(Transform parent, string name, Vector3 localPosition)
    {
        Transform anchor = CreateAnchor(parent, name, localPosition);
        CopyPart(GetCampfire(), "SmokeSpawner", anchor).transform.localPosition = Vector3.zero;
        return anchor;
    }

    private static Transform GetCampfire()
    {
        return PrefabManager.Instance.GetPrefab("fire_pit")?.transform.Find(CampfirePath)
            ?? throw new InvalidOperationException($"fire_pit/{CampfirePath} was not found");
    }

    private static Transform CreateAnchor(Transform parent, string name, Vector3 localPosition)
    {
        GameObject anchor = new(name);
        anchor.transform.SetParent(parent, false);
        anchor.transform.localPosition = localPosition;
        return anchor.transform;
    }

    private static GameObject CopyPart(Transform source, string path, Transform parent)
    {
        Transform part = source.Find(path) ?? throw new InvalidOperationException($"{path} was not found");
        GameObject copy = UnityEngine.Object.Instantiate(part.gameObject, parent);
        copy.name = part.name;
        copy.transform.localPosition = part.localPosition;
        copy.transform.localRotation = part.localRotation;
        copy.transform.localScale = part.localScale;
        return copy;
    }
}
