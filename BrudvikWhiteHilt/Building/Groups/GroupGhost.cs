using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Groups;

/// <summary>
/// A see-through preview of many pieces at once, for paste, blueprints, lines and areas. Only the meshes are copied, so
/// no piece logic runs; blue means it can be placed, red that something is missing.
/// </summary>
public static class GroupGhost
{
    private static readonly Color validColor = new(0.35f, 0.8f, 1f, 0.35f);
    private static readonly Color invalidColor = new(1f, 0.3f, 0.25f, 0.35f);
    private static readonly Dictionary<string, GameObject> templates = new();
    private static readonly List<(string Prefab, GameObject Instance, bool Valid)> instances = new();

    private static GameObject root;
    private static Material validMaterial;
    private static Material invalidMaterial;

    /// <summary>
    /// Shows the preview.
    /// </summary>
    /// <param name="items">The pieces and where they go.</param>
    /// <param name="valid">True if they can be placed.</param>
    /// <param name="itemValid">Per piece, whether that one can be placed; null to use <paramref name="valid"/> for all.</param>
    public static void Show(IList<GroupPlacer.Item> items, bool valid, System.Func<int, bool> itemValid = null)
    {
        if (!Ensure())
        {
            return;
        }

        root.SetActive(true);
        for (int i = 0; i < items.Count; i++)
        {
            string prefab = items[i].Piece.gameObject.name;
            bool ok = valid && (itemValid == null || itemValid(i));
            if (i >= instances.Count || instances[i].Prefab != prefab || instances[i].Instance == null)
            {
                GameObject instance = Object.Instantiate(Template(items[i].Piece), root.transform);
                instance.SetActive(true);
                SetMaterial(instance, ok ? validMaterial : invalidMaterial);
                if (i < instances.Count)
                {
                    Object.Destroy(instances[i].Instance);
                    instances[i] = (prefab, instance, ok);
                }
                else
                {
                    instances.Add((prefab, instance, ok));
                }
            }
            else if (instances[i].Valid != ok)
            {
                SetMaterial(instances[i].Instance, ok ? validMaterial : invalidMaterial);
                instances[i] = (prefab, instances[i].Instance, ok);
            }

            instances[i].Instance.transform.SetPositionAndRotation(items[i].Position, items[i].Rotation);
        }

        for (int i = instances.Count - 1; i >= items.Count; i--)
        {
            Object.Destroy(instances[i].Instance);
            instances.RemoveAt(i);
        }
    }

    /// <summary>
    /// Hides the preview.
    /// </summary>
    public static void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    // Creates the ghost root and its two see-through materials. Called each time, as the root is lost when the scene
    // changes.
    private static bool Ensure()
    {
        if (root != null)
        {
            return true;
        }

        // The scene was left and everything in it is gone.
        instances.Clear();
        templates.Clear();
        if (validMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (shader == null)
            {
                return false;
            }

            validMaterial = new Material(shader) { color = validColor };
            invalidMaterial = new Material(shader) { color = invalidColor };
        }

        root = new GameObject("WhiteHiltGroupGhost");
        return true;
    }

    // A hidden copy of a piece's meshes in the ghost colour, made once per piece and cloned for every ghost of it:
    // placing a large blueprint shows hundreds of ghosts, and instantiating the real prefabs would run their scripts.
    // Lower LOD levels and a plant's other looks are left out so the ghost shows the piece once.
    private static GameObject Template(Piece piece)
    {
        string key = piece.gameObject.name;
        if (templates.TryGetValue(key, out GameObject template) && template != null)
        {
            return template;
        }

        template = new GameObject("Ghost_" + key);
        template.SetActive(false);
        template.transform.SetParent(root.transform, false);
        Transform origin = piece.transform;
        HashSet<Renderer> lowDetail = LowDetailRenderers(piece.gameObject);
        Plant plant = piece.GetComponent<Plant>();
        GameObject[] otherLooks = plant != null ? new[] { plant.m_unhealthy, plant.m_healthyGrown, plant.m_unhealthyGrown } : new GameObject[0];
        foreach (MeshRenderer renderer in piece.GetComponentsInChildren<MeshRenderer>())
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || lowDetail.Contains(renderer)
                || System.Array.Exists(otherLooks, look => look != null && renderer.transform.IsChildOf(look.transform)))
            {
                continue;
            }

            GameObject part = new("Part");
            part.transform.SetParent(template.transform, false);
            part.transform.localPosition = origin.InverseTransformPoint(renderer.transform.position);
            part.transform.localRotation = Quaternion.Inverse(origin.rotation) * renderer.transform.rotation;
            part.transform.localScale = renderer.transform.lossyScale;
            part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            MeshRenderer copy = part.AddComponent<MeshRenderer>();
            copy.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            copy.receiveShadows = false;
        }

        templates[key] = template;
        return template;
    }

    // The renderers of every LOD level but the first, which the ghost leaves out so it does not draw the piece twice.
    private static HashSet<Renderer> LowDetailRenderers(GameObject prefab)
    {
        HashSet<Renderer> lowDetail = new();
        foreach (LODGroup group in prefab.GetComponentsInChildren<LODGroup>())
        {
            LOD[] lods = group.GetLODs();
            for (int i = 1; i < lods.Length; i++)
            {
                foreach (Renderer renderer in lods[i].renderers)
                {
                    if (renderer != null)
                    {
                        lowDetail.Add(renderer);
                    }
                }
            }
        }

        return lowDetail;
    }

    private static void SetMaterial(GameObject instance, Material material)
    {
        foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>())
        {
            Material[] materials = new Material[Mathf.Max(1, renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount)];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }

            renderer.sharedMaterials = materials;
        }
    }
}
