using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Kraken
{
    /// <summary>Applies the Kraken's appearance to a visual instance without modifying shared creature materials.</summary>
    public static class KrakenAppearance
    {
        /// <summary>Darkens the hide and narrows the glowing eyes into angry red slits.</summary>
        /// <param name="visual">The attached Kraken visual, also used by offline previews.</param>
        public static void Apply(GameObject visual)
        {
            foreach (SkinnedMeshRenderer renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMaterials.Any(material => material.IsKeywordEnabled("_EMISSION")))
                    NarrowEyes(visual.transform, renderer);
                renderer.sharedMaterials = renderer.sharedMaterials.Select(source =>
                {
                    Material material = new(source);
                    if (material.IsKeywordEnabled("_EMISSION"))
                    {
                        if (material.HasProperty("_EmissionColor"))
                            material.SetColor("_EmissionColor", new Color(5f, 0.12f, 0.03f));
                        if (material.HasProperty("_Color"))
                            material.color = new Color(0.9f, 0.08f, 0.03f);
                    }
                    else if (material.HasProperty("_Color"))
                        material.color *= new Color(0.42f, 0.22f, 0.27f);
                    return material;
                }).ToArray();
            }
        }

        // Squeezes the glowing eyes on a copy of the mesh into narrow slits that slant inwards, for an angry look.
        private static void NarrowEyes(Transform root, SkinnedMeshRenderer renderer)
        {
            Mesh mesh = Object.Instantiate(renderer.sharedMesh);
            Vector3 up = renderer.transform.InverseTransformDirection(root.up).normalized;
            Vector3 right = renderer.transform.InverseTransformDirection(root.right).normalized;
            Vector3[] vertices = mesh.vertices;
            int[] eyeVertices = Enumerable.Range(0, Mathf.Min(mesh.subMeshCount, renderer.sharedMaterials.Length))
                .Where(slot => renderer.sharedMaterials[slot].IsKeywordEnabled("_EMISSION"))
                .SelectMany(slot => mesh.GetTriangles(slot)).Distinct().ToArray();
            foreach (var eye in eyeVertices.GroupBy(index => Mathf.Sign(root.InverseTransformPoint(renderer.transform.TransformPoint(vertices[index])).x)))
            {
                Vector3 centre = eye.Aggregate(Vector3.zero, (sum, index) => sum + vertices[index]) / eye.Count();
                foreach (int index in eye)
                {
                    Vector3 offset = vertices[index] - centre;
                    vertices[index] += up * (-0.65f * Vector3.Dot(offset, up) + eye.Key * 0.3f * Vector3.Dot(offset, right));
                }
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            renderer.sharedMesh = mesh;
        }

    }
}