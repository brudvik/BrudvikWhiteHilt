using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Makes a cloned piece with our own look break into pieces of that look, not of the vanilla piece it was cloned from.
/// </summary>
public static class PieceFragments
{
    private const string RootName = "WhiteHiltFragments";

    /// <summary>
    /// Sets up the destruction fragments of <paramref name="prefab"/> from what it shows now. Call it after the look is built.
    /// A mesh with chunks in the bundle (<c>&lt;mesh&gt;_frag0</c>, ... from a <c>.fragments.json</c>) breaks into those
    /// chunks and every other visible mesh falls whole. Without chunks, each visible mesh falls whole.
    /// </summary>
    /// <param name="prefab">The cloned piece prefab.</param>
    public static void Apply(GameObject prefab)
    {
        WearNTear wearNTear = prefab.GetComponent<WearNTear>();
        if (wearNTear == null)
        {
            return;
        }

        // An empty list makes WearNTear break the meshes that are visible at that moment.
        wearNTear.m_fragmentRoots = Array.Empty<GameObject>();

        Transform root = prefab.transform;
        MeshRenderer[] renderers = GetShownRenderers(root);
        Dictionary<MeshRenderer, Mesh[]> chunks = renderers
            .ToDictionary(renderer => renderer, renderer => ForagingAssets.LoadFragments(renderer.GetComponent<MeshFilter>().sharedMesh.name));
        if (chunks.Values.All(meshes => meshes.Length == 0))
        {
            return;
        }

        GameObject fragments = new(RootName);
        fragments.SetActive(false);
        fragments.transform.SetParent(root, false);
        foreach (MeshRenderer renderer in renderers)
        {
            Mesh[] meshes = chunks[renderer];
            if (meshes.Length == 0)
            {
                meshes = new[] { renderer.GetComponent<MeshFilter>().sharedMesh };
            }

            foreach (Mesh mesh in meshes)
            {
                AddCopy(fragments.transform, renderer, mesh);
            }
        }

        wearNTear.m_fragmentRoots = new[] { fragments };
    }

    // The enabled meshes of the active look, without the lower LOD levels that only stand in for them at a distance.
    private static MeshRenderer[] GetShownRenderers(Transform root)
    {
        HashSet<Renderer> lowerLods = new(root.GetComponentsInChildren<LODGroup>(true)
            .SelectMany(group => group.GetLODs().Skip(1))
            .SelectMany(lod => lod.renderers)
            .Where(renderer => renderer != null));

        return root.GetComponentsInChildren<MeshRenderer>(true)
            .Where(renderer => renderer.enabled
                && !lowerLods.Contains(renderer)
                && VisualHelper.IsActiveBelow(renderer.transform, root)
                && renderer.GetComponent<MeshFilter>()?.sharedMesh != null)
            .ToArray();
    }

    private static void AddCopy(Transform fragments, MeshRenderer source, Mesh mesh)
    {
        GameObject copy = new(mesh.name) { layer = source.gameObject.layer };
        copy.transform.SetParent(source.transform, false);
        copy.transform.SetParent(fragments, true);
        copy.AddComponent<MeshFilter>().sharedMesh = mesh;
        copy.AddComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
    }
}
